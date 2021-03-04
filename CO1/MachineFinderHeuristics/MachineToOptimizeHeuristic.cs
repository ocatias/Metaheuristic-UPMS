using System;
using System.Collections.Generic;
using System.Text;

namespace CO1.MachineToOptimizeHeuristics
{
    public interface MachineToOptimizeHeuristic
    {
        public int selectMachine();

        public void fillInfo(SolutionCost cost, List<int>[] schedules, List<List<ScheduleForDifferentMachineInfo>> scheduleInfo);

        public bool areMachinesLeft();

        public bool isMachineLeft();
    }
}
