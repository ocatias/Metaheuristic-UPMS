using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public static class Verifier
    {
        public static (long, long) calculateTardMakeSpanFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            (long tardiness, long makespan, int machine) = calculateTardMakeSpanMachineFromMachineAssignment(problem, machinesOrder);
            return (tardiness, makespan);
        }
        public static (long, long, int) calculateTardMakeSpanMachineFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            SolutionCost sc = calcSolutionCostFromAssignment(problem, machinesOrder);
            return (sc.tardiness, sc.makeSpan, sc.makeSpanMachine);
        }

        public static SolutionCost calcSolutionCostFromAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            SolutionCost sc = new SolutionCost(machinesOrder.Count());

            for (int m = 0; m < problem.machines; m++)
            {
                (long tardinessOnThisMachine, long makeSpanOnThisMachine) = calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(problem, machinesOrder, m);

                sc.makeSpanPerMachine[m] = makeSpanOnThisMachine;
                sc.tardinessPerMachine[m] = tardinessOnThisMachine;
            }
            sc.updateMakeSpan();
            sc.updateTardiness();
            return sc;
        }


        public static SolutionCost updateTardMakeSpanMachineFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder, SolutionCost prevSolution, List<int> machinesToUpdate)
        {
            SolutionCost newSolution = new SolutionCost(prevSolution);

            // Ensure that we have no duplicate machines
            machinesToUpdate = machinesToUpdate.Distinct().ToList();

            foreach (int machine in machinesToUpdate)
            {
                (long tardynessOnMachine, long makeSpan) = calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(problem, machinesOrder, machine);
                newSolution.tardiness = newSolution.tardiness - newSolution.tardinessPerMachine[machine] + tardynessOnMachine;
                newSolution.tardinessPerMachine[machine] = tardynessOnMachine;
                newSolution.makeSpanPerMachine[machine] = makeSpan;
            }
            newSolution.updateMakeSpan();

            return newSolution;
        }

        // Returns (TardynessOnThisMachine, MakespanOnThisMachine)
        public static (long, long) calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(ProblemInstance problem, List<int>[] machinesOrder, int machine)
        {
            long tardiness = 0;
            long currMakeSpan = 0, currTimeOnMachine = 0;

            if (machinesOrder[machine].Count == 0)
                return (0,0);

            currMakeSpan += problem.getSetupTimeForJob(0, machinesOrder[machine][0] + 1, machine);
            currMakeSpan += problem.processingTimes[machinesOrder[machine][0], machine];

            currTimeOnMachine += problem.getSetupTimeForJob(0, machinesOrder[machine][0] + 1, machine);
            currTimeOnMachine += problem.processingTimes[machinesOrder[machine][0], machine];
            tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]] : 0;

            for (int i = 1; i < machinesOrder[machine].Count; i++)
            {
                currMakeSpan += problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                currMakeSpan += problem.processingTimes[machinesOrder[machine][i], machine];

                currTimeOnMachine += problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                currTimeOnMachine += problem.processingTimes[machinesOrder[machine][i], machine];
                tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]] : 0;
            }

            currMakeSpan += problem.getSetupTimeForJob(machinesOrder[machine][machinesOrder[machine].Count - 1] + 1, 0, machine);
            
            return (tardiness, currMakeSpan);
        }



        public static void verifyModelSolution(ProblemInstance problem, long tardinessFromModel, long makeSpanFromModel, List<int>[] machinesOrder)
        {
            // Verify if the assignment to machines is correct
            for (int m = 0; m < problem.machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    if (problem.processingTimes[job, m] < 0)
                        throw new Exception("Job assigned to not eligible machine.");
            }

            // Verify if each job is assigned exactly once
            int[] jobs = new int[problem.jobs];
            Array.Clear(jobs, 0, jobs.Length);

            for (int m = 0; m < problem.machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    jobs[job] += 1;
            }

            for (int i = 0; i < problem.jobs; i++)
            {
                if (jobs[i] != 1)
                    throw new Exception("A job is not assigned or assigned multiple times");
            }

            // Verify tardiness and makespan
            long tardiness, makeSpan;
            (tardiness, makeSpan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, machinesOrder);

            if (tardiness > tardinessFromModel)
                throw new Exception("Tardiness from model is contradictory");
            //else if (tardiness < tardinessFromModel)
            //    outputFile.WriteLine("Tardiness could be selected smaller.");

            //if (makeSpan > makeSpanFromModel)
            //    throw new Exception("Makespan from model is contradictory");
            //else if (makeSpan < makeSpanFromModel)
            //    outputFile.WriteLine("Makespan could be selected smaller.");

            //outputFile.WriteLine("Solution verified.");
        }
    }
}
