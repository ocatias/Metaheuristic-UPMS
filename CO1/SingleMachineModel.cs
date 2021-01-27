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

            // Create an empty environment, set options and start
            GRBEnv env = new GRBEnv(true);
            env.Start();

            // Create empty model
            GRBModel solver = new GRBModel(env);

            GRBVar[,] X = new GRBVar[jobsInclDummy, jobsInclDummy];
            GRBVar[] C = new GRBVar[jobsInclDummy];
            GRBVar[] T = new GRBVar[jobsInclDummy];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, solver, X, C, T);
            setConstraints(jobsInclDummy, solver, X, C, T, V);
            setFunctionToMinimize(jobsInclDummy, solver, T);

            setInitialValues(jobsInclDummy, solver, X, C, T);

            solver.Set("TimeLimit", (milliseconds/1000.0).ToString());
            solver.Set("OutputFlag", "0");


            solver.Optimize();

            List<int>[] machinesOrder = calculateMachineAsssignmentFromModel(jobsInclDummy, X);

            Console.WriteLine(String.Format("Single machine model tardiness {0} -> {1}", tardinessBefore, solver.ObjVal));

            List<int> scheduleToReturn = new List<int>();
            for(int i = 0;  i < machinesOrder[0].Count; i++)
            {
                scheduleToReturn.Add(schedule[machinesOrder[0][i]]);
            }

            return scheduleToReturn;
            
        }
        private void setInitialValues(int jobsInclDummy, GRBModel solver, GRBVar[,] X, GRBVar[] C, GRBVar[] T)
        {
            List<Variable> variables = new List<Variable>();
            List<Double> initialValue = new List<double>();

            long currTimeOnMachine = 0;
            long tardiness = 0;

            currTimeOnMachine += problem.getSetupTimeForJob(0, scheduleShifted[1], machine);
            currTimeOnMachine += problem.processingTimes[schedule[0], machine];
            tardiness += (currTimeOnMachine - problem.dueDates[schedule[0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[0]] : 0;

            C[1].Start = currTimeOnMachine;
            T[1].Start = tardiness;

            for (int i = 1; i < schedule.Count; i++)
            {
                currTimeOnMachine += problem.getSetupTimeForJob(scheduleShifted[i], scheduleShifted[i+1], machine);
                currTimeOnMachine += problem.processingTimes[schedule[i], machine];
                tardiness = (currTimeOnMachine - problem.dueDates[schedule[i]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[i]] : 0;
                C[i + 1].Start = currTimeOnMachine;
                T[i + 1].Start = tardiness;
            }

            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    if (i < j && (i + 1) == j)
                    {
                        X[i, j].Start = 1;
                    }
                    else
                    {
                        X[i, j].Start = 0;
                    }
                }
            }
            X[jobsInclDummy - 1, 0].Start = 1;
        }

        private void initializeVariables(int jobsInclDummy, GRBModel solver, GRBVar[,] X, GRBVar[] C, GRBVar[] T)
        {
            // CONSTRAINT (24)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    X[i, j] = solver.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString());
                }
            }

            for (int j = 0; j < jobsInclDummy; j++)
            {
                C[j] = solver.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "C_" + j.ToString());
            }

            // Constraint (22)
            // Be careful: in the model T(dummy job) is not defined
            for (int i = 1; i < jobsInclDummy; i++)
            {
                T[i] = solver.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "T_" + i.ToString());
            }
        }

        private void setConstraints(int jobsInclDummy, GRBModel solver, GRBVar[,] X, GRBVar[] C, GRBVar[] T, int V)
        {
            // Constraint (13)
            for(int j = 1; j < jobsInclDummy; j++)
            {
                GRBLinExpr leftside = new GRBLinExpr();
                for (int i = 0; i < jobsInclDummy; i++)
                {
                    if (i != j)
                        leftside += X[i, j];
                }

                solver.AddConstr(leftside == 1, "c13_" + j.ToString());
            }

            // Constraint (3)
            for(int i = 1; i < jobsInclDummy; i++)
            {
                GRBLinExpr leftside = new GRBLinExpr();
                for (int j = 0; j < jobsInclDummy; j++)
                {   
                    if(i != j)
                        leftside += X[i, j];
                }
                solver.AddConstr(leftside == 1, "c3_" + i.ToString());

            }


            // Constraint (5)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                GRBLinExpr leftside = new GRBLinExpr();
                GRBLinExpr rightside = new GRBLinExpr();
                for(int i = 0; i < jobsInclDummy; i++)
                {
                    if (i == j)
                        continue;

                    leftside += X[i, j];
                    rightside += X[j, i];
                }
                solver.AddConstr(leftside == rightside, "c5_" + j.ToString());

            }

            // Constraint (28)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 1; j < jobsInclDummy; j++)
                {
                    GRBLinExpr r2 = X[i, j] - 1;
                    r2 *= V;

                    r2 += problem.s[scheduleShifted[i], scheduleShifted[j], machine] + problem.processingTimes[scheduleShifted[j] - 1, machine];

                    solver.AddConstr(C[j] >= C[i] + r2, "c28_" + i.ToString() + "," + j.ToString());
                }
            }

            // Constraint (19)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                for (int j = 1; j < jobsInclDummy; j++)
                    lhs += X[0, j];

                solver.AddConstr(lhs == 1.0, "c19");

            }

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                solver.AddConstr(T[j] >= C[j] - problem.dueDates[scheduleShifted[j] - 1], "c21_" + j.ToString());

            }

            // Constraint (23)
            solver.AddConstr(C[0] == 0, "c23");

        }

        private void setFunctionToMinimize(int jobsInclDummy, GRBModel solver, GRBVar[] T)
        {
            // MINIMISE (13)
            GRBLinExpr functionToMinimise = T[1];
            for (int i = 2; i < jobsInclDummy; i++)
            {
                functionToMinimise += T[i];
            }
            solver.SetObjective(functionToMinimise, GRB.MINIMIZE);
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, GRBVar[,] X)
        {
            List<int> machinesOrder = new List<int>() { 0 };

            int? foo = Helpers.getSuccessorJobSingleMachineGRB(machinesOrder.Last(), jobsInclDummy, X);
            while (foo != null)
            {
                machinesOrder.Add((int)foo);
                foo = Helpers.getSuccessorJobSingleMachineGRB(machinesOrder.Last(), jobsInclDummy, X);
            }

            // Create the same format as the original outputs
            machinesOrder.Remove(0);
            for (int i = 0; i < machinesOrder.Count; i++)
                machinesOrder[i] = machinesOrder[i] - 1;
            

            return new List<int>[] { machinesOrder};
        }
    }
}
