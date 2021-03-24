using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CO1
{
    public class VLNS_parameter
    {
        public int millisecondsAddedPerFailedImprovement;
        public float iter_baseValue, iter_dependencyOnJobs, iter_dependencyOnMachines;
        public long weightOneOpti, weightThreeOpti, weightForAllOptionsAbove3InTotal, weightChangeIfSolutionIsGood;
        public VLNS_parameter(string[] args)
        {
            this.millisecondsAddedPerFailedImprovement = int.Parse(args[0], CultureInfo.InvariantCulture);
            this.iter_baseValue = float.Parse(args[1], CultureInfo.InvariantCulture);
            this.iter_dependencyOnJobs = float.Parse(args[2], CultureInfo.InvariantCulture);
            this.iter_dependencyOnMachines = float.Parse(args[3], CultureInfo.InvariantCulture);
            this.weightOneOpti = long.Parse(args[4], CultureInfo.InvariantCulture);
            this.weightThreeOpti = long.Parse(args[5], CultureInfo.InvariantCulture);
            this.weightForAllOptionsAbove3InTotal = long.Parse(args[6], CultureInfo.InvariantCulture);
            this.weightChangeIfSolutionIsGood = long.Parse(args[7], CultureInfo.InvariantCulture);
        }
    }
}
