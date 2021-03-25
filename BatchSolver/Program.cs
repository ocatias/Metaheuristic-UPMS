using CO1;
using System;
using System.IO;
using System.Linq;

namespace BatchSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                Console.WriteLine("Arguments: pathToInstances pathToStoreResults nr_repeats algorithm runtimeInSeconds algorithm-parameters ...");

            string pathToInstances = args[0];
            string pathToStoreResults = args[1];
            int repeats = int.Parse(args[2]);
            string algorithm = args[3].ToLower();
            int runtimeInSeconds = int.Parse(args[4]);

            string[] parameters = new string[args.Length - 5];

            for (int i = 5; i < args.Length; i++)
                parameters[i - 5] = args[i];


            string[] files = System.IO.Directory.GetFiles(pathToInstances, "*.max");
            for (int iteration = 0; iteration < repeats; iteration++)
            {
                foreach (string filePath in files)
                {
                    string file = filePath.Split(Path.DirectorySeparatorChar).Last();
                    Console.WriteLine(file);

                    ProblemInstance problem = new ProblemInstance(filePath);
                    (string fpInfo, string fpSchedule) = getFilepaths(file.Split('.')[0], pathToStoreResults);
                    switch (algorithm)
                    {
                        case "sa":
                            SA_parameter sa_params = new SA_parameter(parameters);
                            SimulatedAnnealingSolver sa_solver = new SimulatedAnnealingSolver(problem, sa_params);
                            sa_solver.solve(runtimeInSeconds, fpInfo, fpSchedule);
                            break;
                        case "vlns":
                            VLNS_parameter vlns_params = new VLNS_parameter(parameters);
                            VLNSSolver vlns_solver = new VLNSSolver(problem, vlns_params);
                            vlns_solver.solve(runtimeInSeconds, fpInfo, fpSchedule);
                            break;
                        case "hybrid":
                            VLNS_parameter vlns_params2 = new VLNS_parameter(parameters);
                            VLNSSolver vlns_solver2 = new VLNSSolver(problem, vlns_params2);
                            vlns_solver2.solve(runtimeInSeconds, fpInfo, fpSchedule, true);
                            break;
                    }
                        

                }
            }
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
