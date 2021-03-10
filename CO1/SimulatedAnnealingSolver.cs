using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Gurobi;

namespace CO1
{
    public class SimulatedAnnealingSolver
    {
        ProblemInstance problem;

        // Parameters:
        int stepsBeforeCooling = 20339;
        double coolingFactor = 0.93;
        double tMin = 6.73;
        double tMax = 2764.93;
        double temperature;
        //int maxStepsSinceLastImprovement = 5000000; //Afterwards we will try and solve a subproblem explicitly

        double probabilityInterMachineMove = 0.66;
        double probabilityShiftMove = 0.84;
        double probabilityBlockMove = 0.04;
        double probabilityTardynessGuideance = 0.84;
        double probabilityMakeSpanGuideance = 0.71;

        public SimulatedAnnealingSolver(ProblemInstance problem)
        {
            this.problem = problem;
        }

        // Simmulated Annealing with Reheating
        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule)
        {
            // Create an empty environment, set options and start
            //GRBEnv env = new GRBEnv(true);
            //env.Start();

            List<int>[] schedules = Heuristics.createInitialSchedules(problem);

            SolutionCost cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

            Random rnd = new Random();

            DateTime startTime = DateTime.UtcNow;

            int howOftenHaveWeCooled = 0;
            long currentStep = 0;
            int stepsSinceLastImprovement = 0;

            SolutionCost lowestCost = new SolutionCost(cost);
            List<int>[] bestSchedules = Helpers.cloneSchedule(schedules);

            temperature = tMax;

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < runtimeInSeconds)
            {
                List<int>[] tempSchedule;
                List<int> changedMachines;
                //if (stepsSinceLastImprovement < maxStepsSinceLastImprovement)
                //{
                    (tempSchedule, changedMachines) = SimulatedAnnealingMoves.doSAStep(problem,  rnd, schedules, cost.makeSpanMachine,
                         probabilityTardynessGuideance, probabilityInterMachineMove, probabilityBlockMove, probabilityShiftMove, probabilityMakeSpanGuideance);
                //}
                //else
                //{
                //    tempSchedule = Helpers.cloneSchedule(schedules);
                //    stepsSinceLastImprovement = 0;


                //    // Selected a machine with more than one job scheduled to it and a nonzero tardiness
                //    List<WeightedItem<int>> weightedMachinesList = new List<WeightedItem<int>>();
                //    for(int m = 0; m < schedules.Length; m++)
                //    {
                //        if (schedules[m].Count > 1 && cost.tardinessPerMachine[m] > 0)
                //            weightedMachinesList.Add(new WeightedItem<int>(m, cost.tardinessPerMachine[m]));
                //    }

                //    int singleMachineIdx = WeightedItem<int>.Choose(weightedMachinesList);



                //    SingleMachineModel sm = new SingleMachineModel(problem, env, schedules[singleMachineIdx], singleMachineIdx);
                //    tempSchedule[singleMachineIdx] = sm.solveModel(1000, cost.tardinessPerMachine[singleMachineIdx]);
                //    changedMachines = new List<int>() { singleMachineIdx };
                //}

                currentStep++;
                if ((currentStep - stepsBeforeCooling* howOftenHaveWeCooled) > stepsBeforeCooling)
                {
                    howOftenHaveWeCooled++;
                    temperature = temperature * coolingFactor;

                    // Reheat
                    if (temperature <= tMin)
                        temperature = tMax;
                }

                if (tempSchedule == null)
                    continue;

                SolutionCost costTemp = new SolutionCost(cost);
                costTemp = Verifier.updateTardMakeSpanMachineFromMachineAssignment(problem, tempSchedule, costTemp, changedMachines);

                if (costTemp.isBetterThan(cost))
                    stepsSinceLastImprovement = 0;
                else
                    stepsSinceLastImprovement++;

                if (costTemp.isBetterThan(cost) || 
                    (rnd.NextDouble() <= Math.Exp(-(Helpers.cost(costTemp.tardiness, costTemp.makeSpan) - Helpers.cost(cost.tardiness, cost.makeSpan))/temperature)))
                {
                    

                    schedules = tempSchedule;
                    cost = costTemp;

                    // Elitism
                    if(cost.isBetterThan(lowestCost))
                    {
                        lowestCost = new SolutionCost(cost);
                        bestSchedules = Helpers.cloneSchedule(schedules);
                    }
                }
                if(currentStep % 10000000 == 0)
                    Console.WriteLine(String.Format("Current: ({0}, {1}); Best: ({2},{3})", cost.tardiness, cost.makeSpan, lowestCost.tardiness, lowestCost.makeSpan));
                       
            }

            if (lowestCost.isBetterThan(cost))
            {
                cost = new SolutionCost(lowestCost);
                schedules = Helpers.cloneSchedule(bestSchedules);
            }


            Console.WriteLine(String.Format("Best Result: ({0},{1})", cost.tardiness, cost.makeSpan));

            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);
            Console.WriteLine(String.Format("Iterations: {0}", currentStep));

            using (StreamWriter outputFile = new StreamWriter(filepathResultInfo))
            {
                outputFile.WriteLine(String.Format("Tardiness: {0}", cost.tardiness));
                outputFile.WriteLine(String.Format("Makespan: {0}", cost.makeSpan));
                outputFile.WriteLine(String.Format("Selected runtime: {0}s", runtimeInSeconds));
                outputFile.WriteLine(String.Format("Number of iterations: {0}", currentStep));

            }
            exportResults(schedules, filepathMachineSchedule);
        }
        public void exportResults(List<int>[] schedules, string filepathMachineSchedule)
        {
            ResultExport.storeMachineSchedule(filepathMachineSchedule, problem, schedules);
        }
    }
}
