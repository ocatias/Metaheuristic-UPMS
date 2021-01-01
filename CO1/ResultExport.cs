using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CO1
{
    public static class ResultExport
    {

        public static void storeMachineSchedule(string filepath, ProblemInstance problem, List<int>[] machinesOrder)
        {
            using (StreamWriter outputFileMachineOrder = new StreamWriter(filepath))
            {
                outputFileMachineOrder.WriteLine("[Schedules]");
                for (int m = 0; m < problem.machines; m++)
                {
                    outputFileMachineOrder.Write(m + ";");
                    foreach (int succ in machinesOrder[m])
                    {
                        outputFileMachineOrder.Write(succ.ToString(new string('0', (int)(Math.Log10(problem.jobs) + 1))));
                        if (machinesOrder[m].IndexOf(succ) != machinesOrder[m].Count - 1)
                            outputFileMachineOrder.Write(";");
                    }
                    outputFileMachineOrder.Write("\n");
                }
            }
        }
    }
}
