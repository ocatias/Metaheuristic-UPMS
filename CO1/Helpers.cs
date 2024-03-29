﻿using Google.OrTools.LinearSolver;
using Gurobi;
using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public static class Helpers
    {
        // onlyCloneThese must contain exactly 2 elements or be null!
        public static List<int>[] cloneSchedule(List<int>[] schedules, int[] onlyCloneThese = null)
        {
            List<int>[] tempSchedule = new List<int>[schedules.Length];
            for (int i = 0; i < schedules.Length; i++)
            {
                if (onlyCloneThese == null || onlyCloneThese[0] == i || onlyCloneThese[1] == i)
                    tempSchedule[i] = new List<int>(schedules[i]);
                else
                    tempSchedule[i] = schedules[i];
            }
            return tempSchedule;
        }

        public static long cost(long tardiness, long makespan)
        {
            return 100000 * tardiness + makespan;
        }

        // Returns true if the job is tardy in the schedule
        public static bool isJobTardy(ProblemInstance problem, List<int>[] schedules, int job, int machine)
        {
            long currTimeOnMachine = 0;

            if (schedules[machine].Count == 0)
                throw new Exception("Machine has no jobs scheduled empty!");

            currTimeOnMachine += problem.getSetupTimeForJob(0, schedules[machine][0] + 1, machine);
            currTimeOnMachine += problem.processingTimes[schedules[machine][0], machine];

            if(schedules[machine][0] == job)
            {
                return currTimeOnMachine > problem.dueDates[job];
            }

            for (int i = 1; i < schedules[machine].Count; i++)
            {
                currTimeOnMachine += problem.getSetupTimeForJob(schedules[machine][i - 1] + 1, schedules[machine][i] + 1, machine);
                currTimeOnMachine += problem.processingTimes[schedules[machine][i], machine];

                if (schedules[machine][i] == job)
                {
                    return currTimeOnMachine > problem.dueDates[job];
                }
            }

            throw new Exception("Job is not scheduled on this machine!");
        }

        // Finds a random tardy job in the schedule
        public static int findTardyJobIdx(ProblemInstance problem, SolutionCost cost, List<int>[] schedules, Random rnd, int machine)
        {
            if (cost.tardinessPerMachine[machine] == 0)
                return rnd.Next(0, schedules[machine].Count);
            int selectedJob = -1;
            long[] tardiness = new long[schedules[machine].Count];
            long[] time = new long[schedules[machine].Count];
            time[0] = problem.getSetupTimeForJob(0, schedules[machine][0] + 1, machine) + problem.processingTimes[schedules[machine][0], machine];
            tardiness[0] = (time[0] > problem.dueDates[schedules[machine][0]]) ? time[0] - problem.dueDates[schedules[machine][0]] : 0;
            for (int i = 1; i < schedules[machine].Count; i++)
            {
                time[i] = time[i - 1] + problem.getSetupTimeForJob(schedules[machine][i - 1] + 1, schedules[machine][i] + 1, machine) + problem.processingTimes[schedules[machine][i], machine];
                tardiness[i] = (time[i] > problem.dueDates[schedules[machine][i]]) ? time[i] - problem.dueDates[schedules[machine][i]] : 0;
            }

            for (int i = schedules[machine].Count - 1; i > 0; i--)
            {
                if (tardiness[i] > 0)
                {
                    selectedJob = rnd.Next(0, i + 1);
                    break;
                }
            }
            if (selectedJob != -1)
                return selectedJob;
            return rnd.Next(0, schedules[machine].Count);
        }

        // Find the successor of a job in a model assignment with multiple machines
        public static int? getSuccessorJobManyMachines(int predecessor, int machine, int jobsInclDummy, Variable[,,] X)
        {
            for (int j = 1; j < jobsInclDummy; j++)
            {
                if (X[predecessor, j, machine].SolutionValue() == 1)
                    return j;
            }

            return null;
        }

        // Find the successor of a job in a model assignment with multiple machines
        public static int? getSuccessorJobManyMachinesGRB(int predecessor, int machine, int jobsInclDummy, GRBVar[,,] X)
        {
            for (int j = 1; j < jobsInclDummy; j++)
            {
                if (X[predecessor, j, machine].X == 1)
                    return j;
            }

            return null;
        }

        // Find the successor of a job in a model assignment with multiple machines
        public static int? getSuccessorJobManyMachinesOnlySomeJobsGRB(int predecessor, int machine, List<int> jobs, GRBVar[,,] X)
        {
            foreach (int j in jobs)
            {
                if (X[predecessor, j, machine].X >= 0.9)
                    return j;
            }

            return null;
        }

        // Find the successor of a job in a model assignment
        public static int? getSuccessorJobSingleMachine(int predecessor, int jobsInclDummy, Variable[,] X)
        {
            for (int j = 1; j < jobsInclDummy; j++)
            {
                if (X[predecessor, j].SolutionValue() == 1)
                    return j;
            }

            return null;
        }

        // Find the successor of a job in a model assignment
        public static int? getSuccessorJobSingleMachineGRB(int predecessor, int jobsInclDummy, GRBVar[,] X)
        {
            for (int j = 1; j < jobsInclDummy; j++)
            {
                if (X[predecessor, j].X >= 0.9)
                    return j;
            }

            return null;
        }
    }
}
