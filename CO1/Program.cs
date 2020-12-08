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
            string path = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\real-life";
            List<string> realLifeDataFileNames = new List<string> { "A-fixed.max" };

            //List<string> realLifeDataFileNames = new List<string> { "A.max", "A-fixed.max" , "B.max", "B-fixed.max", "C.max", "C-assigned.max", "C-assigned-x2.max",
            //"C-assigned-x4.max", "C-assigned-x8.max", "C-assigned-x16.max", "C-x2.max", "C-x4.max", "C-x8.max", "C-x16.max"};

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string filename in realLifeDataFileNames)
            {
                UPMS upms = new UPMS();
                upms.setDoPrintInfo(true);
                upms.loadData(path + "//" + filename);
                upms.createModel();
            }
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0} s", stopwatch.Elapsed);
        }

        public void loadAllTrainingData()
        {
            string[] filenames = Directory.GetFiles("C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\training");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (string filename in filenames)
            {
                UPMS upms = new UPMS();
                upms.setDoPrintInfo(false);
                upms.loadData(filename);
                upms.createModel();
            }
            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0} s", stopwatch.Elapsed);
        }

    }
}
