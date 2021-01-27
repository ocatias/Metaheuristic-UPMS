using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
        int maxStepsSinceLastImprovement = 5000000; //Afterwards we will try and solve a subproblem explicitly

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
            List<int>[] schedules = createInitialSchedules();

            SolutionCost cost = Verifier.calcSolutionCostFromAssignment(problem, schedules);

            Verifier.verifyModelSolution(problem, cost.tardiness, cost.makeSpan, schedules);

            Random rnd = new Random();

            DateTime startTime = DateTime.UtcNow;

            int howOftenHaveWeCooled = 0;
            int currentStep = 0;
            int stepsSinceLastImprovement = 0;

            SolutionCost lowestCost = new SolutionCost(cost);
            List<int>[] bestSchedules = Helpers.cloneSchedule(schedules);

            temperature = tMax;

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < runtimeInSeconds)
            {
                List<int>[] tempSchedule;
                List<int> changedMachines;
                if (stepsSinceLastImprovement < maxStepsSinceLastImprovement)
                {
                    (tempSchedule, changedMachines) = SimulatedAnnealingMoves.doSAStep(problem, rnd, schedules, cost.makeSpanMachine,
                         probabilityTardynessGuideance, probabilityInterMachineMove, probabilityBlockMove, probabilityShiftMove, probabilityMakeSpanGuideance);
                }
                else
                {
                    tempSchedule = Helpers.cloneSchedule(schedules);
                    stepsSinceLastImprovement = 0;


                    // Selected a machine with more than one job scheduled to it and a nonzero tardiness
                    List<WeightedItem<int>> weightedMachinesList = new List<WeightedItem<int>>();
                    for(int m = 0; m < schedules.Length; m++)
                    {
                        if (schedules[m].Count > 1 && cost.tardinessPerMachine[m] > 0)
                            weightedMachinesList.Add(new WeightedItem<int>(m, cost.tardinessPerMachine[m]));
                    }

                    int singleMachineIdx = WeightedItem<int>.Choose(weightedMachinesList);



                    SingleMachineModel sm = new SingleMachineModel(problem, schedules[singleMachineIdx], singleMachineIdx);
                    tempSchedule[singleMachineIdx] = sm.solveModel(10000, cost.tardinessPerMachine[singleMachineIdx]);
                    changedMachines = new List<int>() { singleMachineIdx };
                }

                currentStep++;
                if ((currentStep / stepsBeforeCooling) > howOftenHaveWeCooled)
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

        // 0 is the dummy job during computation, in the returned schedule 0 is the first job
        public List<int>[] createInitialSchedules()
        {
            /*
             * Use a greedy algorithm to create an initial solution
             * "Algorithm 1 Constructive Heuristic (CH)" from the paper
             */

            List<int> jobs = new List<int>(Enumerable.Range(1, problem.jobs).ToArray());
            jobs.OrderBy(job => problem.dueDates[job]);

            List<int>[] schedules = new List<int>[problem.machines];
            int[] machineSpan = new int[problem.machines];
            int[] prevJob = new int[problem.machines];
            for (int i = 0; i < problem.machines; i++)
            {
                schedules[i] = new List<int>() { 0 };
                machineSpan[i] = 0;
                prevJob[i] = 0;
            }

            while(jobs.Count != 0)
            {
                // Find the job with the lowest due date where adding it to a machine increases the total tardiness by the lowest amount
                int job = jobs.Where(j => problem.dueDates[j - 1] == problem.dueDates[jobs[0] - 1]).OrderBy(j => getCostNumberOfJob(j, schedules, machineSpan).Item1).First();

                (int minCost, int selectedMachine) = getCostNumberOfJob(job, schedules, machineSpan);
                machineSpan[selectedMachine] += minCost;
                schedules[selectedMachine].Add(job);
                jobs.Remove(job);
            }

            // Remove dummy jobs from start of schedule so it has the correct format
            foreach (List<int> schedule in schedules)
                schedule.RemoveAt(0);

            foreach (List<int> schedule in schedules)
            {
                for (int i = 0; i < schedule.Count; i++)
                    schedule[i] = schedule[i] - 1;
            }

            return schedules;
        }

        // For the use in createInitialSolution(): What is the tardiness if you add the job now to the machine where it fits best?
        public (int, int) getCostNumberOfJob(int job, List<int>[] schedule, int[] machineSpan)
        {
            int[] cost = new int[problem.machines];
            Array.Clear(cost, 0, cost.Length);

            for (int m = 0; m < problem.machines; m++)
            {
                if (problem.processingTimes[job - 1, m] < 0)
                {
                    cost[m] = int.MaxValue;
                    continue;
                }

                cost[m] = machineSpan[m] + problem.s[schedule[m].Last(), job, m] + problem.processingTimes[job - 1, m];
            }

            int selectedMachine = Array.IndexOf(cost, cost.Min());
            return (cost.Min(), selectedMachine);
        }
    }
}
