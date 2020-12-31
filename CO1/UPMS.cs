using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;

namespace CO1
{
    public class UPMS
    {
        private bool doPrintInfo = false;
        private bool doPrintDataLoad = false;

        public void setDoPrintInfo(bool doPrintInfo) { this.doPrintInfo = doPrintInfo; }

        private int machines;
        private int materialsAmount;

        // Number of jobs, excluding the dummy job
        private int jobs;
        private List<long> dueDates;
        private List<int> materials;

        private int[,] processingTimes;

        // Do not use setupTimes for what is denoted as s[i,j,m] in the formules, use s
        private int[,,] setupTimes;

        private int[,,] s;

        public void loadData(string path)
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
                        Console.Write(string.Format("{0} ", processingTimes[i,j]));
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
            for(int i = 0; i < jobs + 1; i++)
            {
                for(int j = 0; j < jobs + 1; j++)
                {
                    for(int m = 0; m < machines; m++)
                    {
                        int materialOfJobi = i > 0 ? materials[i - 1] : 0;
                        int materialOfJobj = j > 0 ? materials[j - 1] : 0;
                        s[j, i, m] = setupTimes[materialOfJobi, materialOfJobj, m];
                    }
                }
            }
        }

        private void initializeVariables(int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y)
        {
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

            for (int j = 0; j < jobsInclDummy; j++)
            {
                C[j] = solver.MakeIntVar(0.0, double.PositiveInfinity, "C_" + j.ToString());
            }

            // Constraint (22)
            // Be careful: in the model T(dummy job) is not defined
            for (int i = 1; i < jobsInclDummy; i++)
            {
                T[i] = solver.MakeIntVar(0.0, double.PositiveInfinity, "T_" + i.ToString());
            }

            // Constraint (25)
            for (int i = 1; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < machines; j++)
                {
                    Y[i, j] = solver.MakeIntVar(0.0, 1.0, "Y_" + i.ToString() + "," + j.ToString());
                }
            }
        }

        private void setConstraints(int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y, int V)
        {
            // Constraint (26)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr lhs = new LinearExpr();
                for (int m = 0; m < machines; m++)
                {
                    if (processingTimes[j - 1, m] >= 0)
                        lhs += Y[j, m];
                }
                solver.Add(lhs == 1.0);
            }

            // Constraint (27)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr lhs = new LinearExpr();
                bool foundASingleForbiddenMachine = false;
                for (int m = 0; m < machines; m++)
                {
                    if (processingTimes[j - 1, m] < 0)
                    {
                        foundASingleForbiddenMachine = true;
                        lhs += Y[j, m];
                    }
                }
                if (foundASingleForbiddenMachine)
                    solver.Add(lhs == 0.0);

            }

            // Constraint (16)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                for (int m = 0; m < machines; m++)
                {
                    LinearExpr lhs = new LinearExpr();
                    for (int i = 0; i < jobsInclDummy; i++)
                    {
                        if (i != j)
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
                    for (int j = 0; j < jobsInclDummy; j++)
                    {
                        if (i != j)
                        {
                            lhs += X[i, j, m];
                        }
                    }
                    solver.Add(lhs == Y[i, m]);
                }
            }

            // Constraint (28)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 1; j < jobsInclDummy; j++)
                {
                    LinearExpr r2 = new LinearExpr();
                    for (int m = 0; m < machines; m++)
                    {
                        r2 += X[i, j, m];
                    }
                    r2 -= 1.0;
                    r2 *= V;

                    LinearExpr r1 = new LinearExpr();
                    for (int m = 0; m < machines; m++)
                    {
                        r1 += X[i, j, m] * (s[i, j, m] + processingTimes[j - 1, m]);
                    }

                    solver.Add(C[j] >= C[i] + r1 + r2);
                }
            }

            // Constraint (19)
            for (int m = 0; m < machines; m++)
            {
                LinearExpr lhs = new LinearExpr();
                for (int j = 1; j < jobsInclDummy; j++)
                    lhs += X[0, j, m];

                solver.Add(lhs <= 1.0);
            }

            // Constraint (20)
            for (int m = 0; m < machines; m++)
            {
                LinearExpr[] lhs = new LinearExpr[jobsInclDummy + 1];
                LinearExpr lhsFinal = new LinearExpr();

                for (int i = 0; i < jobsInclDummy; i++)
                {
                    lhs[i] = new LinearExpr();
                    for (int j = 1; j < jobsInclDummy; j++)
                    {
                        if (i != j)
                        {
                            lhs[i] += s[i, j, m] * X[i, j, m];
                        }
                    }
                }
                lhs[jobsInclDummy] = new LinearExpr();
                for (int i = 1; i < jobsInclDummy; i++)
                {
                    lhs[jobsInclDummy] += processingTimes[i - 1, m] * Y[i, m] + s[i, 0, m] * X[i, 0, m];

                }
                LinearExpr b = new LinearExpr();
                for (int o = 0; o < jobsInclDummy + 1; o++)
                    b += lhs[o];

                solver.Add(b <= Cmax);
            }

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                solver.Add(T[j] >= C[j] - dueDates[j - 1]);
            }

            // Constraint (23)
            solver.Add(C[0] == 0);
        }

        private void setFunctionToMinimize(int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y, int V)
        {
            // MINIMISE (13)
            LinearExpr functionToMinimise = T[1] * V;
            for (int i = 2; i < jobsInclDummy; i++)
            {
                functionToMinimise += T[i] * V;
            }
            functionToMinimise += Cmax;
            solver.Minimize(functionToMinimise);
        }

        private void checkModelNumberOfConstraintsVariables(Solver solver)
        {
            // |X_ijm| + |C_jm| + |T_j| + 1 (C_max) + |Y_jm|
            // Does not include V
            long numberOfVariables = ((jobs + 1) * (jobs + 1) * machines) + (jobs + 1) + jobs + 1 + jobs * machines;

            if (solver.NumVariables() != numberOfVariables)
                throw new Exception("Number of variables in model is wrong.");

            // [26] jobs +  [27] jobs + [16] jobs*machines + [17] jobs*machines + [28] (jobs + 1)*jobs + [19] machines 
            // [20] machines + [21] jobs + [22] 0 + [23] 1
            long numberOfConstraints = jobs * 2 + jobs * machines * 2 + (jobs + 1) * jobs + machines * 2 + jobs + 1;

            if (machines == 1)
                numberOfConstraints -= jobs;

            // ToDo: Fix this
            //if (solver.NumConstraints() != numberOfConstraints)
            //    throw new Exception("Number of constraints in model is wrong.");
        }

        private int? getSuccessorJob(int predecessor, int machine, int jobsInclDummy, Variable[,,] X)
        {
            for(int j = 1; j < jobsInclDummy; j++)
            {
                if (X[predecessor, j, machine].SolutionValue() == 1)
                    return j;
            }

            return null;
        }

        private (long, long) getTardinessMakeSpanFromModel(int jobsInclDummy, Variable[] T, Variable Cmax)
        {
            long tardiness = 0;

            for (int j = 1; j < jobsInclDummy; j++)
            {
                tardiness += (long)T[j].SolutionValue();
            }
            return (tardiness, (long)Cmax.SolutionValue());
        }

        private void outputModelSolutions(Solver.ResultStatus resultStatus, long tardinessFromModel, int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y, List<int>[] machinesOrder)
        {
            // Check that the problem has an optimal solution.
            switch (resultStatus)
            {
                case (Solver.ResultStatus.OPTIMAL):
                    Console.WriteLine("Solution is optimal.");
                    break;
                case (Solver.ResultStatus.NOT_SOLVED):
                    Console.WriteLine("Not solved.");
                    return;
                default:
                    Console.WriteLine("Solution is not optimal.");
                    break;
            }

            Console.WriteLine("\nTardiness: " + tardinessFromModel);
            Console.WriteLine("Makespan: " + Cmax.SolutionValue().ToString());

            Console.WriteLine("\nMachine order:");
            for (int m = 0; m < machines; m++)
            {
                Console.Write(m + ": ");
                foreach (int succ in machinesOrder[m])
                {
                    Console.Write(succ);
                    if (machinesOrder[m].IndexOf(succ) != machinesOrder[m].Count - 1)
                        Console.Write(", ");
                }
                Console.Write("\n");
            }


            Console.WriteLine("\n\n\n\n\n\nVariable Assignment:");
            for (int j = 1; j < jobsInclDummy; j++)
            {
                Console.WriteLine("T_" + j + ": " + T[j].SolutionValue());
                Console.WriteLine("C_" + j + ": " + C[j].SolutionValue());
                Console.WriteLine("d_" + j + ": " + dueDates[j - 1]);

            }

            for (int j = 1; j < jobsInclDummy; j++)
            {
                for (int m = 0; m < machines; m++)
                {
                    if (Y[j, m].SolutionValue() == 1)
                        Console.WriteLine("Y_" + j + "," + m);
                }
            }

            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    for (int m = 0; m < machines; m++)
                    {
                        if (X[i, j, m].SolutionValue() == 1)
                        {
                            Console.WriteLine("X_" + i + "," + j + "," + m);
                        }
                    }
                }
            }
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, Variable[,,] X)
        {
            List<int>[] machinesOrder = new List<int>[machines];
            for (int m = 0; m < machines; m++)
                machinesOrder[m] = new List<int> { 0 };

            for (int m = 0; m < machines; m++)
            {
                int? foo = getSuccessorJob(machinesOrder[m].Last(), m, jobsInclDummy, X);
                while (foo != null)
                {
                    machinesOrder[m].Add((int)foo);
                    foo = getSuccessorJob(machinesOrder[m].Last(), m, jobsInclDummy, X);
                }

                // Create the same format as the original outputs
                machinesOrder[m].Remove(0);
                for (int i = 0; i < machinesOrder[m].Count; i++)
                    machinesOrder[m][i] = machinesOrder[m][i] - 1;
            }

            return machinesOrder;
        }

        // here: job 0 is a dummy job
        private int getSetupTimeForJob(int jobBefore, int jobAfter, int machine)
        {
            int materialBefore = (jobBefore == 0) ? 0 : jobBefore;
            int materialAfter = (jobAfter == 0) ? 0 : jobAfter;

            return s[materialBefore, materialAfter, machine];
        }

        public (long, long) calculateTardMakeSpanFromMachineAssignment(List<int>[] machinesOrder)
        {
            long tardiness = 0;
            long maxMakeSpan = 0;
            for (int m = 0; m < machines; m++)
            {
                long currMakeSpan = 0, currTimeOnMachine = 0;

                if (machinesOrder[m].Count == 0)
                    continue;

                currMakeSpan += getSetupTimeForJob(0, machinesOrder[m][0] + 1, m);
                currMakeSpan += processingTimes[machinesOrder[m][0], m];


                currTimeOnMachine += getSetupTimeForJob(0, machinesOrder[m][0] + 1, m);
                currTimeOnMachine += processingTimes[machinesOrder[m][0], m];
                tardiness += (currTimeOnMachine - dueDates[machinesOrder[m][0]]) > 0 ? currTimeOnMachine - dueDates[machinesOrder[m][0]] : 0;

                for (int i = 1; i < machinesOrder[m].Count; i++)
                {
                    currMakeSpan += getSetupTimeForJob(machinesOrder[m][i-1] + 1, machinesOrder[m][i] + 1, m);
                    currMakeSpan += processingTimes[machinesOrder[m][i], m];

                    currTimeOnMachine += getSetupTimeForJob(machinesOrder[m][i-1] + 1, machinesOrder[m][i] + 1, m);
                    currTimeOnMachine += processingTimes[machinesOrder[m][i], m];
                    tardiness += (currTimeOnMachine - dueDates[machinesOrder[m][i]]) > 0 ? currTimeOnMachine - dueDates[machinesOrder[m][i]] : 0;
                }

                currMakeSpan += getSetupTimeForJob(machinesOrder[m][machinesOrder[m].Count - 1] + 1, 0, m);

                if (currMakeSpan > maxMakeSpan)
                    maxMakeSpan = currMakeSpan;
            }
            return (tardiness, maxMakeSpan);
        }

        public void verifyModelSolution(long tardinessFromModel, long makeSpanFromModel, List<int>[] machinesOrder)
        {
            // Verify if the assignment to machines is correct
            for(int m = 0; m < machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    if (processingTimes[job, m] < 0)
                        throw new Exception("Job assigned to not eligible machine.");
            }

            // Verify if each job is assigned exactly once
            int[] jobs = new int[this.jobs];
            Array.Clear(jobs, 0, jobs.Length);

            for(int m = 0; m < machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    jobs[job] += 1;
            }

            for(int i = 0; i < this.jobs; i++)
            {
                if (jobs[i] != 1)
                    throw new Exception("A job is not assigned or assigned multiple times");
            }

            // Verify tardiness and makespan
            long tardiness, makeSpan;
            (tardiness, makeSpan) = calculateTardMakeSpanFromMachineAssignment(machinesOrder);

            if (tardiness > tardinessFromModel)
                throw new Exception("Tardiness from model is contradictory");
            else if (tardiness < tardinessFromModel)
                Console.WriteLine("Tardiness could be selected smaller.");

            if (makeSpan > makeSpanFromModel)
                throw new Exception("Makespan from model is contradictory");
            else if (makeSpan < makeSpanFromModel)
                Console.WriteLine("Makespan could be selected smaller.");

            Console.WriteLine("Solution verified.");
        }

        // Data needs to be loaded before calling createModel()
        public void createModel(int seconds)
        {
            Console.WriteLine("Solver runtime: " + seconds + " sec");

            int jobsInclDummy = this.jobs + 1;
            Solver solver = Solver.CreateSolver("gurobi");

            Variable[,,] X = new Variable[jobsInclDummy, jobsInclDummy, machines];
            Variable[] C = new Variable[jobsInclDummy];
            Variable[] T = new Variable[jobsInclDummy];
            Variable Cmax = solver.MakeIntVar(0.0, double.PositiveInfinity, "Cmax");
            Variable[,] Y = new Variable[jobsInclDummy, machines];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, solver, X, C, T, Cmax, Y);
            setConstraints(jobsInclDummy, solver, X, C, T, Cmax, Y, V);
            setFunctionToMinimize(jobsInclDummy, solver, X, C, T, Cmax, Y, V);

            checkModelNumberOfConstraintsVariables(solver);

            Console.WriteLine("Number of variables: " + solver.NumVariables());
            Console.WriteLine("Number of constraints: " + solver.NumConstraints());
    
            solver.SetTimeLimit(1000 * seconds);
            Solver.ResultStatus resultStatus = solver.Solve();

            List<int>[] machinesOrder = calculateMachineAsssignmentFromModel(jobsInclDummy, X);
            (long tardiness, long makespan) = getTardinessMakeSpanFromModel(jobsInclDummy, T, Cmax);

            // Verify
            verifyModelSolution(tardiness, makespan, machinesOrder);

            // Output information
            outputModelSolutions(resultStatus, tardiness, jobsInclDummy, solver, X, C, T, Cmax, Y, machinesOrder);

            
        }
    }
}
