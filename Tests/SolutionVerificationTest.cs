using CO1;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    public class SolutionVerificationTest
    {
        [SetUp]
        public void Setup()
        {
        }

        /*
         * This tests  the tardiness/makespan verification function on 800 instances is correct
         */

        [Test]
        public void Test1()
        {
            string pathToSolution = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\solutions\\validation-set-solutions\\sac-ch";
            string pathToInstance = "C:\\Users\\Fabian\\Desktop\\Informatik\\CO\\Data\\instances\\validation";

            //List<string> filenames = new List<string> { "perez_3-17-20_1_20", "perez_1-364-580_1_1" };
            List<string> filenames = new List<string>();
            foreach (string filenameWithPath in Directory.GetFiles(pathToSolution))
            {
                string filename = filenameWithPath.Split("\\").Last();
                if (!filename.Contains("info"))
                {
                    filenames.Add(filename.Split('.')[0]);
                }
            }

            Parallel.ForEach(filenames, (filename) =>
            //foreach(string filename in filenames)
            {
                string text = System.IO.File.ReadAllText(pathToSolution + "\\" + filename + ".soln");
                List<string> parts = new List<string>(text.Split("\n"));
                parts.RemoveAt(0);

                List<int>[] machineOrder = new List<int>[parts.Count];
                for (int i = 0; i < parts.Count; i++)
                {
                    machineOrder[i] = new List<int>();
                    string[] machines = parts[i].Split(';');
                    machines = machines.Where(m => m != "").ToArray();
                    for (int j = 1; j < machines.Length; j++)
                        machineOrder[i].Add(int.Parse(machines[j]));
                }

                var tempFN = filename.Split('_');
                string tempFN2 = "";
                for (int i = 0; i < tempFN.Length - 1; i++)
                {
                    if (i > 0)
                        tempFN2 += "_";
                    tempFN2 += tempFN[i];
                }
                string fn = tempFN2.Replace("erez", "").Replace("tandard", "").Replace("ight", "") + ".max";

                ProblemInstance problem = new ProblemInstance(pathToInstance + "\\" + fn);

                (long tardinessFromSolution, long makeSpanFromSolution) = getSolutionTardinessAndMakeSpan(pathToSolution + "\\" + filename + ".soln.info");
                Verifier.verifyModelSolution(problem, tardinessFromSolution, makeSpanFromSolution, machineOrder);
            });
        //}
        }

        public (long, long) getSolutionTardinessAndMakeSpan(string path)
        {
            string text = System.IO.File.ReadAllText(path);
            List<string> parts = new List<string>(text.Split("\n"));
            long tardiness = long.Parse(parts.First(p => p.Contains("Tardiness")).Split('=')[1]);
            long makeSpan = long.Parse(parts.First(p => p.Contains("Makespan")).Split('=')[1]);
            return (tardiness, makeSpan);
        }

    }
}