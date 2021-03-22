using Gurobi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public class MultiMachineModel
    {
        private GRBEnv env;
        private ProblemInstance problem;
        private List<int>[] schedule;

        // 0 is the dummy job
        private List<int> scheduleInitialCombinedWithDummy = new List<int>();
        private List<int> scheduleInitialCombinedWithoutDummy = new List<int>();

        private List<int> machinesToChange;
        private List<Tuple<int, int, int>> jobsToFreeze;

        public MultiMachineModel(ProblemInstance problem, GRBEnv env, List<int>[] schedule, List<int> machinesToChange, List<Tuple<int, int, int>> jobsToFreeze = null)
        {
            this.env = env;
            this.problem = problem;
            this.schedule = schedule;
            this.jobsToFreeze = jobsToFreeze;
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

        // Returns (schedule, isOptimal)
        public Tuple<List<int>[], bool> solveModel(int milliseconds, long tardinessBefore, bool optimizePrimarilyForTardiness = true)
        {


            Console.WriteLine("Solver runtime: " + milliseconds + " ms, machines: " + String.Join(", ", machinesToChange));

            if (!optimizePrimarilyForTardiness)
                Console.WriteLine("Optimizing for makespan");


            int jobsInclDummy = this.problem.jobs + 1;
            GRBModel model = new GRBModel(env);

            GRBVar[,,] X = new GRBVar[jobsInclDummy, jobsInclDummy, problem.machines];
            GRBVar[] C = new GRBVar[jobsInclDummy];
            GRBVar[] T = new GRBVar[jobsInclDummy];
            GRBVar Cmax = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "Cmax");
            GRBVar[,] Y = new GRBVar[jobsInclDummy, problem.machines];

            // ToDo = Probably need a bigger and not fixed value
            int V = 100000;

            initializeVariables(jobsInclDummy, model, X, C, T, Cmax, Y);
            setInitialValues(jobsInclDummy, model, X, C, T, Cmax, Y);
            setConstraints(jobsInclDummy, model, X, C, T, Cmax, Y, V);
            setFunctionToMinimize(jobsInclDummy, model, X, C, T, Cmax, Y, V, optimizePrimarilyForTardiness);


            model.Set("TimeLimit", (milliseconds / 1000.0).ToString());

            model.Set("OutputFlag", "0");
            //model.Set("MIPGap", "0.05");
            //var das = model.Parameters.MIPGap;

            model.Update();

            //Console.WriteLine("Start solution value " + Math.Floor(model.ObjVal / V).ToString());

            model.Optimize();
            Console.WriteLine(String.Format("\tMIP Gap: {0:C2}%", model.MIPGap.ToString()));
            Console.WriteLine(String.Format("Objval from gurobi: {0}", model.ObjVal));

            if (model.Status == 3)
                Console.WriteLine("----------------------------------");

            List<int>[] newSchedule = calculateMachineAsssignmentFromModel(jobsInclDummy, Y, X, schedule);

            bool isOptimal = model.Status == 2;

            if (isOptimal)
                Console.WriteLine("\t-> OPTIMAL SOLUTION");

            Console.WriteLine(String.Format("\tTardiness {0} -> {1}", tardinessBefore, Math.Floor(model.ObjVal / V)));

            model.Dispose();
            return new Tuple<List<int>[], bool> (newSchedule, isOptimal);
        }


        private void setInitialValues(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y)
        {
            long maxMakeSpan = 0;
            long tardiness = 0;

            foreach (int machine in machinesToChange)
            {
                if (schedule[machine].Count == 0)
                    continue;

                long currMakeSpan = 0, currTimeOnMachine = 0;

                currMakeSpan += problem.getSetupTimeForJob(0, schedule[machine][0] + 1, machine);
                currMakeSpan += problem.processingTimes[schedule[machine][0], machine];

                currTimeOnMachine += problem.getSetupTimeForJob(0, schedule[machine][0] + 1, machine);
                currTimeOnMachine += problem.processingTimes[schedule[machine][0], machine];
                tardiness += (currTimeOnMachine - problem.dueDates[schedule[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][0]] : 0;


                long currTardinessForThisJob = (currTimeOnMachine - problem.dueDates[schedule[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[schedule[machine][0]] : 0;
                T[schedule[machine][0]+1].Start = currTardinessForThisJob;
                C[schedule[machine][0] + 1].Start = currTimeOnMachine;


                foreach (int j in scheduleInitialCombinedWithDummy)
                {
                    foreach (int m in machinesToChange)
                    {
                        int xValue = j != 0 || m != machine ? 0 : 1;

                        Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 == j - 1 && p.Item2 == schedule[machine][0]).FirstOrDefault();
                        if (frozenJob != null)
                        {
                            model.Remove(X[j, schedule[machine][0] + 1, m]);
                            X[j, schedule[machine][0] + 1, m] = model.AddVar(xValue, xValue, 0.0, GRB.BINARY, "X_" + j.ToString() + "," + (schedule[machine][0] + 1).ToString() + "," + m.ToString());
                        }
                        X[j, schedule[machine][0] + 1, m].Start = xValue;
                    }
                }

                foreach (int m in machinesToChange)
                {
                    int yValue = m != machine ? 0 : 1;
                    Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 == schedule[machine][0] || p.Item2 == schedule[machine][0]).FirstOrDefault();
                    if (frozenJob != null)
                    {
                        model.Remove(Y[schedule[machine][0] + 1, m]);
                        Y[schedule[machine][0] + 1, m] = model.AddVar(yValue, yValue, 0.0, GRB.BINARY, "Y_" + (schedule[machine][0] + 1).ToString() + "," + m.ToString());
                    }

                    Y[schedule[machine][0] + 1, m].Start = yValue;
                    //Console.WriteLine("Y_" + (schedule[machine][0] + 1).ToString() + ", " + m.ToString() + ": " + yValue.ToString());
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

                    foreach (int j in scheduleInitialCombinedWithDummy)
                    {
                        foreach (int m in machinesToChange)
                        {
                            int xValue = j != schedule[machine][i - 1] + 1 || m != machine ? 0 : 1;

                            Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 == j - 1 && p.Item2 == schedule[machine][i]).FirstOrDefault();
                            if (frozenJob != null)
                            {
                                model.Remove(X[j, schedule[machine][i] + 1, m]);
                                X[j, schedule[machine][i] + 1, m] = model.AddVar(xValue, xValue, 0.0, GRB.BINARY, "X_" + j.ToString() + "," + (schedule[machine][0] + 1).ToString() + "," + m.ToString());
                            }
                            X[j, schedule[machine][i] + 1, m].Start = xValue;
                        }
                    }


                    foreach (int m in machinesToChange)
                    {
                        int yValue = m != machine ? 0 : 1;
                        Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 == schedule[machine][i]  || p.Item2 == schedule[machine][i]).FirstOrDefault();
                        if (frozenJob != null)
                        {
                            model.Remove(Y[schedule[machine][i] + 1, m]);
                            Y[schedule[machine][i] + 1, m] = model.AddVar(yValue, yValue, 0.0, GRB.BINARY, "Y_" + (schedule[machine][i] + 1).ToString() + "," + m.ToString());
                        }

                        Y[schedule[machine][i] + 1, m].Start = yValue;
                        //Console.WriteLine("Y_" + (schedule[machine][i] + 1).ToString() + ", " + m.ToString() + ": " + yValue.ToString());

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
                    Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 -1 == i && p.Item2-1 == j).FirstOrDefault();
                    foreach (int k in machinesToChange)
                    {
                        //if (frozenJob == null)
                            X[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                        //else if (frozenJob.Item3 != k)
                        //    X[i, j, k] = model.AddVar(0.0, 0.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                        //else if (frozenJob.Item3 == k)
                        //    X[i, j, k] = model.AddVar(1.0, 1.0, 0.0, GRB.BINARY, "X_" + i.ToString() + "," + j.ToString() + "," + k.ToString());
                        //else
                        //    throw new Exception("Hey");
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
                Tuple<int, int, int> frozenJob = jobsToFreeze.Where(p => p.Item1 == i || p.Item2 == i).FirstOrDefault();

                foreach (int j in machinesToChange)
                {
                    //if(frozenJob == null)
                        Y[i, j] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "Y_" + i.ToString() + "," + j.ToString());
                    //else if(frozenJob.Item3 != j)
                    //    Y[i, j] = model.AddVar(0.0, 0.0, 0.0, GRB.BINARY, "Y_" + i.ToString() + "," + j.ToString());
                    //else if (frozenJob.Item3 == j)
                    //    Y[i, j] = model.AddVar(1.0, 1.0, 0.0, GRB.BINARY, "Y_" + i.ToString() + "," + j.ToString());
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

        private void setFunctionToMinimize(int jobsInclDummy, GRBModel model, GRBVar[,,] X, GRBVar[] C, GRBVar[] T, GRBVar Cmax, GRBVar[,] Y, int V, bool optimizePrimarilyForTardiness)
        {
            // MINIMISE (13)
            GRBLinExpr functionToMinimise = T[scheduleInitialCombinedWithoutDummy[0]] * V;
            for (int i = 1; i < scheduleInitialCombinedWithoutDummy.Count; i++)
            {
                if (optimizePrimarilyForTardiness)
                    functionToMinimise += T[scheduleInitialCombinedWithoutDummy[i]] * V;
                else
                    functionToMinimise += T[scheduleInitialCombinedWithoutDummy[i]];
            }
            if (optimizePrimarilyForTardiness)
                functionToMinimise += Cmax;
            else
                functionToMinimise += Cmax * V;

            model.SetObjective(functionToMinimise, GRB.MINIMIZE);
        }

        private List<int>[] calculateMachineAsssignmentFromModel(int jobsInclDummy, GRBVar[,] Y, GRBVar[,,] X, List<int>[] schedule)
        {
            int count = 0;

            foreach (int m in machinesToChange)
                schedule[m] = new List<int> { 0 };

            //foreach (int i in scheduleInitialCombinedWithDummy)
            //    foreach (int j in scheduleInitialCombinedWithDummy)
            //        foreach (int m in machinesToChange)
            //            if (X[i, j, m].X != 0)
            //                Console.WriteLine("X_" + i.ToString() + ", " + j.ToString() + ", " + m.ToString() + " = " +  X[i, j, m].X.ToString());

            //foreach (int i in scheduleInitialCombinedWithoutDummy)
            //    foreach (int m in machinesToChange)
            //        if (Y[i, m].X != 0)
            //            Console.WriteLine("Y_" + i.ToString() + "," + m.ToString() + " = " + Y[i, m].X.ToString());

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

                count += schedule[m].Count;
            }

            if (count != scheduleInitialCombinedWithoutDummy.Count)
                throw new Exception("Error schedules not fitting together");

            return schedule;
        }

    }
}
