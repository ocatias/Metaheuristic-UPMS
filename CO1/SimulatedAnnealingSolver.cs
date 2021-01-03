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

        public SimulatedAnnealingSolver(ProblemInstance problem)
        {
            this.problem = problem;

        }

        // Simmulated Annealing with Reheating
        public void solve(int runtimeInSeconds, string filepathResultInfo, string filepathMachineSchedule)
        {
            List<int>[] schedules = createInitialSchedules();

            (long tardiness, long makespan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, schedules);
            Verifier.verifyModelSolution(problem, tardiness, makespan, schedules);

            Random rnd = new Random();

            DateTime startTime = DateTime.UtcNow;

            int stepsBeforeCooling = 20339;
            double coolingFactor = 0.93;
            double tMin = 6.73;
            double tMax = 2764.93;
            double temperature = tMax;

            double probabilityInterMachineMove = 0.66;
            double probabilityShiftMove = 0.84;
            double probabilityBlockMove = 0.04;
            double probabilityTardynessGuideance = 0.85;

            int howOftenHaveWeCooled = 0;
            int currentStep = 0;

            long bestTardiness = tardiness;
            long bestMakeSpan = makespan;
            List<int>[] bestSchedules = Helpers.cloneSchedule(schedules);

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < runtimeInSeconds)
            {
                List<int>[] tempSchedule;
                bool selectTardyJob = rnd.NextDouble() < probabilityTardynessGuideance;
                bool doInterMachineMove = rnd.NextDouble() < probabilityInterMachineMove;
                bool doBlockMove = rnd.NextDouble() < probabilityBlockMove;
                bool doShiftMove = rnd.NextDouble() < probabilityShiftMove;

                if (doShiftMove)
                {
                    if (doBlockMove)
                        tempSchedule = generateDoBlockShift(schedules, rnd, selectTardyJob, doInterMachineMove);
                    else
                        tempSchedule = generateDoShiftMove(schedules, rnd, selectTardyJob, doInterMachineMove);
                }
                else
                {
                    if (doBlockMove)
                        tempSchedule = generateDoBlockSwaps(schedules, rnd, selectTardyJob, doInterMachineMove);
                    else
                        tempSchedule = generateDoSwapMove(schedules, rnd, selectTardyJob, doInterMachineMove);
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

                (long tardinessTemp, long makespanTemp) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, tempSchedule);

                if((tardinessTemp < tardiness || (tardinessTemp == tardiness && makespanTemp < makespan)) || (rnd.NextDouble() <= Math.Exp(-(Helpers.cost(tardinessTemp, makespanTemp)- Helpers.cost(tardiness, makespan))/temperature)))
                {
                    schedules = tempSchedule;
                    (tardiness, makespan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, schedules);

                    // Elitism
                    if(tardiness < bestTardiness || (tardiness == bestTardiness && makespan < bestMakeSpan))
                    {
                        bestTardiness = tardiness;
                        bestMakeSpan = makespan;
                        bestSchedules = Helpers.cloneSchedule(schedules);
                    }
                }
                       
            }

            if (tardiness > bestTardiness || (tardiness == bestTardiness && makespan > bestMakeSpan))
            {
                tardiness = bestTardiness;
                makespan = bestMakeSpan;
                schedules = Helpers.cloneSchedule(bestSchedules);
            }
            Console.WriteLine(String.Format("Best Result: ({0},{1})", tardiness, makespan));

            Verifier.verifyModelSolution(problem, tardiness, makespan, schedules);
            Console.WriteLine(String.Format("Iterations: {0}", currentStep));

            Verifier.verifyModelSolution(problem, tardiness, makespan, schedules);

            using (StreamWriter outputFile = new StreamWriter(filepathResultInfo))
            {
                outputFile.WriteLine(String.Format("Tardiness: {0}", tardiness));
                outputFile.WriteLine(String.Format("Makespan: {0}", makespan));
                outputFile.WriteLine(String.Format("Selected runtime: {0}s", runtimeInSeconds));
                outputFile.WriteLine(String.Format("Number of iterations: {0}", currentStep));

            }
            exportResults(schedules, filepathMachineSchedule);
        }

        public int findTardyJobIdx(List<int>[] schedules, Random rnd, int machine)
        {
            int selectedJob = -1;
            for(int i = schedules[machine].Count - 1; i > 0; i--)
            {
                if(Helpers.isJobTardy(problem, schedules, schedules[machine][i], machine))
                {
                    selectedJob = rnd.Next(0, i + 1);
                    break;
                }
            }
            if (selectedJob != -1)
                return selectedJob;
            return rnd.Next(0, schedules[machine].Count);
        }

        

        public List<int>[] generateDoBlockSwaps(List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove)
        {
            int machineJob1, machineJob2, job1Position, job2Position, blockLength1, blockLength2;
            do
            {
                machineJob1 = rnd.Next() % schedules.Length;
            } while (schedules[machineJob1].Count == 0);

            if (doInterMachineMove)
            {
                do
                {
                    machineJob2 = rnd.Next() % schedules.Length;
                } while (schedules[machineJob2].Count == 0);
            }
            else machineJob2 = machineJob1;
            

            if (machineJob1 != machineJob2)
            {
                if (selectTardyJob)
                    job1Position = findTardyJobIdx(schedules, rnd, machineJob1);
                else
                    job1Position = rnd.Next() % schedules[machineJob1].Count;

                job2Position = rnd.Next() % schedules[machineJob2].Count;
                blockLength1 = rnd.Next() % (schedules[machineJob1].Count - job1Position);
                blockLength2 = rnd.Next() % (schedules[machineJob2].Count - job2Position);
            }
            else
            {
                if (schedules[machineJob1].Count == 1)
                    return null;

                if (selectTardyJob)
                    job1Position = findTardyJobIdx(schedules, rnd, machineJob1);
                else
                    job1Position = rnd.Next() % (schedules[machineJob1].Count - 1);

                if (selectTardyJob && job1Position > 0)
                    job2Position = rnd.Next(0, job1Position);
                else
                    job2Position = (rnd.Next() % (schedules[machineJob1].Count - job1Position)) + job1Position;

                if (job1Position == job2Position)
                    return null;

                if(job1Position < job2Position)
                {
                    blockLength1 = rnd.Next(0,job2Position - job1Position) + 1;
                    blockLength2 = rnd.Next(0, schedules[machineJob2].Count - job2Position) + 1;
                }
                else
                {
                    blockLength2 = rnd.Next(0, job1Position - job2Position) + 1;
                    blockLength1 = rnd.Next(0, schedules[machineJob1].Count - job1Position) + 1;
                }
            }

            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);
            doBlockSwap(schedules, job1Position, job2Position, machineJob1, machineJob2, blockLength1, blockLength2);
            return tempSchedule;
        }

        public void doBlockSwap(List<int>[] schedules, int job1Position, int job2Position, int machineJob1, int machineJob2, int blockLength1, int blockLength2)
        {
            for (int i = 0; i < blockLength1; i++)
                if (!problem.isFeasibleJobAssignment(schedules[machineJob1][job1Position + i], machineJob2))
                    return;

            for (int i = 0; i < blockLength2; i++)
                if (!problem.isFeasibleJobAssignment(schedules[machineJob2][job2Position + i], machineJob1))
                    return;

            if (blockLength1 == 0 || blockLength2 == 0)
                return;

            List<int> jobsFromMachine1 = new List<int>(); 
            List<int> jobsFromMachine2 = new List<int>();
            for (int i = 0; i < blockLength1; i++)
            {
                jobsFromMachine1.Add(schedules[machineJob1][job1Position]);
                schedules[machineJob1].RemoveAt(job1Position);
            }

            for (int i = 0; i < blockLength2; i++)
            {
                if (machineJob1 != machineJob2 || job2Position < job1Position)
                {
                    jobsFromMachine2.Add(schedules[machineJob2][job2Position]);
                    schedules[machineJob2].RemoveAt(job2Position);
                }
                else
                {
                    jobsFromMachine2.Add(schedules[machineJob2][job2Position-blockLength1]);
                    schedules[machineJob2].RemoveAt(job2Position-blockLength1);
                }
            }
            if (machineJob1 != machineJob2)
            {
                schedules[machineJob1].InsertRange(job1Position, jobsFromMachine2);
                schedules[machineJob2].InsertRange(job2Position, jobsFromMachine1);
            }
            else
            {
                if(job1Position < job2Position)
                {
                    schedules[machineJob1].InsertRange(job1Position, jobsFromMachine2);
                    schedules[machineJob2].InsertRange(job2Position - blockLength1 + blockLength2, jobsFromMachine1);
                }
                else
                {
                    schedules[machineJob2].InsertRange(job2Position, jobsFromMachine1);
                    schedules[machineJob1].InsertRange(job1Position + blockLength1 - blockLength2, jobsFromMachine2);
                }
            }
        }

        public List<int>[] generateDoBlockShift(List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove)
        {
            int machineJob1, machineJob2;
            do
            {
                machineJob1 = rnd.Next() % schedules.Length;
            } while (schedules[machineJob1].Count == 0);

            if (doInterMachineMove)
                machineJob2 = rnd.Next() % schedules.Length;
            else machineJob2 = machineJob1;

            int jobIndexToShift;
            if (selectTardyJob)
                jobIndexToShift = findTardyJobIdx(schedules, rnd, machineJob1);
            else
                jobIndexToShift = rnd.Next() % schedules[machineJob1].Count;

            int positionAtTargetMatchine = 0;
            if (schedules[machineJob2].Count > 0)
            {
                if (machineJob1 == machineJob2)
                {
                    if (schedules[machineJob2].Count == 1)
                        return null;
                    else if (selectTardyJob)
                        positionAtTargetMatchine = rnd.Next(0, jobIndexToShift);
                    else
                    {
                        positionAtTargetMatchine = jobIndexToShift;
                        while(positionAtTargetMatchine == jobIndexToShift)
                        {
                            positionAtTargetMatchine = rnd.Next(0, schedules[machineJob2].Count);
                        }
                    }
                    if (positionAtTargetMatchine == jobIndexToShift)
                        return null;
                    
                }
                else
                {
                    positionAtTargetMatchine = rnd.Next(0, schedules[machineJob2].Count);
                }
            }

            int blockLength;
            if (machineJob1 != machineJob2)
                blockLength = rnd.Next() % (schedules[machineJob1].Count - jobIndexToShift);
            else
            {
                if (positionAtTargetMatchine < jobIndexToShift)
                    blockLength = rnd.Next() % (schedules[machineJob1].Count - jobIndexToShift) + 1;
                else
                    blockLength = rnd.Next() % (positionAtTargetMatchine - jobIndexToShift) + 1;
            }

            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);
            doBlockShift(tempSchedule, jobIndexToShift, machineJob1, machineJob2, positionAtTargetMatchine, blockLength);
            return tempSchedule;
        }

        public void doBlockShift(List<int>[] schedules, int jobIndexToShift, int machineToShiftFrom, int machineToShiftTo, int positionAtTargetMachine, int blockLength)
        {
            for (int i = 0; i < blockLength; i++)
                if (!problem.isFeasibleJobAssignment(schedules[machineToShiftFrom][jobIndexToShift + i], machineToShiftTo))
                    return;

            List<int> jobsToShift = new List<int>();
            for (int i = 0; i < blockLength; i++)
            {
                jobsToShift.Add(schedules[machineToShiftFrom][jobIndexToShift]);
                schedules[machineToShiftFrom].RemoveAt(jobIndexToShift);
            }

            if(machineToShiftFrom != machineToShiftTo || jobIndexToShift > positionAtTargetMachine)
            {
                for (int i = blockLength - 1; i >= 0; i--)
                    schedules[machineToShiftTo].Insert(positionAtTargetMachine, jobsToShift[i]);
            }
            else
            {
                if(jobIndexToShift < positionAtTargetMachine)
                    for (int i = blockLength - 1; i >= 0; i--)
                        schedules[machineToShiftTo].Insert(positionAtTargetMachine- blockLength, jobsToShift[i]);
            }        
        }

        public List<int>[] generateDoSwapMove(List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove)
        {
            int machineJob1, machineJob2;
            do
            {
                machineJob1 = rnd.Next() % schedules.Length;
            } while (schedules[machineJob1].Count == 0);

            if (doInterMachineMove)
            {
                do
                {
                    machineJob2 = rnd.Next() % schedules.Length;
                } while (schedules[machineJob2].Count == 0);
            }
            else machineJob2 = machineJob1;

            int job1, job2;
            if (selectTardyJob)
                job1 = schedules[machineJob1][findTardyJobIdx(schedules, rnd, machineJob1)];
            else
                job1 = schedules[machineJob1][rnd.Next() % schedules[machineJob1].Count];

            int job1Idx = schedules[machineJob1].FindIndex(j => j == job1);
            if (job1Idx > 0 && machineJob1 == machineJob2 && selectTardyJob)
                job2 = schedules[machineJob2][rnd.Next(0, job1Idx)];
            else
                job2 = schedules[machineJob2][rnd.Next() % schedules[machineJob2].Count];

            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);
            doSwapMove(tempSchedule, job1, job2, machineJob1, machineJob2);
            return tempSchedule;
        }

        public void doSwapMove(List<int>[] schedules, int job1, int job2, int machineJob1, int machineJob2)
        {
            if (!problem.isFeasibleJobAssignment(job1, machineJob2) || !problem.isFeasibleJobAssignment(job2, machineJob1))
                return;

            int idx1 = schedules[machineJob1].FindIndex(j => j == job1);
            int idx2 = schedules[machineJob2].FindIndex(j => j == job2);
            schedules[machineJob1][idx1] = job2;
            schedules[machineJob2][idx2] = job1;
        }

        public List<int>[] generateDoShiftMove(List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove)
        {
            int machineFrom = rnd.Next() % problem.machines;
            int machineTo;
            if (!doInterMachineMove)
                machineTo = machineFrom;
            else
                machineTo = rnd.Next() % problem.machines;

            if (schedules[machineFrom].Count == 0)
                return null;

            int jobToShift;
            if (selectTardyJob)
                jobToShift = schedules[machineFrom][findTardyJobIdx(schedules, rnd, machineFrom)];
            else
                jobToShift = schedules[machineFrom][rnd.Next(0,schedules[machineFrom].Count)];

            int positionAtTargetMachine;
            if (schedules[machineTo].Count == 0)
                positionAtTargetMachine = 0;
            else
            {
                int jobToShiftIdx = schedules[machineFrom].FindIndex(s => s == jobToShift);
                if (selectTardyJob && machineFrom == machineTo && jobToShiftIdx > 0)
                    positionAtTargetMachine = rnd.Next(0, jobToShiftIdx);
                else
                    positionAtTargetMachine = rnd.Next() % (schedules[machineTo].Count);
            }
            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);

            doShiftMove(tempSchedule, jobToShift, machineFrom, machineTo, positionAtTargetMachine);
            
            return tempSchedule;
        }

        // Shifts the move in the schedules list
        public void doShiftMove(List<int>[] schedules, int jobToShift, int machineToShiftFrom, int machineToShiftTo, int positionAtTargetMachine)
        {
            // If the move is not feasibility presevering then cancel
            if (problem.processingTimes[jobToShift, machineToShiftTo] < 0)
                return;

            schedules[machineToShiftFrom].Remove(jobToShift);
            schedules[machineToShiftTo].Insert(positionAtTargetMachine, jobToShift);
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
