using System;
using System.Collections.Generic;
using System.Text;


// A problem instance of UPMS
namespace CO1
{
    public class ProblemInstance
    {
        private bool doPrintInfo = false;
        private bool doPrintDataLoad = false;

        public int machines;
        public int materialsAmount;

        // Number of jobs, excluding the dummy job
        public int jobs;
        public List<long> dueDates;
        public List<int> materials;

        public int[,] processingTimes;

        // Do not use setupTimes for what is denoted as s[i,j,m] in the formules, use s
        private int[,,] setupTimes;

        public int[,,] s;

        public ProblemInstance(string path)
        {
            if (doPrintInfo)
                Console.WriteLine("Loading: " + path);

            // Read 
            string text = System.IO.File.ReadAllText(path);

            List<string> parts = new List<string>(text.Split("\n"));

            parts = parts.FindAll(x => x.Trim() != "");

            machines = Int32.Parse(parts[0].Split(": ")[1]);
            materialsAmount = Int32.Parse(parts[1].Split(": ")[1]);
            jobs = Int32.Parse(parts[2].Split(": ")[1]);


            List<string> dueDatesString = new List<string>(parts[4].Replace(" ", "\t").Split("\t"));
            dueDatesString = dueDatesString.FindAll(x => x.Trim() != "");

            dueDates = new List<long>();

            foreach (string dueDateString in dueDatesString)
                dueDates.Add(long.Parse(dueDateString));

            List<string> materialsString = new List<string>(parts[6].Replace(" ", "\t").Split("\t"));
            materials = new List<int>();

            foreach (string materialString in materialsString)
                materials.Add(Int32.Parse(materialString));

            if (dueDates.Count != jobs)
                throw new Exception("Number of due dates != number of jobs");

            if (materials.Count != jobs)
                throw new Exception("Materials assigned to jobs != number of jobs");

            int processingTimesStartLine = 8;

            processingTimes = new int[jobs, machines];

            for (int currLine = 0; currLine < jobs; currLine++)
            {
                string[] processingTimesLine = parts[processingTimesStartLine + currLine].Replace(" ", "\t").Split("\t");

                for (int currMachine = 0; currMachine < machines; currMachine++)
                {
                    processingTimes[currLine, currMachine] = Int32.Parse(processingTimesLine[currMachine]);
                }
                if (machines < processingTimesLine.Length)
                    throw new Exception("Found more processing times for a job than number of machines");
            }

            if (doPrintDataLoad)
            {
                Console.WriteLine("Machines: " + machines.ToString());
                Console.WriteLine("Materials: " + materialsAmount.ToString());
                Console.WriteLine("Jobs: " + jobs.ToString());

                Console.WriteLine("\nDue Dates: ");
                foreach (long dueDate in dueDates)
                    Console.Write(dueDate + "\t");

                Console.WriteLine("\n\nMaterials: ");
                foreach (int material in materials)
                    Console.Write(material + "\t");

                Console.WriteLine("\nProcessing Times:");
                for (int i = 0; i < jobs; i++)
                {
                    for (int j = 0; j < machines; j++)
                        Console.Write(string.Format("{0} ", processingTimes[i, j]));
                    Console.WriteLine("");
                }

                Console.WriteLine("\nSetup Times:");
            }

            int setupTimesStartLine = processingTimesStartLine + jobs + 2;

            setupTimes = new int[materialsAmount + 1, materialsAmount + 1, machines];
            for (int currMachine = 0; currMachine < machines; currMachine++)
            {
                for (int currMaterial = 0; currMaterial < materialsAmount + 1; currMaterial++)
                {
                    string[] setupTimeLine = parts[setupTimesStartLine + (materialsAmount + 2) * currMachine + currMaterial].Replace(" ", "\t").Split("\t");
                    for (int currMaterial2 = 0; currMaterial2 < materialsAmount + 1; currMaterial2++)
                        setupTimes[currMaterial2, currMaterial, currMachine] = Int32.Parse(setupTimeLine[currMaterial2]);
                }

                if (doPrintDataLoad)
                {
                    Console.WriteLine("\nMachine " + currMachine + ":");
                    for (int i = 0; i < materialsAmount + 1; i++)
                    {
                        for (int j = 0; j < materialsAmount + 1; j++)
                            Console.Write(string.Format("{0} ", setupTimes[j, i, 0]));
                        Console.WriteLine("");
                    }
                }
            }

            s = new int[jobs + 1, jobs + 1, machines];
            for (int i = 0; i < jobs + 1; i++)
            {
                for (int j = 0; j < jobs + 1; j++)
                {
                    for (int m = 0; m < machines; m++)
                    {
                        int materialOfJobi = i > 0 ? materials[i - 1] : 0;
                        int materialOfJobj = j > 0 ? materials[j - 1] : 0;
                        s[j, i, m] = setupTimes[materialOfJobi, materialOfJobj, m];
                    }
                }
            }
        }

        // here: job 0 is a dummy job
        public int getSetupTimeForJob(int jobBefore, int jobAfter, int machine)
        {
            return s[jobBefore, jobAfter, machine];
        }

        public bool isFeasibleJobAssignment(int job, int machine)
        {
            return (processingTimes[job, machine] >= 0);
        }
    }
}
