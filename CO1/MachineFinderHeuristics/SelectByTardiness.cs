﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CO1.MachineFinderHeuristics
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


        public List<int> selectMachines(int nrToSelectAtMost)
        {
            List<int> machines = new List<int>();
            while (machines.Count < nrToSelectAtMost && weightedMachinesList.Count > 0)
                machines.Add(WeightedItem<int>.ChooseAndRemove(ref weightedMachinesList));

            return machines;
        }

        public bool areMachinesLeft()
        {
            return weightedMachinesList.Count > 1;
        }
    }
}