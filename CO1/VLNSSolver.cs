using CO1.MachineFinderHeuristics;
using Gurobi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CO1
{
    public class VLNSSolver
    {
        ProblemInstance problem;

        private float probabilityOptimizeMakespan = 0f;
        private bool isSolvedOptimally = false;
        private TabuList optimallySolvedTL;

        private SolutionCost cost;
        private DateTime startTime;

        int millisecondsAddedPerFailedImprovement, minNrOfJobsToFreeze;
        float iter_baseValue, iter_dependencyOnJobs, iter_dependencyOnMachines, probability_freezing;
        long weightOneOpti, weightThreeOpti, weightForAllOptionsAbove3InTotal, weightChangeIfSolutionIsGood;
        long weightTwoOpti = 20000;

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
            startTime = DateTime.UtcNow;


            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Start();

            Random rnd = new Random();

            List<int>[] schedules;
            if (!isHybridSolver)
                schedules = Heuristics.createInitialSchedules(problem);
            else
            {
                Console.WriteLine("HybridSolver");
                SimulatedAnnealingSolver saSolver = new SimulatedAnnealingSolver(problem);
                int saSolverRuntime = 0;
                if (60 < runtimeInSeconds / 2)
                    saSolverRuntime = 60;
                else
                    saSolverRuntime = (int)(runtimeInSeconds * 0.5);

                schedules = saSolver.solveDirect(saSolverRuntime);
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

            return schedules;
        }

        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule, bool isHybridSolver = false)
        {
            List<int>[] schedules = solveDirect(runtimeInSeconds, isHybridSolver);

            using (StreamWriter outputFile = new StreamWriter(filepathResultInfo))
            {
                outputFile.WriteLine(String.Format("Tardiness: {0}", cost.tardiness));
                outputFile.WriteLine(String.Format("Makespan: {0}", cost.makeSpan));
                outputFile.WriteLine(String.Format("Selected runtime: {0}s", runtimeInSeconds));
                outputFile.WriteLine(String.Format("Actual runtime: {0}s", DateTime.UtcNow.Subtract(startTime).TotalSeconds));
                if (isSolvedOptimally)
                    outputFile.WriteLine("Solution proven to be optimal");
                outputFile.Write(String.Format("Optimally solved pairings:\n{0}", optimallySolvedTL.outputTabulist()));

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
            const int MAXFORBIDDENPAIRSINAROW = 30;

            while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < timeRemainingInMS)
            {
                if (!optimallySolvedTL.isNotATabuPairing(allMachines))
                {
                    Console.WriteLine("SOLVED OPTIMALLY");
                    isSolvedOptimally = true;
                    break;
                }

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
                int choice = WeightedItem<int>.Choose(choices);

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

                machineSelector.fillInfo(cost, schedules, scheduleInfo);

                long tardinessBefore = cost.tardiness;
                long makespanBefore = cost.makeSpan;

                // Ensure that the solver does not go over the maximum time
                int timeForSolver = millisecondsTime;
                int maxTimeForSolver = runtimeInSeconds * 1000 - (int)DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                if (timeForSolver > maxTimeForSolver)
                    timeForSolver = maxTimeForSolver;
                if (maxTimeForSolver <= 0)
                    break;

                if (choice == 1)
                {
                    int singleMachineIdx = machineSelector.selectMachines(1).First();

                    if (!recentlySolvedTL.isAllowedPairing(singleMachineIdx) || !optimallySolvedTL.isNotSubsetOfATabuPairing(singleMachineIdx))
                    {
                        Console.WriteLine("Fordbidden Pairing found.");
                        forbiddenPairsFoundInARow++;
                        continue;
                    }
                    else
                        forbiddenPairsFoundInARow = 0;
                    SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                    (schedules[singleMachineIdx], isOptimal) = sm.solveModel(timeForSolver, cost.tardinessPerMachine[singleMachineIdx]);
                    cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

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

                    recentlySolvedTL.addPairing(singleMachineIdx);
                    if (isOptimal)
                        optimallySolvedTL.addPairing(singleMachineIdx);
                    continue;
                }
                else
                    nrOfMachinesToSolve = choice;

                List<int> machingesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

                if (!recentlySolvedTL.isNotATabuPairing(machingesToChange))
                {
                    Console.WriteLine("Fordbidden Pairing found.");
                    forbiddenPairsFoundInARow++;
                    if (forbiddenPairsFoundInARow >= MAXFORBIDDENPAIRSINAROW)
                        machingesToChange = findNextPairingNotInTabulist(recentlySolvedTL, optimallySolvedTL);
                    else
                        continue;
                }
                else
                    forbiddenPairsFoundInARow = 0;

                long tardinessBeforeForMachingesToChange = 0;
                int nrOfJobs = 0;
                foreach (int m in machingesToChange)
                {
                    tardinessBeforeForMachingesToChange += cost.tardinessPerMachine[m];
                    nrOfJobs += schedules[m].Count;
                }

                List<Tuple<int, int, int>> jobsToFreeze = new List<Tuple<int, int, int>>(); // (Job1Id, Job2Id, Machine) 
                if (nrOfJobs >= minNrOfJobsToFreeze && rnd.NextDouble() < probability_freezing)
                {
                    
                    int maxJobsPerMachine = minNrOfJobsToFreeze / machingesToChange.Count;
                    foreach (int m in machingesToChange)
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


                MultiMachineModel tm = new MultiMachineModel(problem, env, schedules, machingesToChange, jobsToFreeze);

                (schedules, isOptimal) = tm.solveModel(timeForSolver, tardinessBeforeForMachingesToChange, !(rnd.NextDouble() < probabilityOptimizeMakespan));

                foreach (int m in machingesToChange)
                {
                    List<Tuple<int, long>> scheduleInfoForMachine;
                    (cost.tardinessPerMachine[m], cost.makeSpanPerMachine[m], scheduleInfoForMachine) = Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, schedules, m);
                    updateScheduleInfo(m, scheduleInfoForMachine);
                }
                cost.updateTardiness();
                cost.updateMakeSpan();

                if (cost.tardiness != tardinessBefore || cost.makeSpan != makespanBefore)
                {
                    recentlySolvedTL.removePairings(machingesToChange);
                    optimallySolvedTL.removePairings(machingesToChange);
                    WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsGood);
                }
                else
                {
                    millisecondsTime += millisecondsAddedPerFailedImprovement;
                    WeightedItem<int>.adaptWeight(ref choices, choice, weightChangeIfSolutionIsBadAndOptimal);
                }

                recentlySolvedTL.addPairing(machingesToChange);
                // Only add to OPTIMAL tabu lists if we did not freeze
                if (jobsToFreeze.Count == 0)
                {
                    if (isOptimal)
                        optimallySolvedTL.addPairing(machingesToChange);
                }

                //Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

                //cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

                if (!optimallySolvedTL.isNotATabuPairing(allMachines))
                {
                    Console.WriteLine("SOLVED OPTIMALLY");
                    isSolvedOptimally = true;
                    break;
                }
            }

            Console.WriteLine(String.Format("{0}, tabu pairings found", recentlySolvedTL.nrOfTabuPairingsFound()));
        }


        public List<int> findNextPairingNotInTabulist(TabuList recentlySolvedList, TabuList optimallySolvedList)
        {
            List<int> lengthOneList = new List<int>();
            for (int i = 0; i < problem.machines; i++)
                lengthOneList.Add(i);

            List<List<int>> allElementsOffCurLength = new List<List<int>>();
            foreach (int elem in lengthOneList)
                allElementsOffCurLength.Add(new List<int>(elem));

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
                        allElementsOffCurLength.Add(newList);
                    }
                }
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
