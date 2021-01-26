using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public class SingleMachineModel
    {
        private ProblemInstance problem;
        private List<int> schedule;
        private string solverType;

        public SingleMachineModel(ProblemInstance problem, List<int> schedule, string solverType = "gurobi")
        {
            this.problem = problem;
            this.schedule = schedule;
            this.solverType = solverType;
        }

        public void createModel(int milliseconds, long tardinessBefore)
        {
            int jobsInclDummy = schedule.Count + 1;
            Solver solver = Solver.CreateSolver(solverType);

            Variable[,] X = new Variable[jobsInclDummy, jobsInclDummy];
            Variable[] C = new Variable[jobsInclDummy];
            Variable[] T = new Variable[jobsInclDummy];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, solver, X, C, T);
            setConstraints(jobsInclDummy, solver, X, C, T, V);
            setFunctionToMinimize(jobsInclDummy, solver, T);

            Console.WriteLine("Number of variables: " + solver.NumVariables());
            Console.WriteLine("Number of constraints: " + solver.NumConstraints());

            solver.SetTimeLimit(milliseconds);
            Solver.ResultStatus resultStatus = solver.Solve();

            List<int>[] machinesOrder = calculateMachineAsssignmentFromModel(jobsInclDummy, X);

            Console.WriteLine(String.Format("Single machine model tardiness {0} -> {1}", tardinessBefore, solver.Objective().Value()));
            
        }

        private void initializeVariables(int jobsInclDummy, Solver solver, Variable[,] X, Variable[] C, Variable[] T)
        {
            // CONSTRAINT (24)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    X[i, j] = solver.MakeIntVar(0.0, 1.0, "X_" + i.ToString() + "," + j.ToString());
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
        }

        private void setConstraints(int jobsInclDummy, Solver solver, Variable[,] X, Variable[] C, Variable[] T, int V)
        {
            // Constraint (28)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 1; j < jobsInclDummy; j++)
                {
                    LinearExpr r2 = new LinearExpr();
                    r2 += X[i, j];
                    r2 -= 1.0;
                    r2 *= V;

                    LinearExpr r1 = new LinearExpr();
                    for (int m = 0; m < problem.machines; m++)
                    {
                        r1 += X[i, j] * (problem.s[i, j, m] + problem.processingTimes[j - 1, m]);
                    }

                    solver.Add(C[j] >= C[i] + r1 + r2);
                }
            }

            // Constraint (19)
            LinearExpr lhs = new LinearExpr();
            for (int j = 1; j < jobsInclDummy; j++)
                lhs += X[0, j];

            solver.Add(lhs <= 1.0);

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                solver.Add(T[j] >= C[j] - problem.dueDates[j - 1]);
            }

            // Constraint (23)
            solver.Add(C[0] == 0);
        }

        private void setFunctionToMinimize(int jobsInclDummy, Solver solver, Variable[] T)
        {
            // MINIMISE (13)
            LinearExpr functionToMinimise = T[1];
            for (int i = 2; i < jobsInclDummy; i++)
            {
                functionToMinimise += T[i];
            }
            solver.Minimize(functionToMinimise);
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, Variable[,] X)
        {
            List<int> machinesOrder = new List<int>() { 0 };

            int? foo = Helpers.getSuccessorJobSingleMachine(machinesOrder.Last(), jobsInclDummy, X);
            while (foo != null)
            {
                machinesOrder.Add((int)foo);
                foo = Helpers.getSuccessorJobSingleMachine(machinesOrder.Last(), jobsInclDummy, X);
            }

            // Create the same format as the original outputs
            machinesOrder.Remove(0);
            for (int i = 0; i < machinesOrder.Count; i++)
                machinesOrder[i] = machinesOrder[i] - 1;
            

            return new List<int>[] { machinesOrder};
        }
    }
}
