    using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CO1
{
    class Program
    {

        static void Main()
        {
            Console.WriteLine("Start");

            //string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\real-life";
            //List<string> realLifeDataFileNames = new List<string> { "A-fixed - Kopie.max" };

            string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";
            List<string> realLifeDataFileNames = new List<string> { "p_9-180-180_1.max", "t_3-12-200_1.max", "p_15-60-60_1.max", "p_18-80-80_2.max", "p_3-17-20_1.max", "s_1-3-100_1.max", };
            //List<string> realLifeDataFileNames = new List<string> { "t_3-12-200_1.max" };

            Console.WriteLine("Working on these problems:");
            foreach (string filename in realLifeDataFileNames)
                Console.Write(filename + "\t");


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string filename in realLifeDataFileNames)
            {
                string pathToStoreOutput = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Experiments";

                string currOutputFilename = filename;
                string outputFilePath = pathToStoreOutput + "\\" + currOutputFilename + ".soln";
                int i = 2;
                if (File.Exists(outputFilePath))
                {
                    while (File.Exists(pathToStoreOutput + "\\" + currOutputFilename + i + ".soln"))
                        i++;
                    outputFilePath = pathToStoreOutput + "\\" + currOutputFilename + i + ".soln";
                }

                FileStream filestream = new FileStream(outputFilePath, FileMode.Create);
                var streamwriter = new StreamWriter(filestream);
                streamwriter.AutoFlush = true;
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);

                UPMS upms = new UPMS();
                try
                {
                    upms.setDoPrintInfo(true);
                    upms.loadData(path + "\\" + filename);
                    upms.createModel(72);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0} s", stopwatch.Elapsed);

            //loadAllTrainingData();
        }

        public static void loadAllTrainingData()
        {
            string[] filenames = Directory.GetFiles("C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\training");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string filename in filenames)
            {
                Console.WriteLine("Processing " + filename);

                UPMS upms = new UPMS();
                upms.setDoPrintInfo(false);
                upms.loadData(filename);
                upms.createModel(1);
            }
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0} s", stopwatch.Elapsed);
        }

    }
}
