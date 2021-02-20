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

        // 0 is the dummy job
        private List<int> scheduleInitialCombinedWithDummy = new List<int>();
        private List<int> scheduleInitialCombinedWithoutDummy = new List<int>();

        private List<int> machinesToChange;

        public TwoMachineModel(ProblemInstance problem, GRBEnv env, List<int>[] schedule, List<int> machinesToChange)
        {
            this.env = env;
            this.problem = problem;
            this.schedule = schedule;
            foreach (int m in machinesToChange)
            {
                this.scheduleInitialCombinedWithoutDummy.AddRange(schedule[m]);

            }
            this.machinesToChange = machinesToChange;

            for (int i = 0; i < scheduleInitialCombinedWithoutDummy.Count; i++)
                scheduleInitialCombinedWithoutDummy[i]++;

            scheduleInitialCombinedWithDummy = new List<int>(scheduleInitialCombinedWithoutDummy);
            scheduleInitialCombinedWithDummy.Insert(0, 0);
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
            model.Set("OutputFlag", "1");

            Console.WriteLine("Now solving.");

            //Console.WriteLine("Start solution value " + Math.Floor(model.ObjVal / V).ToString());

            model.Optimize();
            List<int>[] newSchedule = calculateMachineAsssignmentFromModel(jobsInclDummy, X, schedule);

            Console.WriteLine(String.Format(machinesToChange.Count.ToString() + " machine model tardiness {0} -> {1}", tardinessBefore, Math.Floor(model.ObjVal / V)));

            if (tardinessBefore < Math.Floor(model.ObjVal / V))
                throw new Exception("AAAH!");

            model.Dispose();
            return newSchedule;
        }


        private void setInitialValues(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y)
        {
            return;

            long maxMakeSpan = 0;

            foreach (int machine in machinesToChange)
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

                foreach (int j in scheduleInitialCombinedWithDummy)
                {
                    foreach (int m in machinesToChange)
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

                foreach (int m in machinesToChange)
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

                    foreach (int j in scheduleInitialCombinedWithDummy)
                    {
                        foreach (int m in machinesToChange)
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


                    foreach (int m in machinesToChange)
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
            foreach (int i in scheduleInitialCombinedWithDummy)
            {
                foreach (int j in scheduleInitialCombinedWithDummy)
                {
                    foreach (int k in machinesToChange)
                    {
                        X[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                    }
                }
            }

            foreach (int j in scheduleInitialCombinedWithDummy)
            {
                C[j] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "C_" + j.ToString());
            }

            // Constraint (22)
            // Be careful: in the model T(dummy job) is not defined
            foreach (int i in scheduleInitialCombinedWithoutDummy)
            {
                T[i] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "T_" + i.ToString());
            }

            // Constraint (25)
            foreach (int i  in scheduleInitialCombinedWithoutDummy)
            {
                foreach (int j in machinesToChange)
                {
                    Y[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "Y_" + i.ToString() + "," + j.ToString());
                }
            }
        }

        private void setConstraints(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y, int V)
        {
            // Constraint (26)
            foreach (int j in scheduleInitialCombinedWithoutDummy)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                foreach (int m in machinesToChange)
                {
                    if (problem.processingTimes[j - 1, m] >= 0)
                        lhs += Y[j, m];
                }
                model.AddConstr(lhs == 1.0, "C26_" + j.ToString());
            }

            // Constraint (27)
            foreach (int j in scheduleInitialCombinedWithoutDummy)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                bool foundASingleForbiddenMachine = false;
                foreach (int m in machinesToChange)
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
            foreach (int j in scheduleInitialCombinedWithoutDummy)
            {
                foreach (int m in machinesToChange)
                {
                    GRBLinExpr lhs = new GRBLinExpr();
                    foreach (int i in scheduleInitialCombinedWithDummy)
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
            foreach (int i in scheduleInitialCombinedWithoutDummy)
            {
                foreach (int m in machinesToChange)
                {
                    GRBLinExpr lhs = new GRBLinExpr();
                    foreach (int j in scheduleInitialCombinedWithDummy)
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
            foreach (int i in scheduleInitialCombinedWithDummy)
            {
                foreach (int j in scheduleInitialCombinedWithoutDummy)
                {
                    GRBLinExpr r2 = new GRBLinExpr();
                    foreach (int m in machinesToChange)
                    {
                        r2 += X[i, j, m];
                    }
                    r2 -= 1.0;
                    r2 *= V;

                    GRBLinExpr r1 = new GRBLinExpr();
                    foreach (int m in machinesToChange)
                    {
                        r1 += X[i, j, m] * (problem.s[i, j, m] + problem.processingTimes[j - 1, m]);
                    }

                    model.AddConstr(C[j] >= C[i] + r1 + r2, "C28_" + i.ToString() + "_" + j.ToString());
                }
            }

            // Constraint (19)
            foreach (int m in machinesToChange)
            {
                GRBLinExpr lhs = new GRBLinExpr();
                foreach (int j in scheduleInitialCombinedWithoutDummy)
                    lhs += X[0, j, m];

                model.AddConstr(lhs <= 1.0, "C19_" + m.ToString());
            }

            // Constraint (20)
            foreach (int m in machinesToChange)
            {
                GRBLinExpr[] lhs = new GRBLinExpr[jobsInclDummy + 1];
                GRBLinExpr lhsFinal = new GRBLinExpr();

                foreach (int i in scheduleInitialCombinedWithDummy)
                {
                    lhs[i] = new GRBLinExpr();
                    foreach (int j in scheduleInitialCombinedWithoutDummy)
                    {
                        if (i != j)
                        {
                            lhs[i] += problem.s[i, j, m] * X[i, j, m];
                        }
                    }
                }
                lhs[jobsInclDummy] = new GRBLinExpr();
                foreach (int i in scheduleInitialCombinedWithoutDummy)
                {
                    lhs[jobsInclDummy] += problem.processingTimes[i - 1, m] * Y[i, m] + problem.s[i, 0, m] * X[i, 0, m];

                }
                GRBLinExpr b = new GRBLinExpr();
                foreach (int o in scheduleInitialCombinedWithDummy)
                    b += lhs[o];

                model.AddConstr(b <= Cmax, "C20_" + m.ToString());
            }

            // Constraint (21)
            foreach (int j in scheduleInitialCombinedWithoutDummy)
            {
                model.AddConstr(T[j] >= C[j] - problem.dueDates[j - 1], "C21_" + j.ToString());
            }

            // Constraint (23)
            model.AddConstr(C[0] == 0, "C23");
        }

        private void setFunctionToMinimize(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y, int V)
        {
            // MINIMISE (13)
            GRBLinExpr functionToMinimise = T[scheduleInitialCombinedWithoutDummy[0]] * V;
            for (int i = 1; i < scheduleInitialCombinedWithoutDummy.Count; i++)
            {
                functionToMinimise += T[scheduleInitialCombinedWithoutDummy[i]] * V;
            }
            functionToMinimise += Cmax;
            model.SetObjective(functionToMinimise, GRB.MINIMIZE);
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, GRBVar[,,] X, List<int>[] schedule)
        {
            foreach (int m in machinesToChange)
                schedule[m] = new List<int> { 0 };

            foreach (int m in machinesToChange)
            {
                int? foo = Helpers.getSuccessorJobManyMachinesOnlySomeJobsGRB(schedule[m].Last(), m, scheduleInitialCombinedWithoutDummy, X);
                while (foo != null)
                {
                    schedule[m].Add((int)foo);
                    foo = Helpers.getSuccessorJobManyMachinesOnlySomeJobsGRB(schedule[m].Last(), m, scheduleInitialCombinedWithoutDummy, X);
                }

                // Create the same format as the original outputs
                schedule[m].Remove(0);
                for (int i = 0; i < schedule[m].Count; i++)
                    schedule[m][i] = schedule[m][i] - 1;
            }

            return schedule;
        }

    }
}
