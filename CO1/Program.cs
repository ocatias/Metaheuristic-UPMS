    using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CO1
{
    class Program
    {

        static void Main()
        {
            string pathValidation = Environment.GetEnvironmentVariable("ValidationDataPath");
            string pathTraining = Environment.GetEnvironmentVariable("TrainingDataPath");
            //string pathRealLife = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\real-life";

            // Big files:
            //List<string> allFilesInDirectory = new List<string>() { "p_13-80-80_1.max", "p_15-60-60_1.max", "p_15-63-80_1.max", "p_16-100-100_1.max", "p_16-180-180_1.max", "p_17-100-100_1.max", "p_18-80-80_2.max", "p_20-180-180_1.max",
            //    "p_22-140-140_1.max", "p_29-140-140_1.max", "p_3-17-20_1.max", "p_7-19-40_1.max", "p_9-180-180_1.max", "s_10-120-180_1.max", "s_1-3-100_1.max", "s_15-80-80_2.max", "s_15-80-80_3.max",
            //    "s_22-149-160_1.max", "s_4-16-20_1.max", "t_10-24-40_1.max", "t_15-77-80_1.max", "t_18-56-100_1.max", "t_20-76-100_1.max", "t_28-34-100_1.max", "t_3-12-200_1.max"  };

            List<string> allFilesInDirectory = new List<string>() { "s_24-900-900_1.max", "p_23-840-840_1.max", "t_22-760-760_1.max" };

            //allFilesInDirectory = allFilesInDirectory.OrderBy(x => Guid.NewGuid()).ToList();

            //runVLNS(180, 1, pathValidation, allFilesInDirectory, "2203_VLNS_180s");
            //runSimulatedAnnealing(10, 1, pathValidation, allFilesInDirectory, "0803_SA_10s");
            //runSimulatedAnnealing(60, 1, pathValidation, allFilesInDirectory, "2203_SA_60s");
            //runSimulatedAnnealing(1800, 1, pathValidation, allFilesInDirectory, "0803_SA_1800s");

            //runLinearModels("MIP_30Min_each");

            runHybridSolver(180, 1, pathValidation, allFilesInDirectory, "2203_Hybrid_180s");
        }

        public static void runVLNS(int secondsPerRun, int repeats, string path, List<string> filenames, string experimentName)
        {
            Console.WriteLine("Run VLNS Solver");

            for (int i = 0; i < repeats; i++)
            {
                //Parallel.ForEach(filenames, (filename) =>
                //{
                foreach (string filename in filenames)
                {
                    Console.WriteLine(String.Format("Current File: {0}", filename));
                    ProblemInstance problem = new ProblemInstance(path + "\\" + filename);
                    (string fpInfo, string fpSchedule) = getFilepaths(filename, experimentName);

                    VLNSSolver solver = new VLNSSolver(problem);
                    solver.solve(secondsPerRun, fpInfo, fpSchedule);
                }
            }
        }

        public static void runHybridSolver(int secondsPerRun, int repeats, string path, List<string> filenames, string experimentName)
        {
            Console.WriteLine("Run Hybrid Solver");

            for (int i = 0; i < repeats; i++)
            {
                //Parallel.ForEach(filenames, (filename) =>
                //{
                foreach (string filename in filenames)
                {
                    Console.WriteLine(String.Format("Current File: {0}", filename));
                    ProblemInstance problem = new ProblemInstance(path + "\\" + filename);
                    (string fpInfo, string fpSchedule) = getFilepaths(filename, experimentName);

                    VLNSSolver solver = new VLNSSolver(problem);
                    solver.solve(secondsPerRun, fpInfo, fpSchedule, true);
                }
            }
        }

        public static void runSimulatedAnnealing(int secondsPerRun, int repeats, string path, List<string> filenames, string experimentName)
        {
            Console.WriteLine("Run Simulated Annealing Solver");

            for (int i = 0; i < repeats; i++)
            {
                //Parallel.ForEach(filenames, (filename) =>
                //{
                foreach (string filename in filenames)
                {
                    Console.WriteLine(String.Format("Current File: {0}", filename));
                    ProblemInstance problem = new ProblemInstance(path + "\\" + filename);
                    (string fpInfo, string fpSchedule) = getFilepaths(filename, experimentName);

                    SimulatedAnnealingSolver solver = new SimulatedAnnealingSolver(problem);
                    solver.solve(secondsPerRun, fpInfo, fpSchedule);
                }
            }
        }

        public static (string fpInfo, string fpSchedule) getFilepaths(string filename, string experimentName)
        {
            string pathToStoreOutput = Environment.GetEnvironmentVariable("OutputPath") + "\\" + experimentName;

            if (!Directory.Exists(pathToStoreOutput))
                Directory.CreateDirectory(pathToStoreOutput);

            string currOutputFilename = filename;
            string outputFilePath = pathToStoreOutput + "\\" + currOutputFilename + ".soln.info";
            string outputFilePath2 = pathToStoreOutput + "\\" + currOutputFilename + ".soln";

            int i = 2;
            if (File.Exists(outputFilePath) || File.Exists(outputFilePath2))
            {
                while (File.Exists(pathToStoreOutput + "\\" + currOutputFilename + i + ".soln") || File.Exists(pathToStoreOutput + "\\" + currOutputFilename + i + ".soln.info"))
                    i++;
                outputFilePath = pathToStoreOutput + "\\" + currOutputFilename + i + ".soln.info";
                outputFilePath2 = pathToStoreOutput + "\\" + currOutputFilename + i + ".soln";
            }
            return (outputFilePath, outputFilePath2);
        }


        public static void runLinearModels(string experimentName)
        {
            string path = Environment.GetEnvironmentVariable("ValidationDataPath");
            List<string> realLifeDataFileNames = new List<string>  { "p_18-80-80_2.max" };

            Console.WriteLine("Working on these problems:");
            foreach (string filename in realLifeDataFileNames)
                Console.Write(filename + "\t");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            foreach (string filename in realLifeDataFileNames)
            {
                (string outputFilePath, string outputFilePath2) = getFilepaths(filename, experimentName);
                ProblemInstance problem = new ProblemInstance(path + "\\" + filename);
                try
                {
                    LinearModel linearModel = new LinearModel(problem);
                    linearModel.createModel(1800, outputFilePath, outputFilePath2);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0} s", stopwatch.Elapsed);
        }
    }
}
