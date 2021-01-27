using Google.OrTools.LinearSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gurobi;

namespace CO1
{
    public class SingleMachineModel
    {
        private ProblemInstance problem;
        private List<int> schedule;
        private int machine;

        // Here the lowest job is 1; add 0 at the beginning 
        private List<int> scheduleShifted;
        private string solverType;


        public SingleMachineModel(ProblemInstance problem, List<int> schedule, int machine, string solverType = "gurobi")
        {
            this.problem = problem;
            this.schedule = schedule;
            this.solverType = solverType;
            this.machine = machine;

            this.scheduleShifted = new List<int>(schedule);
            for(int i = 0; i < scheduleShifted.Count; i++)
            {
                scheduleShifted[i] += 1;
            }
            scheduleShifted.Insert(0, 0);
        }

        public List<int> solveModel(int milliseconds, long tardinessBefore)
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

            setInitialValues(jobsInclDummy, solver, X, C, T);

            Console.WriteLine("Number of variables: " + solver.NumVariables());
            Console.WriteLine("Number of constraints: " + solver.NumConstraints());

            solver.SetTimeLimit(milliseconds);
            Solver.ResultStatus resultStatus = solver.Solve();

            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    if (X[i, j].SolutionValue() == 1)
                    {
                        Console.WriteLine("X_" + i + "," + j);
                    }
                }
            }

            Console.WriteLine("\n\nVariable Assignment:");
            for (int j = 1; j < jobsInclDummy; j++)
            {
                Console.WriteLine("C_" + j + ": " + C[j].SolutionValue());
                Console.WriteLine("T_" + j + ": " + T[j].SolutionValue());


            }


            // Check that the problem has an optimal solution.
            switch (resultStatus)
            {
                case (Solver.ResultStatus.OPTIMAL):
                    Console.WriteLine("Solution is optimal.");
                    break;
                case (Solver.ResultStatus.NOT_SOLVED):
                    Console.WriteLine("Not solved.");
                    break;
                default:
                    Console.WriteLine("Solution is not optimal.");
                    break;
            }

            List<int>[] machinesOrder = calculateMachineAsssignmentFromModel(jobsInclDummy, X);

            Console.WriteLine(String.Format("Single machine model tardiness {0} -> {1}", tardinessBefore, solver.Objective().Value()));

            return machinesOrder[0];
            
        }
        private void setInitialValues(int jobsInclDummy, Solver solver, Variable[,] X, Variable[] C, Variable[] T)
        {
            List<Variable> variables = new List<Variable>();
            List<Double> initialValue = new List<double>();

            double checkTardiness = 0;


            long currTimeOnMachine = 0;
            long tardiness = 0;

            currTimeOnMachine += problem.getSetupTimeForJob(0, schedule[0] + 1, machine);
            currTimeOnMachine += problem.processingTimes[schedule[0], machine];
            tardiness += (currTimeOnMachine - problem.dueDates[schedule[0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[0]] : 0;

            variables.Add(C[1]);
            initialValue.Add(currTimeOnMachine);
            variables.Add(T[1]);
            initialValue.Add(tardiness);
            checkTardiness += tardiness;
            for (int i = 1; i < schedule.Count; i++)
            {
                currTimeOnMachine += problem.getSetupTimeForJob(schedule[i - 1] + 1, schedule[i] + 1, machine);
                currTimeOnMachine += problem.processingTimes[schedule[i], machine];
                tardiness = (currTimeOnMachine - problem.dueDates[schedule[i]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[i]] : 0;
                variables.Add(C[i+1]);
                initialValue.Add(currTimeOnMachine);
                variables.Add(T[i+1]);
                initialValue.Add(tardiness);

                checkTardiness += tardiness;
            }
            Console.WriteLine("Check tardiness: " + checkTardiness.ToString());

            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    if (i < j && (i + 1) == j)
                    {
                        variables.Add(X[i, j]);
                        initialValue.Add(1);
                    }
                    else
                    {
                        variables.Add(X[i, j]);
                        initialValue.Add(0);
                    }
                }
            }

            

            solver.SetHint(new MPVariableVector(variables), initialValue.ToArray());

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
            // Constraint (13)
            for(int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr leftside = new LinearExpr();
                for (int i = 0; i < jobsInclDummy; i++)
                {
                    if (i != j)
                        leftside += X[i, j];
                }
                solver.Add(leftside == 1);
            }

            // Constraint (3)
            for(int i = 1; i < jobsInclDummy; i++)
            {
                LinearExpr leftside = new LinearExpr();
                for (int j = 0; j < jobsInclDummy; j++)
                {   
                    if(i != j)
                        leftside += X[i, j];
                }
                solver.Add(leftside == 1);
            }


            // Constraint (5)
            for(int j = 1; j < jobsInclDummy; j++)
            {
                LinearExpr leftside = new LinearExpr();
                LinearExpr rightside = new LinearExpr();
                for(int i = 0; i < jobsInclDummy; i++)
                {
                    if (i == j)
                        continue;

                    leftside += X[i, j];
                    rightside += X[j, i];
                }
                solver.Add(leftside == rightside);
            }

            // Constraint (28)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 1; j < jobsInclDummy; j++)
                {
                    LinearExpr r2 = X[scheduleShifted[i], scheduleShifted[j]] - 1;
                    r2 *= V;

                    r2 += problem.s[scheduleShifted[i], scheduleShifted[j], machine] + problem.processingTimes[scheduleShifted[j] - 1, machine];

                    solver.Add(C[j] >= C[i] + r2);
                }
            }

            // Constraint (19)
            LinearExpr lhs = new LinearExpr();
            for (int j = 1; j < jobsInclDummy; j++)
                lhs += X[0, j];

            solver.Add(lhs == 1.0);

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                solver.Add(T[j] >= C[j] - problem.dueDates[scheduleShifted[j]-1]);
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
