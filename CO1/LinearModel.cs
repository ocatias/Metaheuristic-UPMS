using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// A linear model of an UPMS instance
namespace CO1
{
    public class LinearModel
    {
        private ProblemInstance problem;
         
        public LinearModel(ProblemInstance problem)
        {
            this.problem = problem;
        }

        private void initializeVariables(int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y)
        {
            // CONSTRAINT (24)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    for (int k = 0; k < problem.machines; k++)
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
                for (int j = 0; j < problem.machines; j++)
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
                for (int m = 0; m < problem.machines; m++)
                {
                    if (problem.processingTimes[j - 1, m] >= 0)
                        lhs += Y[j, m];
                }
                solver.Add(lhs == 1.0);
            }

            // Constraint (27)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr lhs = new LinearExpr();
                bool foundASingleForbiddenMachine = false;
                for (int m = 0; m < problem.machines; m++)
                {
                    if (problem.processingTimes[j - 1, m] < 0)
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
                for (int m = 0; m < problem.machines; m++)
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
                for (int m = 0; m < problem.machines; m++)
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
                    for (int m = 0; m < problem.machines; m++)
                    {
                        r2 += X[i, j, m];
                    }
                    r2 -= 1.0;
                    r2 *= V;

                    LinearExpr r1 = new LinearExpr();
                    for (int m = 0; m < problem.machines; m++)
                    {
                        r1 += X[i, j, m] * (problem.s[i, j, m] + problem.processingTimes[j - 1, m]);
                    }

                    solver.Add(C[j] >= C[i] + r1 + r2);
                }
            }

            // Constraint (19)
            for (int m = 0; m < problem.machines; m++)
            {
                LinearExpr lhs = new LinearExpr();
                for (int j = 1; j < jobsInclDummy; j++)
                    lhs += X[0, j, m];

                solver.Add(lhs <= 1.0);
            }

