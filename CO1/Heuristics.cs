using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public static class Heuristics
    {
        // 0 is the dummy job during computation, in the returned schedule 0 is the first job
        public static List<int>[] createInitialSchedules(ProblemInstance problem)
        {
            /*
             * Use a greedy algorithm to create an initial solution
             * "Algorithm 1 Constructive Heuristic (CH)" from the paper
             */

            List<int> jobs = new List<int>(Enumerable.Range(1, problem.jobs).ToArray());
            jobs.OrderBy(job => problem.dueDates[job]);

            List<int>[] schedules = new List<int>[problem.machines];
            int[] machineSpan = new int[problem.machines];
            int[] prevJob = new int[problem.machines];
            for (int i = 0; i < problem.machines; i++)
            {
                schedules[i] = new List<int>() { 0 };
                machineSpan[i] = 0;
                prevJob[i] = 0;
            }

            while (jobs.Count != 0)
            {
                // Find the job with the lowest due date where adding it to a machine increases the total tardiness by the lowest amount
                int job = jobs.Where(j => problem.dueDates[j - 1] == problem.dueDates[jobs[0] - 1]).OrderBy(j => getCostNumberOfJob(j, schedules, machineSpan, problem).Item1).First();

                (int minCost, int selectedMachine) = getCostNumberOfJob(job, schedules, machineSpan, problem);
                machineSpan[selectedMachine] += minCost;
                schedules[selectedMachine].Add(job);
                jobs.Remove(job);
            }

            // Remove dummy jobs from start of schedule so it has the correct format
            foreach (List<int> schedule in schedules)
                schedule.RemoveAt(0);

            foreach (List<int> schedule in schedules)
            {
                for (int i = 0; i < schedule.Count; i++)
                    schedule[i] = schedule[i] - 1;
            }

            return schedules;
        }

        // For the use in createInitialSolution(): What is the tardiness if you add the job now to the machine where it fits best?
        public static (int, int) getCostNumberOfJob(int job, List<int>[] schedule, int[] machineSpan, ProblemInstance problem)
        {
            int[] cost = new int[problem.machines];
            Array.Clear(cost, 0, cost.Length);

            for (int m = 0; m < problem.machines; m++)
            {
                if (problem.processingTimes[job - 1, m] < 0)
                {
                    cost[m] = int.MaxValue;
                    continue;
                }

                cost[m] = machineSpan[m] + problem.s[schedule[m].Last(), job, m] + problem.processingTimes[job - 1, m];
            }

            int selectedMachine = Array.IndexOf(cost, cost.Min());
            return (cost.Min(), selectedMachine);
        }
    }
}
