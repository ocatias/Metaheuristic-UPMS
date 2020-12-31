using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public static class Verifier
    {
        public static (long, long) calculateTardMakeSpanFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            long tardiness = 0;
            long maxMakeSpan = 0;
            for (int m = 0; m < problem.machines; m++)
            {
                long currMakeSpan = 0, currTimeOnMachine = 0;

                if (machinesOrder[m].Count == 0)
                    continue;

                currMakeSpan += problem.getSetupTimeForJob(0, machinesOrder[m][0] + 1, m);
                currMakeSpan += problem.processingTimes[machinesOrder[m][0], m];


                currTimeOnMachine += problem.getSetupTimeForJob(0, machinesOrder[m][0] + 1, m);
                currTimeOnMachine += problem.processingTimes[machinesOrder[m][0], m];
                tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[m][0]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[m][0]] : 0;

                for (int i = 1; i < machinesOrder[m].Count; i++)
                {
                    currMakeSpan += problem.getSetupTimeForJob(machinesOrder[m][i - 1] + 1, machinesOrder[m][i] + 1, m);
                    currMakeSpan += problem.processingTimes[machinesOrder[m][i], m];

                    currTimeOnMachine += problem.getSetupTimeForJob(machinesOrder[m][i - 1] + 1, machinesOrder[m][i] + 1, m);
                    currTimeOnMachine += problem.processingTimes[machinesOrder[m][i], m];
                    tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[m][i]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[m][i]] : 0;
                }

                currMakeSpan += problem.getSetupTimeForJob(machinesOrder[m][machinesOrder[m].Count - 1] + 1, 0, m);

                if (currMakeSpan > maxMakeSpan)
                    maxMakeSpan = currMakeSpan;
            }
            return (tardiness, maxMakeSpan);
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
