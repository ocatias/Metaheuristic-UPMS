using CO1.MachineFinderHeuristics;
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



            List<int>[] tempSchedule = schedules;
            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                tempSchedule[singleMachineIdx] = sm.solveModel((int)(runtimeInSeconds * 1000.0 / (problem.machines)), cost.tardinessPerMachine[singleMachineIdx]);
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, tempSchedule);
            Console.WriteLine(String.Format("Best Result from SM: ({0},{1})", cost.tardiness, cost.makeSpan));

            //MachineToOptimizeHeuristic machineSelector = new SelectByTardiness();
            MachineToOptimizeHeuristic machineSelector = new SelectByFindingBigProblems();


            for (int nrOfMachinesToSolve = 2; nrOfMachinesToSolve <= problem.machines; nrOfMachinesToSolve++)
            {
                int millisecondsTime = 30000 / (problem.machines / nrOfMachinesToSolve);

                machineSelector.fillInfo(cost, schedules, scheduleInfo);
                
                while (machineSelector.areMachinesLeft())
                {
                    List<int> machingesToChange = machineSelector.selectMachines(nrOfMachinesToSolve);

                    long tardinessBefore = 0;
                    foreach (int m in machingesToChange)
                        tardinessBefore += cost.tardinessPerMachine[m]; 

                    MultiMachineModel tm = new MultiMachineModel(problem, env, tempSchedule, machingesToChange);
                    tempSchedule = tm.solveModel(millisecondsTime, tardinessBefore, !(rnd.NextDouble() < probabilityOptimizeMakespan));

                    foreach (int m in machingesToChange)
                    {
                        List<Tuple<int, long>> scheduleInfoForMachine;
                        (cost.tardinessPerMachine[m], cost.makeSpanPerMachine[m], scheduleInfoForMachine) = Verifier.calcuTdMsScheduleInfoForSingleMachine(problem, tempSchedule, m);
                        updateScheduleInfo(m, scheduleInfoForMachine);
                    }
                    cost.updateTardiness();
                    cost.updateMakeSpan();

                    Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, tempSchedule);
                }

                cost = Verifier.calcSolutionCostFromAssignment(problem, tempSchedule);
                Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));
            
            }

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, tempSchedule[singleMachineIdx], singleMachineIdx);
                tempSchedule[singleMachineIdx] = sm.solveModel((int)(runtimeInSeconds * 1000.0 / (problem.machines)), cost.tardinessPerMachine[singleMachineIdx]);
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, tempSchedule);
            Console.WriteLine(String.Format("Best Result from SM: ({0},{1})", cost.tardiness, cost.makeSpan));
            Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));

            ResultExport.storeMachineSchedule(filepathMachineSchedule, problem, schedules);
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
