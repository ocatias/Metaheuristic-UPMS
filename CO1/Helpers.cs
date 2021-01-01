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
    }
}
