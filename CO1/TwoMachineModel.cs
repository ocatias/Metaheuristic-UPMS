using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public class TwoMachineModel
    {
        private GRBEnv env;
        private ProblemInstance problem;
        private List<int>[] schedule;
        private int machine1, machine2;
        private List<int> scheduleInitialCombined = new List<int>();
        private List<int> machinesToChange;

        public TwoMachineModel(ProblemInstance problem, GRBEnv env, List<int>[] schedule, List<int> machinesToChange)
        {
            this.env = env;
            this.problem = problem;
            this.schedule = schedule;
            this.scheduleInitialCombined.AddRange(schedule[machine1]);
            this.scheduleInitialCombined.AddRange(schedule[machine2]);
            this.machinesToChange = machinesToChange;
        }

        public List<int>[] solveModel(int milliseconds, long tardinessBefore)
        {
            Console.WriteLine("Solver runtime: " + milliseconds/1000 + " sec");

            int jobsInclDummy = this.problem.jobs + 1;
            GRBModel model = new GRBModel(env);

            GRBVar[,,] X = new GRBVar[jobsInclDummy, jobsInclDummy, problem.machines];
            GRBVar[] C = new GRBVar[jobsInclDummy];
            GRBVar[] T = new GRBVar[jobsInclDummy];
            GRBVar Cmax = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "C,ax");
            GRBVar[,] Y = new GRBVar[jobsInclDummy, problem.machines];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, model, X, C, T, Cmax, Y);
            setConstraints(jobsInclDummy, model, X, C, T, Cmax, Y, V);
            setFunctionToMinimize(jobsInclDummy, model, X, C, T, Cmax, Y, V);

            setInitialValues(jobsInclDummy, model, X, C, T, Cmax, Y);

            model.Set("TimeLimit", (milliseconds / 1000.0).ToString());
            model.Set("OutputFlag", "0");

            Console.WriteLine("Now solving.");
            model.Optimize();

            List<int>[] newSchedule = calculateMachineAsssignmentFromModel(jobsInclDummy, X);

            Console.WriteLine(String.Format(machinesToChange.Count.ToString() + " machine model tardiness {0} -> {1}", tardinessBefore, Math.Floor(model.ObjVal / V)));

            model.Dispose();
            return newSchedule;
        }


        private void setInitialValues(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y)
        {
            long maxMakeSpan = 0;

            for (int machine = 0; machine < problem.machines; machine++)
            {
                if (schedule[machine].Count == 0)
                    continue;

                long tardiness = 0;
                long currMakeSpan = 0, currTimeOnMachine = 0;

                // Is true if we should not edit this machine
                //bool isAFixedMachine = false;
                bool isAFixedMachine = !machinesToChange.Contains(machine);

                currMakeSpan += problem.getSetupTimeForJob(0, schedule[machine][0] + 1, machine);
                currMakeSpan += problem.processingTimes[schedule[machine][0], machine];

                currTimeOnMachine += problem.getSetupTimeForJob(0, schedule[machine][0] + 1, machine);
                currTimeOnMachine += problem.processingTimes[schedule[machine][0], machine];
                tardiness += (currTimeOnMachine - problem.dueDates[schedule[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][0]] : 0;


                long currTardinessForThisJob = (currTimeOnMachine - problem.dueDates[schedule[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][0]] : 0;
                T[schedule[machine][0]+1].Start = currTardinessForThisJob;
                C[schedule[machine][0] + 1].Start = currTimeOnMachine;

                // If we shouldn't edit this machine then we fix the variables to a constant value by setting upper bound equal to the lower bound
                if(isAFixedMachine)
                {
                    T[schedule[machine][0] + 1].UB = currTardinessForThisJob;
                    T[schedule[machine][0] + 1].LB = currTardinessForThisJob;
                    C[schedule[machine][0] + 1].UB = currTimeOnMachine;
                    C[schedule[machine][0] + 1].LB = currTimeOnMachine;
                }

                for(int j = 0; j < jobsInclDummy; j++)
                {
                    for(int m = 0; m < problem.machines; m++)
                    {
                        int xValue = j != 0 || m != machine ? 0 : 1;
                        X[j, schedule[machine][0] + 1, m].Start = xValue;

                        if(isAFixedMachine)
                        {
                            X[j, schedule[machine][0] + 1, m].UB = xValue;
                            X[j, schedule[machine][0] + 1, m].LB = xValue;
                        }

                    }
                }

                for(int m = 0; m < problem.machines; m++)
                {
                    int yValue = m != machine ? 0 : 1;
                    Y[schedule[machine][0] + 1, m].Start = yValue;

                    if(isAFixedMachine)
                    {
                        Y[schedule[machine][0] + 1, m].UB = yValue;
                        Y[schedule[machine][0] + 1, m].LB = yValue;
                    }
                }

                for (int i = 1; i < schedule[machine].Count; i++)
                {
                    currMakeSpan += problem.getSetupTimeForJob(schedule[machine][i - 1] + 1, schedule[machine][i] + 1, machine);
                    currMakeSpan += problem.processingTimes[schedule[machine][i], machine];

                    currTimeOnMachine += problem.getSetupTimeForJob(schedule[machine][i - 1] + 1, schedule[machine][i] + 1, machine);
                    currTimeOnMachine += problem.processingTimes[schedule[machine][i], machine];
                    tardiness += (currTimeOnMachine - problem.dueDates[schedule[machine][i]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][i]] : 0;

                    currTardinessForThisJob = (currTimeOnMachine - problem.dueDates[schedule[machine][i]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][i]] : 0;

                    T[schedule[machine][i] + 1].Start = currTardinessForThisJob;
                    C[schedule[machine][i] + 1].Start = currTimeOnMachine;

                    if (isAFixedMachine)
                    {
                        T[schedule[machine][i] + 1].UB = currTardinessForThisJob;
                        T[schedule[machine][i] + 1].LB = currTardinessForThisJob;
                        C[schedule[machine][i] + 1].UB = currTimeOnMachine;
                        C[schedule[machine][i] + 1].LB = currTimeOnMachine;
                    }

                    for (int j = 0; j < jobsInclDummy; j++)
                    {
                        for (int m = 0; m < problem.machines; m++)
                        {
                            int xValue = j != schedule[machine][i - 1] + 1 || m != machine ? 0 : 1;
                            X[j, schedule[machine][i] + 1, m].Start = xValue;

                            if (isAFixedMachine)
                            {
                                X[j, schedule[machine][i] + 1, m].UB = xValue;
                                X[j, schedule[machine][i] + 1, m].LB = xValue;
                            }
                        }
                    }


                    for (int m = 0; m < problem.machines; m++)
                    {
                        int yValue = m != machine ? 0 : 1;
                        Y[schedule[machine][i] + 1, m].Start = yValue;

                        if (isAFixedMachine)
                        {
                            Y[schedule[machine][i] + 1, m].UB = yValue;
                            Y[schedule[machine][i] + 1, m].LB = yValue;
                        }
                    }
                }

                currMakeSpan += problem.getSetupTimeForJob(schedule[machine][schedule[machine].Count - 1] + 1, 0, machine);

                if (currMakeSpan > maxMakeSpan)
                    maxMakeSpan = currMakeSpan;
            }
            Cmax.Start = maxMakeSpan;
        }

        private void initializeVariables(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y)
        {
            // CONSTRAINT (24)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < jobsInclDummy; j++)
                {
                    for (int k = 0; k < problem.machines; k++)
                    {
                        X[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                    }
                }
            }

            for (int j = 0; j < jobsInclDummy; j++)
            {
                C[j] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "C_" + j.ToString());
            }

            // Constraint (22)
            // Be careful: in the model T(dummy job) is not defined
            for (int i = 1; i < jobsInclDummy; i++)
            {
                T[i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "T_" + i.ToString());
            }

            // Constraint (25)
            for (int i = 1; i < jobsInclDummy; i++)
            {
                for (int j = 0; j < problem.machines; j++)
                {
                    Y[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "Y_" + i.ToString() + "," + j.ToString());
                }
            }
        }

        private void setConstraints(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y, int V)
        {
            // Constraint (26)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                for (int m = 0; m < problem.machines; m++)
                {
                    if (problem.processingTimes[j - 1, m] >= 0)
                        lhs += Y[j, m];
                }
                model.AddConstr(lhs == 1.0, "C26_" + j.ToString());
            }

            // Constraint (27)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                GRBLinExpr lhs = new GRBLinExpr();
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
                    model.AddConstr(lhs == 0.0, "C27_" + j.ToString());

            }

            // Constraint (16)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                for (int m = 0; m < problem.machines; m++)
                {
                    GRBLinExpr lhs = new GRBLinExpr();
                    for (int i = 0; i < jobsInclDummy; i++)
                    {
                        if (i != j)
                        {
                            lhs += X[i, j, m];
                        }
                    }
                    model.AddConstr(lhs == Y[j, m], "C16_" + j.ToString() + "_" + m.ToString());
                }
            }

            // Constraint 17
            for (int i = 1; i < jobsInclDummy; i++)
            {
                for (int m = 0; m < problem.machines; m++)
                {
                    GRBLinExpr lhs = new GRBLinExpr();
                    for (int j = 0; j < jobsInclDummy; j++)
                    {
                        if (i != j)
                        {
                            lhs += X[i, j, m];
                        }
                    }
                    model.AddConstr(lhs == Y[i, m], "C17_" + i.ToString() + "_" + m.ToString());
                }
            }

            // Constraint (28)
            for (int i = 0; i < jobsInclDummy; i++)
            {
                for (int j = 1; j < jobsInclDummy; j++)
                {
                    GRBLinExpr r2 = new GRBLinExpr();
                    for (int m = 0; m < problem.machines; m++)
                    {
                        r2 += X[i, j, m];
                    }
                    r2 -= 1.0;
                    r2 *= V;

                    GRBLinExpr r1 = new GRBLinExpr();
                    for (int m = 0; m < problem.machines; m++)
                    {
                        r1 += X[i, j, m] * (problem.s[i, j, m] + problem.processingTimes[j - 1, m]);
                    }

                    model.AddConstr(C[j] >= C[i] + r1 + r2, "C28_" + i.ToString() + "_" + j.ToString());
                }
            }

            // Constraint (19)
            for (int m = 0; m < problem.machines; m++)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                for (int j = 1; j < jobsInclDummy; j++)
                    lhs += X[0, j, m];

                model.AddConstr(lhs <= 1.0, "C19_" + m.ToString());
            }

            // Constraint (20)
            for (int m = 0; m < problem.machines; m++)
            {
                GRBLinExpr[] lhs = new GRBLinExpr[jobsInclDummy + 1];
                GRBLinExpr lhsFinal = new GRBLinExpr();

                for (int i = 0; i < jobsInclDummy; i++)
                {
                    lhs[i] = new GRBLinExpr();
                    for (int j = 1; j < jobsInclDummy; j++)
                    {
                        if (i != j)
                        {
                            lhs[i] += problem.s[i, j, m] * X[i, j, m];
                        }
                    }
                }
                lhs[jobsInclDummy] = new GRBLinExpr();
                for (int i = 1; i < jobsInclDummy; i++)
                {
                    lhs[jobsInclDummy] += problem.processingTimes[i - 1, m] * Y[i, m] + problem.s[i, 0, m] * X[i, 0, m];

                }
                GRBLinExpr b = new GRBLinExpr();
                for (int o = 0; o < jobsInclDummy + 1; o++)
                    b += lhs[o];

                model.AddConstr(b <= Cmax, "C20_" + m.ToString());
            }

            // Constraint (21)
            for (int j = 1; j < jobsInclDummy; j++)
            {
                model.AddConstr(T[j] >= C[j] - problem.dueDates[j - 1], "C21_" + j.ToString());
            }

            // Constraint (23)
            model.AddConstr(C[0] == 0, "C23");
        }

        private void setFunctionToMinimize(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y, int V)
        {
            // MINIMISE (13)
            GRBLinExpr functionToMinimise = T[1] * V;
            for (int i = 2; i < jobsInclDummy; i++)
            {
                functionToMinimise += T[i] * V;
            }
            functionToMinimise += Cmax;
            model.SetObjective(functionToMinimise, GRB.MINIMIZE);
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, GRBVar[,,] X)
        {
            List<int>[] machinesOrder = new List<int>[problem.machines];
            for (int m = 0; m < problem.machines; m++)
                machinesOrder[m] = new List<int> { 0 };

            for (int m = 0; m < problem.machines; m++)
            {
                int? foo = Helpers.getSuccessorJobManyMachinesGRB(machinesOrder[m].Last(), m, jobsInclDummy, X);
                while (foo != null)
                {
                    machinesOrder[m].Add((int)foo);
                    foo = Helpers.getSuccessorJobManyMachinesGRB(machinesOrder[m].Last(), m, jobsInclDummy, X);
                }

                // Create the same format as the original outputs
                machinesOrder[m].Remove(0);
                for (int i = 0; i < machinesOrder[m].Count; i++)
                    machinesOrder[m][i] = machinesOrder[m][i] - 1;
            }

            return machinesOrder;
        }

    }
}
