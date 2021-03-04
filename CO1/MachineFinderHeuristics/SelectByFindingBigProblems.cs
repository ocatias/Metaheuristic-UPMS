using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1.MachineFinderHeuristics
{
    class SelectByFindingBigProblems : MachineToOptimizeHeuristic
    {
        List<List<ScheduleForDifferentMachineInfo>> scheduleInfo;
        List<Tuple<int, long>> machineTardinessList = new List<Tuple<int, long>>();

        bool MachineToOptimizeHeuristic.areMachinesLeft()
        {
            return machineTardinessList.Count > 1;
        }

        void MachineToOptimizeHeuristic.fillInfo(SolutionCost cost, List<int>[] schedules, List<List<ScheduleForDifferentMachineInfo>> scheduleInfo)
        {
            this.scheduleInfo = scheduleInfo;
            for(int m = 0; m < cost.tardinessPerMachine.Count; m++)
            {
                machineTardinessList.Add(new Tuple<int, long>(m, cost.tardinessPerMachine[m]));
            }
            machineTardinessList = machineTardinessList.OrderByDescending(t =>t.Item2).ToList(); 
        }

        List<int> MachineToOptimizeHeuristic.selectMachines(int nrToSelectAtMost)
        {
            List<int> machinesSelected = new List<int>();
            machinesSelected.Add(machineTardinessList[0].Item1);
            machineTardinessList.RemoveAt(0);

            while(machinesSelected.Count < nrToSelectAtMost && machineTardinessList.Count > 0)
            {
                double scoreBest = double.NegativeInfinity;
                int machineBest = -1;
                int machineBestIdx = -1;
                for(int i = 0; i < machineTardinessList.Count; i++)
                {
                    if (i == machinesSelected[0])
                        continue;
                    //double score = scheduleInfo[machinesSelected[0]][machineTardinessList[i].Item1].getNrPrematureJobs() + scheduleInfo[machinesSelected[0]][machineTardinessList[i].Item1].getNrTardyJobs();
                    double score = scheduleInfo[machinesSelected[0]][machineTardinessList[i].Item1].getNrTardyJobs();

                    if (score > scoreBest)
                    {
                        scoreBest = score;
                        machineBest = machineTardinessList[i].Item1;
                        machineBestIdx = i;
                    }
                }
                machinesSelected.Add(machineBest);
                machineTardinessList.RemoveAt(machineBestIdx);
            }


            return machinesSelected;
        }
    }
}
