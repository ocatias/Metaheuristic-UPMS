using System;
using System.Collections.Generic;
using Google.OrTools.LinearSolver;

namespace CO1
{
    public class UPMS
    {
        private bool doPrintInfo = false;

        public void setDoPrintInfo(bool doPrintInfo) { this.doPrintInfo = doPrintInfo; }


        private int machines;
        private int materialsAmount;

        // Number of jobs, excluding the dummy job
        private int jobs;
        private List<long> dueDates;
        private List<int> materials;

        private int[,] processingTimes;
        private int[,,] setupTimes;

        public void loadData(string path)
        {
            // Read 
            string text = System.IO.File.ReadAllText(path);

            List<string> parts = new List<string>(text.Split("\n"));

            parts = parts.FindAll(x => x.Trim() != "");

            machines = Int32.Parse(parts[0].Split(": ")[1]);
            materialsAmount = Int32.Parse(parts[1].Split(": ")[1]);
            jobs = Int32.Parse(parts[2].Split(": ")[1]);


            List<string> dueDatesString = new List<string>(parts[4].Split("\t"));
            dueDatesString = dueDatesString.FindAll(x => x.Trim() != "");

            dueDates = new List<long>();

            foreach (string dueDateString in dueDatesString)
                dueDates.Add(long.Parse(dueDateString));

            List<string> materialsString = new List<string>(parts[6].Split("\t"));
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
                string[] processingTimesLine = parts[processingTimesStartLine + currLine].Split("\t");
                for (int currMachine = 0; currMachine < machines; currMachine++)
                {
                    processingTimes[currLine, currMachine] = Int32.Parse(processingTimesLine[currMachine]);
                }
                if (machines < processingTimesLine.Length)
                    throw new Exception("Found more processing times for a job than number of machines");
            }

            if (doPrintInfo)
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
                        Console.Write(string.Format("{0} ", processingTimes[i,j]));
                    Console.WriteLine("");
                }

