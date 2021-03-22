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

        private List<int>[] schedules;
        private SolutionCost cost;
        private long currentStep;

        // Parameters:
        int stepsBeforeCooling, maxBlockLength;
        double coolingFactor, tMin, tMax, temperature;
        //int maxStepsSinceLastImprovement = 5000000; //Afterwards we will try and solve a subproblem explicitly

        double probabilityInterMachineMove, probabilityShiftMove, probabilityBlockMove, probabilityTardynessGuideance, probabilityMakeSpanGuideance;

        public SimulatedAnnealingSolver(ProblemInstance problem, double tMax = 276400.93, double tMin = 6.73, int stepsBeforeCooling = 20339,
            double probabilityInterMachineMove = 0.66, double probabilityShiftMove = 0.84, double probabilityBlockMove = 0.04, 
            double probabilityTardynessGuideance = 0.84, double probabilityMakeSpanGuideance = 0.71, int maxBlockLength = 30, double coolingFactor = 0.93)
        {
            this.problem = problem;
            this.tMax = tMax;
            this.tMin = tMin;
            this.stepsBeforeCooling = stepsBeforeCooling;
            this.probabilityInterMachineMove = probabilityInterMachineMove;
            this.probabilityShiftMove = probabilityShiftMove;
            this.probabilityBlockMove = probabilityBlockMove;
            this.probabilityTardynessGuideance = probabilityTardynessGuideance;
            this.probabilityMakeSpanGuideance = probabilityMakeSpanGuideance;
            this.maxBlockLength = maxBlockLength;
            this.maxBlockLength = maxBlockLength;
            this.coolingFactor = coolingFactor;
        }

        public List<int>[] solveDirect(int runtimeInSeconds)
        {
            schedules = Heuristics.createInitialSchedules(problem);
            cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

            Random rnd = new Random();

            DateTime startTime = DateTime.UtcNow;

            int howOftenHaveWeCooled = 0;
            currentStep = 0;
            int stepsSinceLastImprovement = 0;

            SolutionCost lowestCost = new SolutionCost(cost);
            List<int>[] bestSchedules = Helpers.cloneSchedule(schedules);

            temperature = tMax;

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < runtimeInSeconds)
            {
                List<int>[] tempSchedule;
                List<int> changedMachines;
                (tempSchedule, changedMachines) = SimulatedAnnealingMoves.doSAStep(problem, rnd, schedules, cost.makeSpanMachine,
                     probabilityTardynessGuideance, probabilityInterMachineMove, probabilityBlockMove, probabilityShiftMove, probabilityMakeSpanGuideance, maxBlockLength);
                
                currentStep++;
                if ((currentStep - stepsBeforeCooling * howOftenHaveWeCooled) > stepsBeforeCooling)
                {
                    howOftenHaveWeCooled++;
                    temperature *= coolingFactor;

                    // Reheat
                    if (temperature <= tMin)
                    {
                        //Console.WriteLine(String.Format("Reheat: {0} -> {1}", temperature, tMax));
                        temperature = tMax;
                    }
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
                    (rnd.NextDouble() <= Math.Exp(-(Helpers.cost(costTemp.tardiness, costTemp.makeSpan) - Helpers.cost(cost.tardiness, cost.makeSpan)) / temperature)))
                {
                    //if (cost.tardiness < costTemp.tardiness)
                    //    Console.WriteLine(String.Format("{0}: {1} -> {2}", currentStep, cost.tardiness, costTemp.tardiness));

                    schedules = tempSchedule;
                    cost = costTemp;

                    // Elitism
                    if (cost.isBetterThan(lowestCost))
                    {
                        lowestCost = new SolutionCost(cost);
                        bestSchedules = Helpers.cloneSchedule(schedules);
                    }
                }
                //if (currentStep % 10000000 == 0)
                //    Console.WriteLine(String.Format("Current: ({0}, {1}); Best: ({2},{3})", cost.tardiness, cost.makeSpan, lowestCost.tardiness, lowestCost.makeSpan));

            }

            if (lowestCost.isBetterThan(cost))
            {
                cost = new SolutionCost(lowestCost);
                schedules = Helpers.cloneSchedule(bestSchedules);
            }
            return schedules;
        }

        // Simmulated Annealing with Reheating
        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule)
        {
            solveDirect(runtimeInSeconds);

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
