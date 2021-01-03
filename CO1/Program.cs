﻿    using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CO1
{
    class Program
    {

        static void Main()
        {
            runSimulatedAnnealing();
        }

        public static void runSimulatedAnnealing()
        {
            string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";
            List<string> realLifeDataFileNames = new List<string> { "s_1-3-100_1.max" };

            foreach (string filename in realLifeDataFileNames)
            {
                ProblemInstance problem = new ProblemInstance(path + "\\" + filename);
                (string fpInfo, string fpSchedule) = getFilepaths(filename);

                SimulatedAnnealingSolver solver = new SimulatedAnnealingSolver(problem);
                solver.solve(60, fpSchedule);
            }
        }

        public static (string fpInfo, string fpSchedule) getFilepaths(string filename)
        {
            string pathToStoreOutput = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Experiments";

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


        public static void runLinearModels()
        {
            string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";
            //List<string> realLifeDataFileNames = new List<string> { "p_9-180-180_1.max", "t_3-12-200_1.max", "p_15-60-60_1.max", "p_18-80-80_2.max", "p_3-17-20_1.max", "s_1-3-100_1.max", };
            List<string> realLifeDataFileNames = new List<string> { "p_3-17-20_1.max" };

            Console.WriteLine("Working on these problems:");
            foreach (string filename in realLifeDataFileNames)
                Console.Write(filename + "\t");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string filename in realLifeDataFileNames)
            {
                (string outputFilePath, string outputFilePath2) = getFilepaths(filename);
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
