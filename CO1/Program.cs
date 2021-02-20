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
            List<string> allFilesInDirectory = new List<string>() { "s_23-148-160_1.max" };
           



            //List<string> allFilesInDirectory = (List<string>)Directory.GetFiles(pathTraining).ToList().Where(x => x.Contains(".max")).ToList();
            //for (int i = 0; i < allFilesInDirectory.Count; i++)
            //    allFilesInDirectory[i] = allFilesInDirectory[i].Split("\\").Last();



            //allFilesInDirectory = allFilesInDirectory.OrderBy(x => Guid.NewGuid()).ToList();

            runVLNS(30, 1, pathTraining, allFilesInDirectory, "1802SingleMachineOpt");
            //runSimulatedAnnealing(60, 1, pathValidation, allFilesInDirectory, "2801CompareToMax(Seq)");
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
            //List<string> realLifeDataFileNames = new List<string> { "p_9-180-180_1.max", "t_3-12-200_1.max", "p_15-60-60_1.max", "p_18-80-80_2.max", "p_3-17-20_1.max", "s_1-3-100_1.max", };
            List<string> realLifeDataFileNames = new List<string> { "p_3-17-20_1.max" };

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
                    linearModel.createModel(30, outputFilePath, outputFilePath2);
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
