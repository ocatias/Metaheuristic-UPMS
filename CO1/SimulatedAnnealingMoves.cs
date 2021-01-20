using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public static class SimulatedAnnealingMoves
    {
        public static List<int>[] doSAStep(ProblemInstance problem, Random rnd, List<int>[] schedules, int makeSpanMachine, 
            double probabilityTardynessGuideance, double probabilityInterMachineMove, double probabilityBlockMove, double probabilityShiftMove, double probabilityMakeSpanGuideance)
        {
            List<int>[] tempSchedule;
            bool selectTardyJob = rnd.NextDouble() < probabilityTardynessGuideance;
            bool doInterMachineMove = rnd.NextDouble() < probabilityInterMachineMove;
            bool doBlockMove = rnd.NextDouble() < probabilityBlockMove;
            bool doShiftMove = rnd.NextDouble() < probabilityShiftMove;
            bool doMakeSpanGuideance = rnd.NextDouble() < probabilityMakeSpanGuideance;

            if (doShiftMove)
            {
                if (doBlockMove)
                    tempSchedule = generateDoBlockShift(problem, schedules, rnd, selectTardyJob, doInterMachineMove, doMakeSpanGuideance, makeSpanMachine);
                else
                    tempSchedule = generateDoShiftMove(problem, schedules, rnd, selectTardyJob, doInterMachineMove, doMakeSpanGuideance, makeSpanMachine);
            }
            else
            {
                if (doBlockMove)
                    tempSchedule = generateDoBlockSwaps(problem, schedules, rnd, selectTardyJob, doInterMachineMove, doMakeSpanGuideance, makeSpanMachine);
                else
                    tempSchedule = generateDoSwapMove(problem, schedules, rnd, selectTardyJob, doInterMachineMove, doMakeSpanGuideance, makeSpanMachine);
            }

            return tempSchedule;
        }

        public static List<int>[] generateDoBlockSwaps(ProblemInstance problem, List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove, bool doMakeSpanGuideance, int makeSpanMachine)
        {
            int machineJob1, machineJob2, job1Position, job2Position, blockLength1, blockLength2;

            if (doMakeSpanGuideance)
                machineJob1 = makeSpanMachine;
            else
            {
                do
                {
                    machineJob1 = rnd.Next() % schedules.Length;
                } while (schedules[machineJob1].Count == 0);
            }

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
                    job1Position = Helpers.findTardyJobIdx(problem, schedules, rnd, machineJob1);
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
                    job1Position = Helpers.findTardyJobIdx(problem, schedules, rnd, machineJob1);
                else
                    job1Position = rnd.Next() % (schedules[machineJob1].Count - 1);

                if (selectTardyJob && job1Position > 0)
                    job2Position = rnd.Next(0, job1Position);
                else
                    job2Position = (rnd.Next() % (schedules[machineJob1].Count - job1Position)) + job1Position;

                if (job1Position == job2Position)
                    return null;

                if (job1Position < job2Position)
                {
                    blockLength1 = rnd.Next(0, job2Position - job1Position) + 1;
                    blockLength2 = rnd.Next(0, schedules[machineJob2].Count - job2Position) + 1;
                }
                else
                {
                    blockLength2 = rnd.Next(0, job1Position - job2Position) + 1;
                    blockLength1 = rnd.Next(0, schedules[machineJob1].Count - job1Position) + 1;
                }
            }

            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);
            doBlockSwap(problem, schedules, job1Position, job2Position, machineJob1, machineJob2, blockLength1, blockLength2);
            return tempSchedule;
        }

        public static void doBlockSwap(ProblemInstance problem, List<int>[] schedules, int job1Position, int job2Position, int machineJob1, int machineJob2, int blockLength1, int blockLength2)
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
                    jobsFromMachine2.Add(schedules[machineJob2][job2Position - blockLength1]);
                    schedules[machineJob2].RemoveAt(job2Position - blockLength1);
                }
            }
            if (machineJob1 != machineJob2)
            {
                schedules[machineJob1].InsertRange(job1Position, jobsFromMachine2);
                schedules[machineJob2].InsertRange(job2Position, jobsFromMachine1);
            }
            else
            {
                if (job1Position < job2Position)
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

        public static List<int>[] generateDoBlockShift(ProblemInstance problem, List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove, bool doMakeSpanGuideance, int makeSpanMachine)
        {
            int machineJob1, machineJob2;
            if (doMakeSpanGuideance)
            {
                machineJob1 = makeSpanMachine;
            }
            else
            {
                do
                {
                    machineJob1 = rnd.Next() % schedules.Length;
                } while (schedules[machineJob1].Count == 0);
            }


            if (doInterMachineMove)
                machineJob2 = rnd.Next() % schedules.Length;
            else machineJob2 = machineJob1;

            int jobIndexToShift;
            if (selectTardyJob)
                jobIndexToShift = Helpers.findTardyJobIdx(problem, schedules, rnd, machineJob1);
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
                        while (positionAtTargetMatchine == jobIndexToShift)
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
            doBlockShift(problem, tempSchedule, jobIndexToShift, machineJob1, machineJob2, positionAtTargetMatchine, blockLength);
            return tempSchedule;
        }

        public static void doBlockShift(ProblemInstance problem, List<int>[] schedules, int jobIndexToShift, int machineToShiftFrom, int machineToShiftTo, int positionAtTargetMachine, int blockLength)
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

            if (machineToShiftFrom != machineToShiftTo || jobIndexToShift > positionAtTargetMachine)
            {
                for (int i = blockLength - 1; i >= 0; i--)
                    schedules[machineToShiftTo].Insert(positionAtTargetMachine, jobsToShift[i]);
            }
            else
            {
                if (jobIndexToShift < positionAtTargetMachine)
                    for (int i = blockLength - 1; i >= 0; i--)
                        schedules[machineToShiftTo].Insert(positionAtTargetMachine - blockLength, jobsToShift[i]);
            }
        }

        public static List<int>[] generateDoSwapMove(ProblemInstance problem, List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove, bool doMakeSpanGuideance, int makeSpanMachine)
        {
            int machineJob1, machineJob2;
            if (doMakeSpanGuideance)
                machineJob1 = makeSpanMachine;
            else
            {
                do
                {
                    machineJob1 = rnd.Next() % schedules.Length;
                } while (schedules[machineJob1].Count == 0);
            }

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
                job1 = schedules[machineJob1][Helpers.findTardyJobIdx(problem, schedules, rnd, machineJob1)];
            else
                job1 = schedules[machineJob1][rnd.Next() % schedules[machineJob1].Count];

            int job1Idx = schedules[machineJob1].FindIndex(j => j == job1);
            if (job1Idx > 0 && machineJob1 == machineJob2 && selectTardyJob)
                job2 = schedules[machineJob2][rnd.Next(0, job1Idx)];
            else
                job2 = schedules[machineJob2][rnd.Next() % schedules[machineJob2].Count];

            List<int>[] tempSchedule = Helpers.cloneSchedule(schedules);
            doSwapMove(problem, tempSchedule, job1, job2, machineJob1, machineJob2);
            return tempSchedule;
        }

        public static void doSwapMove(ProblemInstance problem, List<int>[] schedules, int job1, int job2, int machineJob1, int machineJob2)
        {
            if (!problem.isFeasibleJobAssignment(job1, machineJob2) || !problem.isFeasibleJobAssignment(job2, machineJob1))
                return;

            int idx1 = schedules[machineJob1].FindIndex(j => j == job1);
            int idx2 = schedules[machineJob2].FindIndex(j => j == job2);
            schedules[machineJob1][idx1] = job2;
            schedules[machineJob2][idx2] = job1;
        }

        public static List<int>[] generateDoShiftMove(ProblemInstance problem, List<int>[] schedules, Random rnd, bool selectTardyJob, bool doInterMachineMove, bool doMakeSpanGuideance, int makeSpanMachine)
        {

            int machineFrom;
            if (doMakeSpanGuideance)
                machineFrom = makeSpanMachine;
            else
                machineFrom = rnd.Next() % problem.machines;

            int machineTo;
            if (!doInterMachineMove)
                machineTo = machineFrom;
            else
                machineTo = rnd.Next() % problem.machines;

            if (schedules[machineFrom].Count == 0)
                return null;

            int jobToShift;
            if (selectTardyJob)
                jobToShift = schedules[machineFrom][Helpers.findTardyJobIdx(problem, schedules, rnd, machineFrom)];
            else
                jobToShift = schedules[machineFrom][rnd.Next(0, schedules[machineFrom].Count)];

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

            doShiftMove(problem, tempSchedule, jobToShift, machineFrom, machineTo, positionAtTargetMachine);

            return tempSchedule;
        }

        // Shifts the move in the schedules list
        public static void doShiftMove(ProblemInstance problem, List<int>[] schedules, int jobToShift, int machineToShiftFrom, int machineToShiftTo, int positionAtTargetMachine)
        {
            // If the move is not feasibility presevering then cancel
            if (problem.processingTimes[jobToShift, machineToShiftTo] < 0)
                return;

            schedules[machineToShiftFrom].Remove(jobToShift);
            schedules[machineToShiftTo].Insert(positionAtTargetMachine, jobToShift);
        }
    }
}
