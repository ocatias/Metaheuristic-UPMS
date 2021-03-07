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

        // How many jobs from firstList are tardy and can be put on the machine from secondList
        private List<List<ScheduleForDifferentMachineInfo>> scheduleInfo = new List<List<ScheduleForDifferentMachineInfo>>();

        public VLNSSolver(ProblemInstance problem)
        {
            this.problem = problem;

        }

        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule)
        {
            DateTime startTime = DateTime.UtcNow;


            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Start();

            Random rnd = new Random();

            List<int>[] schedules = Heuristics.createInitialSchedules(problem);

            SolutionCost cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            for(int m = 0; m < problem.machines; m++)
            {
                scheduleInfo.Add(new List<ScheduleForDifferentMachineInfo>());
                updateScheduleInfo(m, Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, schedules, m).Item3);
            }
            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
            Console.WriteLine(String.Format("Best Result from Heuristic: ({0},{1})", cost.tardiness, cost.makeSpan));

            solveSmallAmounts(startTime, ref schedules, env, ref cost, rnd, runtimeInSeconds, filepathResultInfo);


            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));
            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

            using (StreamWriter outputFile = new StreamWriter(filepathResultInfo))
            {
                outputFile.WriteLine(String.Format("Tardiness: {0}", cost.tardiness));
                outputFile.WriteLine(String.Format("Makespan: {0}", cost.makeSpan));
                outputFile.WriteLine(String.Format("Selected runtime: {0}s", runtimeInSeconds));
                outputFile.WriteLine(String.Format("Actual runtime: {0}s", DateTime.UtcNow.Subtract(startTime).TotalSeconds));
                if (isSolvedOptimally)
                    outputFile.WriteLine("Solution proven to be optimal");

            }

            ResultExport.storeMachineSchedule(filepathMachineSchedule, problem, schedules);
        }


        // First optimize only one machine, then two and so on;
        private void solveRisingFalling(DateTime startTime, ref List<int>[] schedules, GRBEnv env, ref SolutionCost cost, Random rnd, int runtimeInSeconds)
        {
            double timeRemainingInMS = runtimeInSeconds*1000 - DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;

            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                schedules[singleMachineIdx] = sm.solveModel((int)(timeRemainingInMS / 10 / problem.machines), cost.tardinessPerMachine[singleMachineIdx]).Item1;
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));

            MachineToOptimizeHeuristic machineSelector = new SelectByTardiness();
            //MachineToOptimizeHeuristic machineSelector = new SelectByFindingBigProblems();

            List<int> nrMachinesToSolveList = new List<int>();

            for (int i = 2; i <= problem.machines; i++)
                nrMachinesToSolveList.Add(i);

            for (int i = problem.machines - 1; i > 1; i--)
                nrMachinesToSolveList.Add(i);

            timeRemainingInMS = runtimeInSeconds * 1000 - DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            double timePerNr = timeRemainingInMS * 0.9 / (nrMachinesToSolveList.Count);


            foreach (int nrOfMachinesToSolve in nrMachinesToSolveList)
            {
                int millisecondsTime = (int)Math.Ceiling(timePerNr / (problem.machines / nrOfMachinesToSolve));

                machineSelector.fillInfo(cost, schedules, scheduleInfo);

                while (machineSelector.areMachinesLeft())
                {
                    List<int> machingesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

                    long tardinessBefore = 0;
                    foreach (int m in machingesToChange)
                        tardinessBefore += cost.tardinessPerMachine[m];

                    MultiMachineModel tm = new MultiMachineModel(problem, env, schedules, machingesToChange);
                    bool isOptimal;
                    (schedules, isOptimal) = tm.solveModel(millisecondsTime, tardinessBefore, !(rnd.NextDouble() < probabilityOptimizeMakespan));

                    foreach (int m in machingesToChange)
                    {
                        List<Tuple<int, long>> scheduleInfoForMachine;
                        (cost.tardinessPerMachine[m], cost.makeSpanPerMachine[m], scheduleInfoForMachine) = Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, schedules, m);
                        updateScheduleInfo(m, scheduleInfoForMachine);
                    }
                    cost.updateTardiness();
                    cost.updateMakeSpan();

                    Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
                }

                cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            }

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                schedules[singleMachineIdx] = sm.solveModel((int)(timeRemainingInMS / problem.machines), cost.tardinessPerMachine[singleMachineIdx]).Item1;
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
        }

        private void solveSmallAmounts(DateTime startTime, ref List<int>[] schedules, GRBEnv env, ref SolutionCost cost, Random rnd, int runtimeInSeconds, string outputFile)
        {
            int millisecondsAddedPerFailedImprovement = 2000;

            int nrIterations = 30;

            long weightOneOpti = 12000;
            long weightTwoOpti = 20000;
            long weightThreeOpti = 8000;

            long weightForAllOptionsAbove3InTotal = 1000;

            long weightManyOpti = 0; 
            long weightVariance = 0; 

            if(problem.machines > 3)
            {
                weightManyOpti = (long)(weightForAllOptionsAbove3InTotal / ((problem.machines - 3) / 2.0f));
                weightVariance = (10 - weightManyOpti) / (problem.machines - 3);
            }

            long weightChangeIfSolutionIsGood = +100;
            long weightChangeIfSolutionIsBadAndOptimal = -500;

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
            TabuList optimallySolvedTL = new TabuList();
            bool isOptimal;


            double timeRemainingInMS = runtimeInSeconds * 1000 - DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;

            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                (schedules[singleMachineIdx], isOptimal) = sm.solveModel((int)(timeRemainingInMS / 10 / problem.machines), cost.tardinessPerMachine[singleMachineIdx]);
                recentlySolvedTL.addPairing(singleMachineIdx);
                if (isOptimal)
                    optimallySolvedTL.addPairing(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            MachineToOptimizeHeuristic machineSelector1 = new SelectByTardiness();
            MachineToOptimizeHeuristic machineSelector2 = new SelectByFindingBigProblems();

            int millisecondsTime = (int)Math.Ceiling(timeRemainingInMS / nrIterations);

            List<int> allMachines = new List<int>();
            for (int i = 0; i < problem.machines; i++)
                allMachines.Add(i);

            while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < timeRemainingInMS)
            {
                if (!optimallySolvedTL.isAllowedPairing(allMachines))
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

                    if (!recentlySolvedTL.isAllowedPairing(singleMachineIdx))
                    {
                        Console.WriteLine("Fordbidden Pairing found.");
                        continue;
                    }
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

                //while (machineSelector.areMachinesLeft())
                //{
                List<int> machingesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

                if (!recentlySolvedTL.isAllowedPairing(machingesToChange))
                {
                    Console.WriteLine("Fordbidden Pairing found.");
                    continue;
                }

                long tardinessBeforeForMachingesToChange = 0;
                foreach (int m in machingesToChange)
                    tardinessBeforeForMachingesToChange += cost.tardinessPerMachine[m];

                MultiMachineModel tm = new MultiMachineModel(problem, env, schedules, machingesToChange);

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
                if (isOptimal)
                    optimallySolvedTL.addPairing(machingesToChange);

                //Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
                //}

                //cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

                if (!optimallySolvedTL.isAllowedPairing(allMachines))
                {
                    Console.WriteLine("SOLVED OPTIMALLY");
                    isSolvedOptimally = true;
                    break;
                }
            }

            Console.WriteLine(String.Format("{0}, tabu pairings found", recentlySolvedTL.nrOfTabuPairingsFound()));
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
