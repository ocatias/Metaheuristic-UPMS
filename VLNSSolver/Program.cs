using CO1;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VLNSSolverCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Arguments: pathToInstance runtime produceFiles millisecondsAddedPerFailedImprovement iter_baseValue iter_dependencyOnJobs iter_dependencyOnMachines " +
                    "weightOneOpti weightThreeOpti weightForAllOptionsAbove3InTotal weightChangeIfSolutionIsGood pathToStoreResults");

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
                schedule = solver.solveDirect(runtime, true);
            else
            {
                string file = pathToInstance.Split(Path.DirectorySeparatorChar).Last();
                string pathToStoreResults = args[13];
                (string fpInfo, string fpSchedule) = getFilepaths(file.Split('.')[0], pathToStoreResults);
                solver.solve(runtime, fpInfo, fpSchedule, true);
                return;
            }
            
            (long tardiness, long makespan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, schedule);
            Console.Write(tardiness * 1000000 + makespan);

        }

        public static (string fpInfo, string fpSchedule) getFilepaths(string filename, string pathToStoreOutput)
        {
            if (!Directory.Exists(pathToStoreOutput))
                Directory.CreateDirectory(pathToStoreOutput);

            string currOutputFilename = filename;
            string outputFilePath = pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + ".soln.info";
            string outputFilePath2 = pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + ".soln";

            int i = 2;
            if (File.Exists(outputFilePath) || File.Exists(outputFilePath2))
            {
                while (File.Exists(pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + i + ".soln") || File.Exists(pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + "_" + i + ".soln.info"))
                    i++;
                outputFilePath = pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + "_" + i + ".soln.info";
                outputFilePath2 = pathToStoreOutput + Path.DirectorySeparatorChar + currOutputFilename + "_" + i + ".soln";
            }
            return (outputFilePath, outputFilePath2);
        }
    }
}
