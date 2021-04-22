using CO1.MachineFinderHeuristics;
using Gurobi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CO1
{
    public class VLNSSolver
    {
        ProblemInstance problem;

        private float probabilityOptimizeMakespan = 0f;
        private bool isSolvedOptimally = false;
        private TabuList optimallySolvedTL;

        public List<int>[] schedules;

        private SolutionCost cost;
        private DateTime startTime;

        int millisecondsAddedPerFailedImprovement, minNrOfJobsToFreeze;
        float iter_baseValue, iter_dependencyOnJobs, iter_dependencyOnMachines, probability_freezing;
        long weightOneOpti, weightThreeOpti, weightForAllOptionsAbove3InTotal, weightChangeIfSolutionIsGood;
        long weightTwoOpti = 20000;

        bool isParallel = true;
        int max_threads = 6;
        int max_non_gurobi_threads = 2;
        int times_gurobi_is_better = 0;
        int times_sa_is_better = 0;
        int ties = 0;

        List<SimulatedAnnealingSolver> saSolvers = new List<SimulatedAnnealingSolver>();
        List<CancellationTokenSource> sources = new List<CancellationTokenSource>();
        List<Task> tasks = new List<Task>();

        // How many jobs from firstList are tardy and can be put on the machine from secondList
        private List<List<ScheduleForDifferentMachineInfo>> scheduleInfo = new List<List<ScheduleForDifferentMachineInfo>>();

        public VLNSSolver(ProblemInstance problem, VLNS_parameter parameter)
        {
            this.problem = problem;
            this.millisecondsAddedPerFailedImprovement = parameter.millisecondsAddedPerFailedImprovement;
            this.iter_baseValue = parameter.iter_baseValue;
            this.iter_dependencyOnJobs = parameter.iter_dependencyOnJobs;
            this.iter_dependencyOnMachines = parameter.iter_dependencyOnMachines;
            this.weightOneOpti = parameter.weightOneOpti;
            this.weightThreeOpti = parameter.weightThreeOpti;
            this.weightForAllOptionsAbove3InTotal = parameter.weightForAllOptionsAbove3InTotal;
            this.weightChangeIfSolutionIsGood = parameter.weightChangeIfSolutionIsGood;
            this.probability_freezing = parameter.probability_freezing;
            this.minNrOfJobsToFreeze = parameter.minNrOfJobsToFreeze;
        }

        public VLNSSolver(ProblemInstance problem, int millisecondsAddedPerFailedImprovement = 2000, float iter_baseValue = 30, float iter_dependencyOnJobs = 0.01f, float iter_dependencyOnMachines = 0.5f, 
            long weightOneOpti = 12000, long weightThreeOpti = 8000, long weightForAllOptionsAbove3InTotal = 1000, long weightChangeIfSolutionIsGood = +100, int minNrOfJobsToFreeze = 90, float probability_freezing = 0.8f)
        {
            this.problem = problem;
            this.millisecondsAddedPerFailedImprovement = millisecondsAddedPerFailedImprovement;
            this.iter_baseValue = iter_baseValue;
            this.iter_dependencyOnJobs = iter_dependencyOnJobs;
            this.iter_dependencyOnMachines = iter_dependencyOnMachines;
            this.weightOneOpti = weightOneOpti;
            this.weightThreeOpti = weightThreeOpti;
            this.weightForAllOptionsAbove3InTotal = weightForAllOptionsAbove3InTotal;
            this.weightChangeIfSolutionIsGood = weightChangeIfSolutionIsGood;
            this.probability_freezing = probability_freezing;
            this.minNrOfJobsToFreeze = minNrOfJobsToFreeze;
        }

        public List<int>[] solveDirect(int runtimeInSeconds, bool isHybridSolver = false)
        {
            return solveDirectAsync(runtimeInSeconds, isHybridSolver).GetAwaiter().GetResult();
        }

        private Tuple<SolutionCost, int> get_best_from_SAS(List<SimulatedAnnealingSolver> saSolvers, bool onlyCheck_max_non_gurboi_threads = false)
        {
            SolutionCost best_cost = saSolvers[0].lowestCost;
            int best_solver_idx = 0;
            int nr_solvers_to_check = saSolvers.Count;
            if (onlyCheck_max_non_gurboi_threads)
                nr_solvers_to_check = max_non_gurobi_threads;

            for (int i = 1; i < nr_solvers_to_check; i++)
            { 
                if(saSolvers[i].cost.isBetterThan(best_cost))
                {
                    best_cost = saSolvers[i].lowestCost;
                    best_solver_idx = i;
                }
            }
            return new Tuple<SolutionCost, int>(best_cost, best_solver_idx);
        }

        // Updates only the first max_non_gurobi_threads threads
        private void update_SAS(List<SimulatedAnnealingSolver> saSolvers, SolutionCost best_cost, List<int>[] best_schedule)
        {
            for(int i = 0; i < max_non_gurobi_threads; i++)
            {
                saSolvers[i].update(best_cost, best_schedule);
            }
        }

        private Tuple<List<Task>, List<CancellationTokenSource>> run_sasolvers(List<SimulatedAnnealingSolver> saSolvers)
        {
            var tasks = new List<Task>();
            var sources = new List<CancellationTokenSource>();

            for (int i = 0; i < max_non_gurobi_threads; i++)
            {
                
                var source = new CancellationTokenSource();
                var token = source.Token;
                var solver = saSolvers[i];
                tasks.Add(Task.Run(() =>
                { 
                    while (!token.IsCancellationRequested)
                    {
                        for (int iteration = 0; iteration < 100; iteration++)
                            solver.single_iteration();
                    } }));
                sources.Add(source);
            }
            return new Tuple<List<Task>, List<CancellationTokenSource>>(tasks, sources);
        }

        private async void cancel_tasks_and_wait(List<Task> tasks, List<CancellationTokenSource> sources)
        {
            foreach (CancellationTokenSource source in sources)
                source.Cancel();

            await Task.WhenAll(tasks);
        }

        private List<int> get_machines_that_changed(List<int>[] machines_before, List<int>[] machines_after)
        {
            List<int> machines_that_changed = new List<int>();
            for(int m = 0; m < problem.machines; m++)
            {
                bool equal = true;
                if (machines_before[m].Count() != machines_after[m].Count())
                    equal = false;
                else
                {
                    for(int i = 0; i < machines_before[m].Count(); i++)
                    {
                        if(machines_before[m][i] != machines_after[m][i])
                        {
                            equal = false;
                            break;
                        }
                    }
                }

                if (!equal)
                    machines_that_changed.Add(m);
            }

            return machines_that_changed;
        }

        public async Task<List<int>[]> solveDirectAsync(int runtimeInSeconds, bool isHybridSolver = false)
        {
            startTime = DateTime.UtcNow;

            saSolvers = new List<SimulatedAnnealingSolver>();
            sources = new List<CancellationTokenSource>();
            tasks = new List<Task>();

            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Start();

            Random rnd = new Random();

            if (!isHybridSolver)
                schedules = Heuristics.createInitialSchedules(problem);
            else
            {
                Console.WriteLine("HybridSolver");

                int saSolverRuntime;
                if (60 < runtimeInSeconds / 2)
                    saSolverRuntime = 60;
                else
                    saSolverRuntime = (int)(runtimeInSeconds * 0.5);

                if (!isParallel)
                {
                    SimulatedAnnealingSolver saSolver = new SimulatedAnnealingSolver(problem);
                    schedules = saSolver.solveDirect(saSolverRuntime);
                }
                else
                {
                    saSolvers = new List<SimulatedAnnealingSolver>();
                    for(int i = 0; i < max_threads; i++)
                    {
                        SimulatedAnnealingSolver solver = new SimulatedAnnealingSolver(new ProblemInstance (problem));
                        solver.seed = i;
                        saSolvers.Add(solver);
                    }
                    var tasks = new List<Task>();

                    for (int i = 0; i < max_threads; i++)
                    {
                        SimulatedAnnealingSolver solver = saSolvers[i];
                        tasks.Add(Task.Run(() => solver.solveDirect(saSolverRuntime)));
                    }
                    await Task.WhenAll(tasks);
                    var tuple = get_best_from_SAS(saSolvers);
                    update_SAS(saSolvers, tuple.Item1, saSolvers[tuple.Item2].schedules);
                    foreach (SimulatedAnnealingSolver solver in saSolvers)
                        Console.WriteLine(solver.seed.ToString() + ": " + solver.cost.tardiness.ToString() + ", " + solver.cost.makeSpan.ToString());
                    schedules = Helpers.cloneSchedule(saSolvers[0].bestSchedules);

                    (tasks, sources) = run_sasolvers(saSolvers);
                }

                
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            for (int m = 0; m < problem.machines; m++)
            {
                scheduleInfo.Add(new List<ScheduleForDifferentMachineInfo>());
                updateScheduleInfo(m, Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, schedules, m).Item3);
            }
            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
            Console.WriteLine(String.Format("Best Result from Heuristic: ({0},{1})", cost.tardiness, cost.makeSpan));

            solveSmallAmounts(startTime, ref schedules, env, ref cost, rnd, runtimeInSeconds);


            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));
            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
            env.Dispose();
            return schedules;
        }

        // Returns (isSolutionFromGurobi, machines changed)
        private Tuple<bool, List<int>> update_parallel_solutions(List<int> machines_gurobi_changed, bool run_solvers_again = true)
        {
            List<int> machines_changed = new List<int>();
            bool is_solution_from_gurobi = true;

            if (isParallel)
            {
                cancel_tasks_and_wait(tasks, sources);
                (SolutionCost cost_from_solver, int best_solver_idx) = get_best_from_SAS(saSolvers, true);
                if (cost_from_solver.isBetterThan(cost))
                {
                    Console.WriteLine("SA beats Gurobi");
                    times_sa_is_better++;
                    is_solution_from_gurobi = false;

                    machines_changed = get_machines_that_changed(schedules, saSolvers[best_solver_idx].bestSchedules);
                    schedules = Helpers.cloneSchedule(saSolvers[best_solver_idx].bestSchedules);
                    cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
                    update_SAS(saSolvers, cost_from_solver, saSolvers[best_solver_idx].bestSchedules);
                }
                else if (cost.isBetterThan(cost_from_solver))
                {
                    Console.WriteLine("Gurobi beats SA");
                    times_gurobi_is_better++;
                    update_SAS(saSolvers, cost, schedules);
                    machines_changed = machines_gurobi_changed;
                }
                else
                {
                    Console.WriteLine("Tie");
                    ties++;
                    update_SAS(saSolvers, cost, schedules);
                    machines_changed = machines_gurobi_changed;


                }
                if (run_solvers_again)
                    (tasks, sources) = run_sasolvers(saSolvers);
            }
            return new Tuple<bool, List<int>>(is_solution_from_gurobi, machines_changed);
        }

        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule, bool isHybridSolver = false)
        {
            schedules = solveDirect(runtimeInSeconds, isHybridSolver);

            using (StreamWriter outputFile = new StreamWriter(filepathResultInfo))
            {
                outputFile.WriteLine(String.Format("Tardiness: {0}", cost.tardiness));
                outputFile.WriteLine(String.Format("Makespan: {0}", cost.makeSpan));
                outputFile.WriteLine(String.Format("Selected runtime: {0}s", runtimeInSeconds));
                Console.WriteLine(DateTime.UtcNow.Subtract(startTime).TotalSeconds);
                outputFile.WriteLine(String.Format("Actual runtime: {0}s", DateTime.UtcNow.Subtract(startTime).TotalSeconds));
                if (isSolvedOptimally)
                    outputFile.WriteLine("Solution proven to be optimal");
                outputFile.Write(String.Format("Optimally solved pairings:\n{0}", optimallySolvedTL.outputTabulist()));
                if(isParallel)
                {
                    outputFile.WriteLine(String.Format("Gurobi wins: {0}", times_gurobi_is_better.ToString()));
                    outputFile.WriteLine(String.Format("SA wins: {0}", times_sa_is_better.ToString()));
                    outputFile.WriteLine(String.Format("Ties: {0}", ties.ToString()));


                }
            }

            ResultExport.storeMachineSchedule(filepathMachineSchedule, problem, schedules);
        }

        private void solveSmallAmounts(DateTime startTime, ref List<int>[] schedules, GRBEnv env, ref SolutionCost cost, Random rnd, int runtimeInSeconds)
        {

            long weightChangeIfSolutionIsBadAndOptimal = -weightChangeIfSolutionIsGood;
            int nrIterations = (int)Math.Ceiling(iter_baseValue + iter_dependencyOnJobs * problem.jobs + iter_dependencyOnMachines * problem.machines);


            long weightManyOpti = 0; 
            long weightVariance = 0; 

            if(problem.machines > 3)
            {
                weightManyOpti = (long)(weightForAllOptionsAbove3InTotal / ((problem.machines - 3) / 2.0f));
                weightVariance = (10 - weightManyOpti) / (problem.machines - 3);
            }


            List<WeightedItem<int>> choices = new List<WeightedItem<int>> {
                new WeightedItem<int>(1, weightOneOpti), new  WeightedItem<int>(2, weightTwoOpti),
                new WeightedItem<int>(3, weightThreeOpti)};

            while (choices.Count > problem.machines)
                choices.RemoveAt(choices.Count - 1);

            while (choices.Count < problem.machines)
            {
                choices.Add(new WeightedItem<int>(choices.Count, weightManyOpti));
                weightManyOpti += weightVariance;
            }
         
            TabuList recentlySolvedTL = new TabuList();
            optimallySolvedTL = new TabuList();
            bool isOptimal;

            double timeRemainingInMS = runtimeInSeconds * 1000 - DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;

            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                double timeToUse = timeRemainingInMS / nrIterations;
                if (timeToUse > timeRemainingInMS)
                    timeToUse = timeRemainingInMS;
                (schedules[singleMachineIdx], isOptimal) = sm.solveModel((int)(timeToUse), cost.tardinessPerMachine[singleMachineIdx]);

                recentlySolvedTL.addPairing(singleMachineIdx);
                if (isOptimal)
                    optimallySolvedTL.addPairing(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            

            MachineToOptimizeHeuristic machineSelector1 = new SelectByTardiness();
            MachineToOptimizeHeuristic machineSelector2 = new SelectByFindingBigProblems();

            int millisecondsTime = (int)Math.Ceiling(timeRemainingInMS / nrIterations);

            // If there is only machine we do not need to call the MIP solver multiple times
            if (problem.machines == 1)
                millisecondsTime = (int)timeRemainingInMS;

            List<int> allMachines = new List<int>();
            for (int i = 0; i < problem.machines; i++)
                allMachines.Add(i);

            int forbiddenPairsFoundInARow = 0;
            const int MAXFORBIDDENPAIRSINAROW = 5;

            while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < runtimeInSeconds*1000)
            {
                //if (!optimallySolvedTL.isNotATabuPairing(allMachines))
                //{
                //    Console.WriteLine("SOLVED OPTIMALLY");
                //    isSolvedOptimally = true;
                //    break;
                //}

                Console.WriteLine(String.Format("Current solution: ({0},{1})", cost.tardiness, cost.makeSpan));         

                machineSelector1 = new SelectByTardiness();
                machineSelector2 = new SelectByFindingBigProblems();

                int nrOfMachinesToSolve;

                // Clean TabuList if it is too full
                if (recentlySolvedTL.Count() >= Math.Pow(2, problem.machines) - 1)
                {
                    Console.WriteLine("CLEAN TABU LIST");
                    recentlySolvedTL.clean(optimallySolvedTL);
                    millisecondsTime = runtimeInSeconds*1000 - (int)DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                }
                int choice = WeightedItem<int>.Choose(choices, rnd);

                MachineToOptimizeHeuristic machineSelector;
                if (rnd.NextDouble() < 0.1)
                {
                    Console.WriteLine("Select by tardiness");
                    machineSelector = machineSelector1;
                }
                else
                {
                    Console.WriteLine("Select by finding big problem");
                    machineSelector = machineSelector2;
                }

                machineSelector.fillInfo(cost, schedules, scheduleInfo, rnd);

                long tardinessBefore = cost.tardiness;
                long makespanBefore = cost.makeSpan;

                // Ensure that the solver does not go over the maximum time
                int timeForSolver = millisecondsTime;
                int maxTimeForSolver = runtimeInSeconds * 1000 - (int)DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                if (timeForSolver > maxTimeForSolver)
                    timeForSolver = maxTimeForSolver;
                if (maxTimeForSolver <= 0)
                    break;

                List<int> machinesToChange = null;

                if (choice == 1)
                {
                    int singleMachineIdx = machineSelector.selectMachines(1).First();

                    if (!recentlySolvedTL.isAllowedPairing(singleMachineIdx) || !optimallySolvedTL.isNotSubsetOfATabuPairing(singleMachineIdx))
                    {
                        Console.WriteLine("Fordbidden Pairing found: " + forbiddenPairsFoundInARow.ToString());
                        forbiddenPairsFoundInARow++;
                        if (forbiddenPairsFoundInARow >= MAXFORBIDDENPAIRSINAROW)
                        {
                            Console.WriteLine("Fix manually (sm)");
                            machinesToChange = findNextPairingNotInTabulist(recentlySolvedTL, optimallySolvedTL);
                            if (machinesToChange == null)
                            {
                                machinesToChange = new List<int>();
                                for (int i = 0; i < problem.machines; i++)
                                    machinesToChange.Append(i);
                            }
                            nrOfMachinesToSolve = machinesToChange.Count;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        Console.WriteLine("Singlemachine: " + singleMachineIdx.ToString() + ", time: " + timeForSolver.ToString());
                        forbiddenPairsFoundInARow = 0;
                        SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                        (schedules[singleMachineIdx], isOptimal) = sm.solveModel(timeForSolver, cost.tardinessPerMachine[singleMachineIdx]);
                        cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
                        machinesToChange = new List<int>{ singleMachineIdx };

                        if (cost.tardiness != tardinessBefore || cost.makeSpan != makespanBefore)
                        {
                            recentlySolvedTL.removePairings(new List<int> { singleMachineIdx });
                            optimallySolvedTL.removePairings(new List<int> { singleMachineIdx });
                            WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsGood);
                        }
                        else
                        {
                            timeRemainingInMS += millisecondsAddedPerFailedImprovement;
                            WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsBadAndOptimal);
                        }
                        bool solution_from_gurobi2 = true;
                        if (isParallel)
                            (solution_from_gurobi2, machinesToChange) = update_parallel_solutions(machinesToChange);

                        recentlySolvedTL.addPairing(machinesToChange);
                        if (isOptimal && solution_from_gurobi2)
                            optimallySolvedTL.addPairing(machinesToChange);
                        continue;
                    }
                }
                else
                    nrOfMachinesToSolve = choice;

                if (machinesToChange == null)
                    machinesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

  

                if (!recentlySolvedTL.isNotATabuPairing(machinesToChange))
                {
                    Console.WriteLine("Fordbidden Pairing found: " + forbiddenPairsFoundInARow.ToString());
                    forbiddenPairsFoundInARow++;
                    if (forbiddenPairsFoundInARow >= MAXFORBIDDENPAIRSINAROW)
                    {
                        Console.WriteLine("Fix manually (mm)");
                        machinesToChange = findNextPairingNotInTabulist(recentlySolvedTL, optimallySolvedTL);
                        forbiddenPairsFoundInARow = 0;
                    }
                    else
                        continue;
                }
                else
                    forbiddenPairsFoundInARow = 0;



                Console.WriteLine("Calc #jobs");
                long tardinessBeforeForMachingesToChange = 0;
                int nrOfJobs = 0;

                if (machinesToChange == null)
                {
                    machinesToChange = new List<int>();
                    for (int i = 0; i < problem.machines; i++)
                        machinesToChange.Append(i);
                }

                foreach (int m in machinesToChange)
                {
                    tardinessBeforeForMachingesToChange += cost.tardinessPerMachine[m];
                    nrOfJobs += schedules[m].Count;
                }

                Console.WriteLine("Calc jobs to freeze");

                List<Tuple<int, int, int>> jobsToFreeze = new List<Tuple<int, int, int>>(); // (Job1Id, Job2Id, Machine) 
                if (nrOfJobs >= minNrOfJobsToFreeze && rnd.NextDouble() < probability_freezing)
                {
                    
                    int maxJobsPerMachine = minNrOfJobsToFreeze / machinesToChange.Count;
                    foreach (int m in machinesToChange)
                    {
                        List<int> machineSchedule = new List<int>(schedules[m]);
                        while (machineSchedule.Count > maxJobsPerMachine)
                        {
                            int idx = rnd.Next(0, machineSchedule.Count - 1);
                            jobsToFreeze.Add(new Tuple<int, int, int>(machineSchedule[idx], machineSchedule[idx + 1], m));
                            machineSchedule.RemoveAt(idx + 1);
                            machineSchedule.RemoveAt(idx);
                        }
                    }
                    Console.WriteLine(String.Format("Freezing {0} pairs", jobsToFreeze.Count));
                }

                // This is a failsafe that should never get triggered!
                if (machinesToChange.Count == 0)
                {
                    for (int i = 0; i < problem.machines; i++)
                        machinesToChange.Add(i);
                }

                Console.WriteLine("Start mm");
                MultiMachineModel tm = new MultiMachineModel(problem, env, schedules, machinesToChange, jobsToFreeze);

                (schedules, isOptimal) = tm.solveModel(timeForSolver, tardinessBeforeForMachingesToChange, !(rnd.NextDouble() < probabilityOptimizeMakespan));

                foreach (int m in machinesToChange)
                {
                    List<Tuple<int, long>> scheduleInfoForMachine;
                    (cost.tardinessPerMachine[m], cost.makeSpanPerMachine[m], scheduleInfoForMachine) = Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, schedules, m);
                    updateScheduleInfo(m, scheduleInfoForMachine);
                }
                cost.updateTardiness();
                cost.updateMakeSpan();
                bool solution_from_gurobi = true;

                if (isParallel)
                    (solution_from_gurobi, machinesToChange) = update_parallel_solutions(machinesToChange);

                if (cost.tardiness != tardinessBefore || cost.makeSpan != makespanBefore)
                {
                    recentlySolvedTL.removePairings(machinesToChange);
                    optimallySolvedTL.removePairings(machinesToChange);
                    WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsGood);
                }
                else
                {
                    millisecondsTime += millisecondsAddedPerFailedImprovement;
                    WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsBadAndOptimal);
                }

                // Only add to tabu lists if we did not freeze
                if (jobsToFreeze.Count == 0)
                {
                    recentlySolvedTL.addPairing(machinesToChange);

                    if (isOptimal && solution_from_gurobi)
                        optimallySolvedTL.addPairing(machinesToChange);
                }

                //Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

                //cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

                //if (!optimallySolvedTL.isNotATabuPairing(allMachines))
                //{
                //    Console.WriteLine("SOLVED OPTIMALLY");
                //    isSolvedOptimally = true;
                //    break;
                //}
            }
            update_parallel_solutions(null, false);
            Console.WriteLine(String.Format("{0}, tabu pairings found", recentlySolvedTL.nrOfTabuPairingsFound()));
        }


        public List<int> findNextPairingNotInTabulist(TabuList recentlySolvedList, TabuList optimallySolvedList)
        {
            List<int> lengthOneList = new List<int>();
            for (int i = 0; i < problem.machines; i++)
                lengthOneList.Add(i);

            List<List<int>> allElementsOffCurLength = new List<List<int>>();
            foreach (int elem in lengthOneList)
                allElementsOffCurLength.Add(new List<int> { elem });

            for(int length = 1; length <= problem.machines; length++)
            {
                foreach (List<int> pairing in allElementsOffCurLength)
                    if (recentlySolvedList.isNotATabuPairing(pairing) && optimallySolvedList.isNotSubsetOfATabuPairing(pairing))
                        return pairing;

                List<List<int>> allElementsOffCurLengthNew = new List<List<int>>();
                for(int i = 0; i < allElementsOffCurLength.Count; i++)
                {
                    foreach(int elem in lengthOneList)
                    {
                        if (allElementsOffCurLength[i].Contains(elem))
                            continue;

                        List<int> newList = new List<int>(allElementsOffCurLength[i]);
                        newList.Add(elem);
                        allElementsOffCurLengthNew.Add(newList);
                    }
                }
                allElementsOffCurLength = allElementsOffCurLengthNew;
            }
            return null;
        }

        // Update the schedule info for a given machine 
        // note that if multiple machines change then this needs to be called for every single one of them otherwise the data becomes inconsistent
        private void updateScheduleInfo(int machine, List<Tuple<int, long>> machineTardinessPairs)
        {
            List<ScheduleForDifferentMachineInfo> scheduleInfoForMachine = new List<ScheduleForDifferentMachineInfo>();
            for (int m = 0; m < problem.machines; m++)
            {
                int nrTardyJobs = 0;
                int nrPrematureJobs = 0;

                if (m == machine)
                {
                    scheduleInfoForMachine.Add(null);
                    continue;
                }

                foreach (Tuple<int, long> tuple in machineTardinessPairs)
                {
                    if (problem.processingTimes[tuple.Item1, m] >= 0)
                    {
                        if (tuple.Item2 > 0)
                        {
                            nrTardyJobs++;
                        }
                        else if (tuple.Item2 <= -50)
                        {
                            nrPrematureJobs++;
                        }
                    }
                }
                scheduleInfoForMachine.Add(new ScheduleForDifferentMachineInfo(nrTardyJobs, nrPrematureJobs));
            }
            scheduleInfo[machine] = scheduleInfoForMachine;
        }

    }
}
