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

            string[] parameters = new string[args.Length -3];

            for (int i = 3; i < args.Length; i++)
                parameters[i - 3] = args[i];

            VLNS_parameter vlns_params = new VLNS_parameter(parameters);

            //(string outputFilePath, string outputFilePath2) = getFilepaths(filename, experimentName);
            ProblemInstance problem = new ProblemInstance(pathToInstance);

            VLNSSolver solver = new VLNSSolver(problem, vlns_params);

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
