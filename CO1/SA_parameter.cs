using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CO1
{
    public class SA_parameter
    {
        public double tMax, tMin, pI, pS, pB, pT, pM, alpha;
        public long ns;
        public int Bmax;
        public SA_parameter(string[] args)
        {
            this.tMax = double.Parse(args[0], CultureInfo.InvariantCulture);
            this.tMin = double.Parse(args[1], CultureInfo.InvariantCulture);
            this.ns = long.Parse(args[2]);
            this.pI = double.Parse(args[3], CultureInfo.InvariantCulture);
            this.pS = double.Parse(args[4], CultureInfo.InvariantCulture);
            this.pB = double.Parse(args[5], CultureInfo.InvariantCulture);
            this.pT = double.Parse(args[6], CultureInfo.InvariantCulture);
            this.pM = double.Parse(args[7], CultureInfo.InvariantCulture);
            this.Bmax = int.Parse(args[8]);
            this.alpha = double.Parse(args[9], CultureInfo.InvariantCulture);
        }

    }
}
