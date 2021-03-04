using System;
using System.Collections.Generic;
using System.Text;

namespace CO1.MachineToOptimizeHeuristics
{
    public class SelectByTardiness : MachineToOptimizeHeuristic
    {
        List<WeightedItem<int>> weightedMachinesList = new List<WeightedItem<int>>();

        public void fillInfo(SolutionCost cost, List<int>[] schedules, List<List<ScheduleForDifferentMachineInfo>> scheduleInfo)
        {
            weightedMachinesList = new List<WeightedItem<int>>();
            for (int m = 0; m < schedules.Length; m++)
            {
                if (schedules[m].Count > 1 && cost.tardinessPerMachine[m] > 0)
                    weightedMachinesList.Add(new WeightedItem<int>(m, cost.tardinessPerMachine[m]));
            }
        }


        public int selectMachine()
        {
            return 0;
        }

        public bool areMachinesLeft()
        {
            return weightedMachinesList.Count > 1;
        }

        public bool isMachineLeft()
        {
            return weightedMachinesList.Count >= 1;
        }

    }
}
