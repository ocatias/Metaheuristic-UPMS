# Unrelated Parallel Machine Scheduling
The objective of the _Unrelated Parallel Machine Scheduling problem_ (PMSP) is to schedule jobs on different machines in a way that minimizes the total tardiness of all jobs and the makespan. I build on the work of [Moser](https://repositum.tuwien.at/handle/20.500.12708/11486) and implement one of their mixed integer programming (MIP) models and their simulated annealing solver (SA). I experiment with three very large neighborhood solvers to combine the strengths MIP and SA. Evaluations show that the parallel very large neighborhood search (P-VLNS) solver manages to find better schedules than the simulated annealing solver.

## Mixed Integer Programing
<img src="/Pictures/mip_comparison.png" alt="Results" width="800">
My **MIP model** achieve better results than Moser's which is most likely due do the newer version of Gurobi I used.

## Simulated Annealing
<img src="/Pictures/SA.png" alt="Results" width="400">
My **simulated annealing** implementation performs similar albeit slight worse than Moser's.

## Very Large Neighborhood Search
<img src="/Pictures/box_VLNS.png" alt="Results" width="400">
Pure **very large neighborhood search** (VLNS) calculates a schedule by solving small subproblems with Gurobi. This gives significantly worse results than simulated annealing.

<img src="/Pictures/SA.png" alt="Results" width="400">
** Hybrid VLNS ** (H-VLNS) uses simulated annealing to generate an initial solution which it then improves via VLNS. This performs similar to SA. **Parallel VLNS** runs two simulated annealing solver and one VLNS solver in parallel. This achieves slightly better results than SA.

## Other Results
<img src="/Pictures/algo_results.png" alt="Results" width="400">
<img src="/Pictures/algo_results2.png" alt="Results" width="600">

## Used Technologies
- C#
- Gurobi (9.1.1)
- Google OR-Tools

The MIP model was evaluated on a computer with a Ryzen 5 1600 CPU (6 cores with 3.2 GHz each) and 16 GB of RAM. All other models were evaluated on a computer with two Intel Xeon E5345 CPUs (4 cores each, max.  2.33GHz) and 48 GB of RAM.
