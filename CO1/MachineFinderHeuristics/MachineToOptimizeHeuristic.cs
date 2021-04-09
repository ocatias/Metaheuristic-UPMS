using System;
using System.Collections.Generic;
using System.Text;

namespace CO1.MachineFinderHeuristics
{
    public interface MachineToOptimizeHeuristic
    {
        public List<int> selectMachines(int nrToSelectAtMost);

        public void fillInfo(SolutionCost cost, List<int>[] schedules, List<List<ScheduleForDifferentMachineInfo>> scheduleInfo, Random rnd);

        public bool areMachinesLeft();

    }
}
