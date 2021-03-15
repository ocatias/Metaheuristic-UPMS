using CO1;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SASolver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Arguments: pathToInstance runtime produceFiles tmax tmin ns pI pS pB pT pM Bmax alpha ");

            string pathToInstance = args[0];
            int runtime = int.Parse(args[1]);
            bool produceFiles = bool.Parse(args[2]);
            double tmax = double.Parse(args[3], CultureInfo.InvariantCulture);
            double tmin = double.Parse(args[4], CultureInfo.InvariantCulture);
            long ns = long.Parse(args[5]);
            double pI = double.Parse(args[6], CultureInfo.InvariantCulture);
            double pS = double.Parse(args[7], CultureInfo.InvariantCulture);
            double pB = double.Parse(args[8], CultureInfo.InvariantCulture);
            double pT = double.Parse(args[9], CultureInfo.InvariantCulture);
            double pM = double.Parse(args[10], CultureInfo.InvariantCulture);
            long Bmax = long.Parse(args[11]);
            double alpha = double.Parse(args[12], CultureInfo.InvariantCulture);


            //(string outputFilePath, string outputFilePath2) = getFilepaths(filename, experimentName);
            ProblemInstance problem = new ProblemInstance(pathToInstance);


            SimulatedAnnealingSolver solver = new SimulatedAnnealingSolver(problem, tmax, tmin, (int)ns, pI, pS, pB, pT, pM, Bmax);

            List<int>[] schedule;

            if (!produceFiles)
                schedule = solver.solveDirect(runtime);
            else
                throw new Exception("Not implemented yet.");

            (long tardiness, long makespan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, schedule);
            Console.Write(tardiness * 1000000 + makespan);

        }
    }
}
