using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1.MachineFinderHeuristics
{
    class SelectByFindingBigProblems : MachineToOptimizeHeuristic
    {
        List<WeightedItem<int>> machinesWeighted = new List<WeightedItem<int>>();
        List<List<ScheduleForDifferentMachineInfo>> scheduleInfo;
        List<Tuple<int, long>> machineTardinessList = new List<Tuple<int, long>>();

        bool MachineToOptimizeHeuristic.areMachinesLeft()
        {
            return machineTardinessList.Count > 1;
        }

        void MachineToOptimizeHeuristic.fillInfo(SolutionCost cost, List<int>[] schedules, List<List<ScheduleForDifferentMachineInfo>> scheduleInfo)
        {
            this.scheduleInfo = scheduleInfo;
            for (int m = 0; m < cost.tardinessPerMachine.Count; m++)
            {
                machineTardinessList.Add(new Tuple<int, long>(m, cost.tardinessPerMachine[m]));
            }
            machineTardinessList = machineTardinessList.OrderByDescending(t => t.Item2).ToList();

            for (int m = 0; m < schedules.Length; m++)
            {
                if (schedules[m].Count > 1 && cost.tardinessPerMachine[m] > 0)
                    machinesWeighted.Add(new WeightedItem<int>(m, cost.tardinessPerMachine[m]));
            }
        }

        List<int> MachineToOptimizeHeuristic.selectMachines(int nrToSelectAtMost)
        {
            List<int> machinesSelected = new List<int>();
            machinesSelected.Add(WeightedItem<int>.ChooseAndRemove(ref machinesWeighted));
            

            while(machinesSelected.Count < nrToSelectAtMost && machineTardinessList.Count > 0)
            {
                List<WeightedItem<int>> machinesWeightedByApplicableTardyJobs = new List<WeightedItem<int>>();
                for (int i = 1; i < machineTardinessList.Count; i++)
                {
                    if (machineTardinessList[i].Item1 == machinesSelected[0])
                        continue;

                    long score = scheduleInfo[machinesSelected[0]][machineTardinessList[i].Item1].getNrTardyJobs();
                    machinesWeightedByApplicableTardyJobs.Add(new WeightedItem<int> (i, score));
                }
                int idx = WeightedItem<int>.ChooseAndRemove(ref machinesWeightedByApplicableTardyJobs);
                machinesSelected.Add(machineTardinessList[idx].Item1);
                machineTardinessList.RemoveAt(idx);
            }


            return machinesSelected;
        }
    }
}
