﻿using CO1.MachineFinderHeuristics;
using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public class VLNSSolver
    {
        ProblemInstance problem;

        private float probabilityOptimizeMakespan = 0f;

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

            solveSmallAmounts(startTime, ref schedules, env, ref cost, rnd, runtimeInSeconds);


            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));
            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
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
                schedules[singleMachineIdx] = sm.solveModel((int)(timeRemainingInMS / 10 / problem.machines), cost.tardinessPerMachine[singleMachineIdx]);
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
                    schedules = tm.solveModel(millisecondsTime, tardinessBefore, !(rnd.NextDouble() < probabilityOptimizeMakespan));

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
                schedules[singleMachineIdx] = sm.solveModel((int)(timeRemainingInMS / problem.machines), cost.tardinessPerMachine[singleMachineIdx]);
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
        }

        private void solveSmallAmounts(DateTime startTime, ref List<int>[] schedules, GRBEnv env, ref SolutionCost cost, Random rnd, int runtimeInSeconds)
        {
            int nrIterations = 30;

            double timeRemainingInMS = runtimeInSeconds * 1000 - DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;

            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                schedules[singleMachineIdx] = sm.solveModel((int)(timeRemainingInMS / 10 / problem.machines), cost.tardinessPerMachine[singleMachineIdx]);
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);
            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));

            MachineToOptimizeHeuristic machineSelector1 = new SelectByTardiness();
            MachineToOptimizeHeuristic machineSelector2 = new SelectByFindingBigProblems();

            while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < timeRemainingInMS)
            {
                double randomDouble = rnd.NextDouble();
                int nrOfMachinesToSolve;

                if (randomDouble >= 0.8)
                {

                    continue;
                }
                else  if(randomDouble < 0.8)
                {
                    nrOfMachinesToSolve = 2;
                }
                else
                {
                    nrOfMachinesToSolve = 3;
                }

                int millisecondsTime = (int)Math.Ceiling(timeRemainingInMS / nrIterations);

                MachineToOptimizeHeuristic machineSelector;

                if (rnd.NextDouble() < 0.7)
                    machineSelector = machineSelector1;
                else
                    machineSelector = machineSelector2;

                machineSelector.fillInfo(cost, schedules, scheduleInfo);

                while (machineSelector.areMachinesLeft())
                {
                    List<int> machingesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

                    long tardinessBefore = 0;
                    foreach (int m in machingesToChange)
                        tardinessBefore += cost.tardinessPerMachine[m];

                    MultiMachineModel tm = new MultiMachineModel(problem, env, schedules, machingesToChange);
                    schedules = tm.solveModel(millisecondsTime, tardinessBefore, !(rnd.NextDouble() < probabilityOptimizeMakespan));

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