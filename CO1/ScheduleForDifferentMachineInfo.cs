using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    // Collects information about what kind of jobs the schedule contains that can be move to other machines
    // Note: This class does not know for which machine we store this information
    public class ScheduleForDifferentMachineInfo
    {
        // How many tardy / premature jobs does the schedule contain that are also eligible for the machine?
        private long nrTardyJobs, nrPrematureJobs;
        public long getNrTardyJobs()
        {
            return nrTardyJobs;
        }

        public long getNrPrematureJobs()
        {
            return nrPrematureJobs;
        }

        public ScheduleForDifferentMachineInfo(long nrTardyJobs, long nrPrematureJobs)
        {
            this.nrTardyJobs = nrTardyJobs;
            this.nrPrematureJobs = nrPrematureJobs;
        }

    }
}