                Console.WriteLine("\nSetup Times:");
            }

            int setupTimesStartLine = processingTimesStartLine + jobs + 2;

            setupTimes = new int[machines, materialsAmount + 1, materialsAmount + 1];
            for (int currMachine = 0; currMachine < machines; currMachine++)
            {
                for (int currMaterial = 0; currMaterial < materialsAmount + 1; currMaterial++)
                {
                    string[] setupTimeLine = parts[setupTimesStartLine + (materialsAmount + 2) * currMachine + currMaterial].Split("\t");
                    for (int currMaterial2 = 0; currMaterial2 < materialsAmount + 1; currMaterial2++)
                        setupTimes[currMachine, currMaterial2, currMaterial] = Int32.Parse(setupTimeLine[currMaterial2]);
                }

                if (doPrintInfo)
                {
                    Console.WriteLine("\nMachine " + currMachine + ":");
                    for (int i = 0; i < materialsAmount + 1; i++)
                    {
                        for (int j = 0; j < materialsAmount + 1; j++)
                            Console.Write(string.Format("{0} ", setupTimes[0, j, i]));
                        Console.WriteLine("");
                    }
                }
            }
        }

        // Data needs to be loaded before calling createModel()
        public void createModel()
        {
            int jobsInclDummy = this.jobs + 1;
            Solver solver = Solver.CreateSolver("SCIP");


            // Create variables for model and some constraints
            Variable[,,] X = new Variable[jobsInclDummy, jobsInclDummy, machines];

            // CONSTRAINT (24)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    for (int k = 0; k < machines; k++)
                    {
                        X[i, j, k] = solver.MakeIntVar(0.0, 1.0, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                    }
                }
            }

            //Variable[,] C = new Variable[jobsInclDummy, machines];
            //for (int i = 0; i < jobsInclDummy; i++)
            //{
            //    for (int j = 0; j < machines; j++)
            //    {
            //        C[i, j] = solver.MakeNumVar(0.0, double.PositiveInfinity, "C_" + i.ToString() + "," + j.ToString());
            //    }
            //}

            Variable[] C = new Variable[jobsInclDummy];
            for (int j = 0; j < jobsInclDummy; j++)
            {
                C[j] = solver.MakeNumVar(0.0, double.PositiveInfinity, "C_" + j.ToString());
            }
            


            // Be careful: in the model T(dummy job) is not defined
            Variable[] T = new Variable[jobsInclDummy];
            for (int i = 1; i < jobsInclDummy; i++)
            {
                T[i] = solver.MakeNumVar(0.0, double.PositiveInfinity, "T_" + i.ToString());
            }

            Variable Cmax = solver.MakeNumVar(0.0, double.PositiveInfinity, "Cmax");


            // ToDo = Probably need a bigger and not fixed value
            int V = 1000000;

            // Be careful: in the model Y(dummy job, machine) is not defined
            Variable[,] Y = new Variable[jobsInclDummy, machines];

            // CONSTRAINT (25)
            for (int i = 1; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < machines; j++)
                {
                    Y[i, j] = solver.MakeIntVar(0.0, 1.0, "Y_" + i.ToString() + "," + j.ToString());
                }
            }


            // ToDo: Add the lex and include Cmax
            // MINIMISE (13)
            LinearExpr functionToMinimise = T[1];
            for (int i = 2; i < jobsInclDummy; i++)
            {
                functionToMinimise += T[i];
            }
            solver.Minimize(functionToMinimise);

            // CONSTRAINT (26)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr lhs = new LinearExpr();
                for(int m = 0; m < machines; m++)
                {
                    if (processingTimes[j-1,m] >= 0)
                        lhs += Y[j,m];
                }
                solver.Add(lhs == 1.0);
            }

            // Constraint (27)
            for(int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr lhs = new LinearExpr();
                for(int m = 0; m < machines; m++)
                {
                    if (processingTimes[j - 1, m] < 0)
                        lhs += Y[j, m];
                }
                solver.Add(lhs == 0.0);

            }

            // Constraint (16)
            for(int j = 1; j < jobsInclDummy; j++)
            {
                for(int m = 0; m < machines; m++)
                {
                    LinearExpr lhs = new LinearExpr();
                    for(int i = 0; i < jobsInclDummy; i++)
                    {
                        if(i != j)
                        {
                            lhs += X[i, j, m];
                        }
                    }
                    solver.Add(lhs == Y[j, m]);
                }
            }

            // Constraint 17
            for (int i = 1; i < jobsInclDummy; i++)
            {
                for (int m = 0; m < machines; m++)
                {
                    LinearExpr lhs = new LinearExpr();
                    for(int j = 0; j < jobsInclDummy; j++)
                    {
                        if(i != j)
                        {
                            lhs += X[i, j, m];
                        }
                    }
                    solver.Add(lhs == Y[i, m]);
                }
            }

            // Constraint (28)
            for(int i = 0; i < jobsInclDummy; i ++)
            {
                for(int j = 1; j < jobsInclDummy; j++)
                {
                    LinearExpr rhs = new LinearExpr();
                    rhs = C[i];
                    for(int m = 0; m < machines; m++)
                    {
                        rhs += X[i, j, m] * (setupTimes[i, j, m] + processingTimes[j-1, m]);
                        rhs += V * X[i, j, m];
                    }
                    // This is weird, check again
                    rhs -= V;

                    solver.Add(C[j] >= rhs);
                }
            }


            // |X_ijm| + |C_jm| + |T_j| + 1 (C_max) + |Y_jm|
            // Does not include V
            long numberOfVariables = ((jobs + 1) * (jobs + 1) * machines) + (jobs+1) + jobs + 1 + jobs * machines;

            if (solver.NumVariables() != numberOfVariables)
                throw new Exception("Number of variables in model is wrong.");

            // [26] jobs +  [27] jobs + [16] jobs*machines + [17] jobs*machines + [28] (jobs + 1)*jobs
            long numberOfConstraints = jobs*2 + jobs*machines*2 + (jobs + 1) * jobs;

            if(solver.NumConstraints() != numberOfConstraints)
                throw new Exception("Number of constraints in model is wrong.");


            if (doPrintInfo)
            {
                Console.WriteLine("Number of variables: " + solver.NumVariables());
                Console.WriteLine("Should be: " + numberOfVariables);

                Console.WriteLine("Number of constraints: " + solver.NumConstraints());
                Console.WriteLine("Should be: " + numberOfConstraints);
            }

            solver.Solve();

            Console.WriteLine("Solution:");

            for(int j = 1; j < jobsInclDummy; j++)
                Console.WriteLine("T_" + j + ": " + T[j].SolutionValue());
        }
    }
}
