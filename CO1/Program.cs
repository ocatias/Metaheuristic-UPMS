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

            string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";
            //List<string> realLifeDataFileNames = new List<string> { "A-fixed - Kopie.max" };
            //List<string> realLifeDataFileNames = new List<string> { "p_15-60-60_1.max", "p_18-80-80_2.max", "p_3-17-20_1.max", "s_1-3-100_1.max",  };
            List<string> realLifeDataFileNames = new List<string> { "p_9-180-180_1.max" };

           
            Console.WriteLine("Working on these problems:");
            foreach (string filename in realLifeDataFileNames)
                Console.Write(filename + "\t");


            //string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";
            //List<string> realLifeDataFileNames = new List<string> { "s_4-16-20_1.max" };


            ////List<string> realLifeDataFileNames = new List<string> { "A.max", "A-fixed.max" , "B.max", "B-fixed.max", "C.max", "C-assigned.max", "C-assigned-x2.max",
            ////"C-assigned-x4.max", "C-assigned-x8.max", "C-assigned-x16.max", "C-x2.max", "C-x4.max", "C-x8.max", "C-x16.max"};

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
                //try
                //{
                    upms.setDoPrintInfo(true);
                    upms.loadData(path + "\\" + filename);
                    upms.createModel(1);
                //}
                //catch (StackOverflowException e)
                //{
                //    Console.WriteLine("StackOverflow");
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //}
                
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
