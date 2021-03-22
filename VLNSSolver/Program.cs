using CO1;
using System;
using System.Collections.Generic;
using System.Globalization;


namespace VLNSSolverCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Arguments: pathToInstance runtime produceFiles millisecondsAddedPerFailedImprovement iter_baseValue iter_dependencyOnJobs iter_dependencyOnMachines " +
                    "weightOneOpti weightThreeOpti weightForAllOptionsAbove3InTotal weightChangeIfSolutionIsGood");

            string pathToInstance = args[0];
            int runtime = int.Parse(args[1]);
            bool produceFiles = bool.Parse(args[2]);
            int millisecondsAddedPerFailedImprovement = int.Parse(args[3], CultureInfo.InvariantCulture);
            float iter_baseValue = float.Parse(args[4], CultureInfo.InvariantCulture);
            float iter_dependencyOnJobs = float.Parse(args[5], CultureInfo.InvariantCulture);
            float iter_dependencyOnMachines = float.Parse(args[6], CultureInfo.InvariantCulture);
            long weightOneOpti = long.Parse(args[7], CultureInfo.InvariantCulture);
            long weightThreeOpti = long.Parse(args[8], CultureInfo.InvariantCulture);
            long weightForAllOptionsAbove3InTotal = long.Parse(args[9], CultureInfo.InvariantCulture);
            long weightChangeIfSolutionIsGood = long.Parse(args[10], CultureInfo.InvariantCulture);


            //(string outputFilePath, string outputFilePath2) = getFilepaths(filename, experimentName);
            ProblemInstance problem = new ProblemInstance(pathToInstance);

            VLNSSolver solver = new VLNSSolver(problem, millisecondsAddedPerFailedImprovement, iter_baseValue, iter_dependencyOnJobs, iter_dependencyOnMachines, weightOneOpti, weightThreeOpti, weightForAllOptionsAbove3InTotal, weightChangeIfSolutionIsGood);

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
