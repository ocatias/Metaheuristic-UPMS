using Gurobi;
using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public class VLNSSolver
    {
        ProblemInstance problem;
        public VLNSSolver(ProblemInstance problem)
        {
            this.problem = problem;

        }

        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule)
        {
            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Start();

            List<int>[] schedules = Heuristics.createInitialSchedules(problem);

            SolutionCost cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

            Console.WriteLine(String.Format("Best Result from Heuristic: ({0},{1})", cost.tardiness, cost.makeSpan));

            List<int>[] tempSchedule = schedules;
            List<int> changedMachines = new List<int>();

            for (int singleMachineIdx = 0; singleMachineIdx < problem.machines; singleMachineIdx++)
            {
                SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                tempSchedule[singleMachineIdx] = sm.solveModel((int)(runtimeInSeconds*1000.0/(problem.machines)), cost.tardinessPerMachine[singleMachineIdx]);
                changedMachines.Add(singleMachineIdx);
            }

            cost = Verifier.calcSolutionCostFromAssignment(problem, tempSchedule);
            Console.WriteLine(String.Format("Best Result from SM: ({0},{1})", cost.tardiness, cost.makeSpan));

            List<WeightedItem<int>> weightedMachinesList = new List<WeightedItem<int>>();

            for (int nrOfMachinesToSolve = 2; nrOfMachinesToSolve <= problem.machines - 1; nrOfMachinesToSolve++)
            {
                for (int m = 0; m < schedules.Length; m++)
                {
                    if (schedules[m].Count > 1 && cost.tardinessPerMachine[m] > 0)
                        weightedMachinesList.Add(new WeightedItem<int>(m, cost.tardinessPerMachine[m]));
                }

                int millisecondsTime = 30000 / (problem.machines / nrOfMachinesToSolve);


                while (weightedMachinesList.Count > 1)
                {
                    List<int> machingesToChange = new List<int>();
                    for (int selector = 0; selector < nrOfMachinesToSolve && weightedMachinesList.Count > 1; selector++)
                    {
                        int selectedMachine = WeightedItem<int>.ChooseAndRemove(weightedMachinesList);
                        machingesToChange.Add(selectedMachine);
                    }

                    long tardinessBefore = 0;
                    foreach (int m in machingesToChange)
                        tardinessBefore += cost.tardinessPerMachine[m];

                    TwoMachineModel tm = new TwoMachineModel(problem, env, tempSchedule, machingesToChange);
                    tempSchedule = tm.solveModel(millisecondsTime, tardinessBefore);

                    foreach (int m in machingesToChange)
                    {
                        (cost.tardinessPerMachine[m], cost.makeSpanPerMachine[m]) = Verifier.calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(problem, tempSchedule, m);
                    }
                    cost.updateTardiness();
                    cost.updateMakeSpan();

                    Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, tempSchedule);
                }

                cost = Verifier.calcSolutionCostFromAssignment(problem, tempSchedule);
                Console.WriteLine(String.Format("Best Result from VLNS: ({0},{1})", cost.tardiness, cost.makeSpan));
            }

            

            ResultExport.storeMachineSchedule(filepathMachineSchedule, problem, schedules);
        }
    }
}
