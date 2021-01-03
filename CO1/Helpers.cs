using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public static class Helpers
    {
        public static List<int>[] cloneSchedule(List<int>[] schedules)
        {
            List<int>[] tempSchedule = new List<int>[schedules.Length];
            for (int i = 0; i < schedules.Length; i++)
                tempSchedule[i] = new List<int>(schedules[i]);
            return tempSchedule;
        }

        public static long cost(long tardiness, long makespan)
        {
            return 100000 * tardiness + makespan;
        }

        // Returns true if the job is tardy in the schedule
        public static bool isJobTardy(ProblemInstance problem, List<int>[] schedules, int job, int machine)
        {
            long currTimeOnMachine = 0;

            if (schedules[machine].Count == 0)
                throw new Exception("Machine has no jobs scheduled empty!");

            currTimeOnMachine += problem.getSetupTimeForJob(0, schedules[machine][0] + 1, machine);
            currTimeOnMachine += problem.processingTimes[schedules[machine][0], machine];

            if(schedules[machine][0] == job)
            {
                return currTimeOnMachine > problem.dueDates[job];
            }

            for (int i = 1; i < schedules[machine].Count; i++)
            {
                currTimeOnMachine += problem.getSetupTimeForJob(schedules[machine][i - 1] + 1, schedules[machine][i] + 1, machine);
                currTimeOnMachine += problem.processingTimes[schedules[machine][i], machine];

                if (schedules[machine][i] == job)
                {
                    return currTimeOnMachine > problem.dueDates[job];
                }
            }

            throw new Exception("Job is not scheduled on this machine!");
        }
    }
}