            // Constraint (20)
            for (int m = 0; m < problem.machines; m++)
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
                            lhs[i] += problem.s[i, j, m] * X[i, j, m];
                        }
                    }
                }
                lhs[jobsInclDummy] = new LinearExpr();
                for (int i = 1; i < jobsInclDummy; i++)
                {
                    lhs[jobsInclDummy] += problem.processingTimes[i - 1, m] * Y[i, m] + problem.s[i, 0, m] * X[i, 0, m];

                }
                LinearExpr b = new LinearExpr();
                for (int o = 0; o < jobsInclDummy + 1; o++)
                    b += lhs[o];

                solver.Add(b <= Cmax);
            }

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                solver.Add(T[j] >= C[j] - problem.dueDates[j - 1]);
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
            long numberOfVariables = ((problem.jobs + 1) * (problem.jobs + 1) * problem.machines) + (problem.jobs + 1) + problem.jobs + 1 + problem.jobs * problem.machines;

            if (solver.NumVariables() != numberOfVariables)
                throw new Exception("Number of variables in model is wrong.");

            // [26] jobs +  [27] jobs + [16] jobs*machines + [17] jobs*machines + [28] (jobs + 1)*jobs + [19] machines 
            // [20] machines + [21] jobs + [22] 0 + [23] 1
            long numberOfConstraints = problem.jobs * 2 + problem.jobs * problem.machines * 2 + (problem.jobs + 1) * problem.jobs + problem.machines * 2 + problem.jobs + 1;

            if (problem.machines == 1)
                numberOfConstraints -= problem.jobs;

            // ToDo: Fix this
            //if (solver.NumConstraints() != numberOfConstraints)
            //    throw new Exception("Number of constraints in model is wrong.");
        }

        private int? getSuccessorJob(int predecessor, int machine, int jobsInclDummy, Variable[,,] X)
        {
            for (int j = 1; j < jobsInclDummy; j++)
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

        private void outputModelSolutions(StreamWriter outputFileSolInfo, Solver.ResultStatus resultStatus, long tardinessFromModel, int jobsInclDummy, Solver solver, Variable[,,] X, Variable[] C, Variable[] T, Variable Cmax, Variable[,] Y, List<int>[] machinesOrder)
        {
            // Check that the problem has an optimal solution.
            switch (resultStatus)
            {
                case (Solver.ResultStatus.OPTIMAL):
                    outputFileSolInfo.WriteLine("Solution is optimal.");
                    break;
                case (Solver.ResultStatus.NOT_SOLVED):
                    outputFileSolInfo.WriteLine("Not solved.");
                    return;
                default:
                    outputFileSolInfo.WriteLine("Solution is not optimal.");
                    break;
            }

            outputFileSolInfo.WriteLine("\nTardiness: " + tardinessFromModel);
            outputFileSolInfo.WriteLine("Makespan: " + Cmax.SolutionValue().ToString());

            outputFileSolInfo.WriteLine("\n\nVariable Assignment:");
            for (int j = 1; j < jobsInclDummy; j++)
            {
                outputFileSolInfo.WriteLine("T_" + j + ": " + T[j].SolutionValue());
                outputFileSolInfo.WriteLine("C_" + j + ": " + C[j].SolutionValue());
                outputFileSolInfo.WriteLine("d_" + j + ": " + problem.dueDates[j - 1]);

            }

            for (int j = 1; j < jobsInclDummy; j++)
            {
                for (int m = 0; m < problem.machines; m++)
                {
                    if (Y[j, m].SolutionValue() == 1)
                        outputFileSolInfo.WriteLine("Y_" + j + "," + m);
                }
            }

            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    for (int m = 0; m < problem.machines; m++)
                    {
                        if (X[i, j, m].SolutionValue() == 1)
                        {
                            outputFileSolInfo.WriteLine("X_" + i + "," + j + "," + m);
                        }
                    }
                }
            }
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, Variable[,,] X)
        {
            List<int>[] machinesOrder = new List<int>[problem.machines];
            for (int m = 0; m < problem.machines; m++)
                machinesOrder[m] = new List<int> { 0 };

            for (int m = 0; m < problem.machines; m++)
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

        // Data needs to be loaded before calling createModel()
        public void createModel(int seconds, string filepathSolInfo, string filepathSolMachineOrder)
        {
            StreamWriter outputFileSolInfo = new StreamWriter(filepathSolInfo);

            outputFileSolInfo.WriteLine("Solver runtime: " + seconds + " sec");

            int jobsInclDummy = this.problem.jobs + 1;
            Solver solver = Solver.CreateSolver("gurobi");

            Variable[,,] X = new Variable[jobsInclDummy, jobsInclDummy, problem.machines];
            Variable[] C = new Variable[jobsInclDummy];
            Variable[] T = new Variable[jobsInclDummy];
            Variable Cmax = solver.MakeIntVar(0.0, double.PositiveInfinity, "Cmax");
            Variable[,] Y = new Variable[jobsInclDummy, problem.machines];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, solver, X, C, T, Cmax, Y);
            setConstraints(jobsInclDummy, solver, X, C, T, Cmax, Y, V);
            setFunctionToMinimize(jobsInclDummy, solver, X, C, T, Cmax, Y, V);

            checkModelNumberOfConstraintsVariables(solver);

            outputFileSolInfo.WriteLine("Number of variables: " + solver.NumVariables());
            outputFileSolInfo.WriteLine("Number of constraints: " + solver.NumConstraints());

            solver.SetTimeLimit(1000 * seconds);
            Solver.ResultStatus resultStatus = solver.Solve();

            List<int>[] machinesOrder = calculateMachineAsssignmentFromModel(jobsInclDummy, X);
            (long tardiness, long makespan) = getTardinessMakeSpanFromModel(jobsInclDummy, T, Cmax);

            // Verify
            try
            {
                Verifier.verifyModelSolution(problem, tardiness, makespan, machinesOrder);
                outputFileSolInfo.WriteLine("Solution verified.");
            }
            catch(Exception e)
            {
                outputFileSolInfo.WriteLine("Verifier found a problem: " + e.Message);
            }                

            // Output information
            outputModelSolutions(outputFileSolInfo, resultStatus, tardiness, jobsInclDummy, solver, X, C, T, Cmax, Y, machinesOrder);

            ResultExport.storeMachineSchedule(filepathSolMachineOrder, problem, machinesOrder);
            outputFileSolInfo.Close();
        }
    }
}
