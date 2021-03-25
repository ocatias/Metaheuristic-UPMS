using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public static class Verifier
    {
        public static (long, long) calculateTardMakeSpanFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            (long tardiness, long makespan, int machine) = calculateTardMakeSpanMachineFromMachineAssignment(problem, machinesOrder);
            return (tardiness, makespan);
        }
        public static (long, long, int) calculateTardMakeSpanMachineFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            SolutionCost sc = calcSolutionCostFromAssignment(problem, machinesOrder);
            return (sc.tardiness, sc.makeSpan, sc.makeSpanMachine);
        }

        public static SolutionCost calcSolutionCostFromAssignment(ProblemInstance problem, List<int>[] machinesOrder)
        {
            SolutionCost sc = new SolutionCost(machinesOrder.Count());

            for (int m = 0; m < problem.machines; m++)
            {
                (long tardinessOnThisMachine, long makeSpanOnThisMachine) = calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(problem, machinesOrder, m);

                sc.makeSpanPerMachine[m] = makeSpanOnThisMachine;
                sc.tardinessPerMachine[m] = tardinessOnThisMachine;
                //sc.tardinessPerJob[m] = tardinessPerJob;
            }
            sc.updateMakeSpan();
            sc.updateTardiness();
            return sc;
        }


        public static SolutionCost updateTardMakeSpanMachineFromMachineAssignment(ProblemInstance problem, List<int>[] machinesOrder, ref SolutionCost prevSolution, List<int> machinesToUpdate, bool guaranteedNoDuplicates = false)
        {
            //SolutionCost newSolution = new SolutionCost(prevSolution);

            // Ensure that we have no duplicate machines
            if (!guaranteedNoDuplicates)
                machinesToUpdate = machinesToUpdate.Distinct().ToList();

            bool updateMakespan = false;

            foreach (int machine in machinesToUpdate)
            {
                long prevMakespanOnMachine = prevSolution.makeSpanPerMachine[machine];
                (long tardynessOnMachine, long makeSpan) = calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(problem, machinesOrder, machine);
                prevSolution.tardiness = prevSolution.tardiness - prevSolution.tardinessPerMachine[machine] + tardynessOnMachine;
                prevSolution.tardinessPerMachine[machine] = tardynessOnMachine;
                prevSolution.makeSpanPerMachine[machine] = makeSpan;
                //prevSolution.tardinessPerJob[machine] = tardinessPerjob;

                if (makeSpan > prevSolution.makeSpan || prevMakespanOnMachine == prevSolution.makeSpan)
                    updateMakespan = true;
            }
            if (updateMakespan)
                prevSolution.updateMakeSpan();

            return prevSolution;
        }

        // Returns (TardynessOnThisMachine, MakespanOnThisMachine)
        public static (long, long) calculateTardMakeSpanMachineFromMachineAssignmentForSingleMachine(ProblemInstance problem, List<int>[] machinesOrder, int machine)
        {
            long tardiness = 0;
            long currMakeSpan = 0, currTimeOnMachine = 0, currTardiness = 0;
            //List<int> tardinessPerJob = new List<int>();

            if (machinesOrder[machine].Count == 0)
                return (0,0);

            long setupTime, processingTime;

            setupTime = problem.getSetupTimeForJob(0, machinesOrder[machine][0] + 1, machine);
            processingTime = problem.processingTimes[machinesOrder[machine][0], machine];

            currMakeSpan += setupTime + processingTime;

            currTimeOnMachine += setupTime + processingTime;
            currTardiness = (currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]] : 0;
            //tardinessPerJob.Add((int)currTardiness);
            tardiness += currTardiness;

            for (int i = 1; i < machinesOrder[machine].Count; i++)
            {
                setupTime = problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                setupTime = problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                processingTime = problem.processingTimes[machinesOrder[machine][i], machine];

                currMakeSpan += setupTime + processingTime;

                currTimeOnMachine += setupTime + processingTime;
                currTardiness = (currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]] : 0;
                //tardinessPerJob.Add((int)currTardiness);
                tardiness += currTardiness;
            }

            currMakeSpan += problem.getSetupTimeForJob(machinesOrder[machine][machinesOrder[machine].Count - 1] + 1, 0, machine);
            
            return (tardiness, currMakeSpan);
        }

        // Returns (TardynessOnThisMachine, MakespanOnThisMachine, List<(Job, TardynessOfThisJob)>)
        // Note: 0 is NOT a dummy job in the list
        public static (long, long, List<Tuple<int, long>>) calcuTdMsScheduleInfoForSingleMachine(ProblemInstance problem, List<int>[] machinesOrder, int machine)
        {
            List<Tuple<int, long>> machineScheduleInfo = new List<Tuple<int, long>>();

            long tardiness = 0;
            long currMakeSpan = 0, currTimeOnMachine = 0;

            if (machinesOrder[machine].Count == 0)
                return (0, 0, null);

            currMakeSpan += problem.getSetupTimeForJob(0, machinesOrder[machine][0] + 1, machine);
            currMakeSpan += problem.processingTimes[machinesOrder[machine][0], machine];

            currTimeOnMachine += problem.getSetupTimeForJob(0, machinesOrder[machine][0] + 1, machine);
            currTimeOnMachine += problem.processingTimes[machinesOrder[machine][0], machine];
            tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][0]] : 0;

            machineScheduleInfo.Add(new Tuple<int, long>(machinesOrder[machine][0], tardiness));

            for (int i = 1; i < machinesOrder[machine].Count; i++)
            {
                currMakeSpan += problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                currMakeSpan += problem.processingTimes[machinesOrder[machine][i], machine];

                currTimeOnMachine += problem.getSetupTimeForJob(machinesOrder[machine][i - 1] + 1, machinesOrder[machine][i] + 1, machine);
                currTimeOnMachine += problem.processingTimes[machinesOrder[machine][i], machine];
                tardiness += (currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]]) > 0 ? currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]] : 0;

                machineScheduleInfo.Add(new Tuple<int, long>(machinesOrder[machine][i], currTimeOnMachine - problem.dueDates[machinesOrder[machine][i]]));
            }

            currMakeSpan += problem.getSetupTimeForJob(machinesOrder[machine][machinesOrder[machine].Count - 1] + 1, 0, machine);

            return (tardiness, currMakeSpan, machineScheduleInfo);
        }



        public static void verifyModelSolution(ProblemInstance problem, long tardinessFromModel, long makeSpanFromModel, List<int>[] machinesOrder)
        {
            // Verify if the assignment to machines is correct
            for (int m = 0; m < problem.machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    if (problem.processingTimes[job, m] < 0)
                        throw new Exception("Job assigned to not eligible machine.");
            }

            // Verify if each job is assigned exactly once
            int[] jobs = new int[problem.jobs];
            Array.Clear(jobs, 0, jobs.Length);

            for (int m = 0; m < problem.machines; m++)
            {
                foreach (int job in machinesOrder[m])
                    jobs[job] += 1;
            }

            for (int i = 0; i < problem.jobs; i++)
            {
                if (jobs[i] != 1)
                    throw new Exception("A job is not assigned or assigned multiple times");
            }

            // Verify tardiness and makespan
            long tardiness, makeSpan;
            (tardiness, makeSpan) = Verifier.calculateTardMakeSpanFromMachineAssignment(problem, machinesOrder);

            if (tardiness > tardinessFromModel)
                throw new Exception("Tardiness from model is contradictory");
            //else if (tardiness < tardinessFromModel)
            //    outputFile.WriteLine("Tardiness could be selected smaller.");

            //if (makeSpan > makeSpanFromModel)
            //    throw new Exception("Makespan from model is contradictory");
            //else if (makeSpan < makeSpanFromModel)
            //    outputFile.WriteLine("Makespan could be selected smaller.");

            //outputFile.WriteLine("Solution verified.");
        }
    }
}
