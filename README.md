# Unrelated Parallel Machine Scheduling

The _Unrelated Parallel Machine Scheduling problem_ asks us to create schedule of jobs to be performed on different machines. The goal is to minimize the total tardiness and the makespan, i.e. the amount of time it takes us to complete all jobs. As some jobs might require special equipment we have constraints that some jobs can only be scheduled on selected machines. Furthermore, different jobs might require different preparations as equipment might need to be changed between jobs. As such, our problem models this by taking the setup time between different times of jobs into consideration.
This problem is formulated very generally and allows us to model many situations that arise in the industry.

I build on the work of [Moser](https://repositum.tuwien.at/handle/20.500.12708/11486) and implement one of their mixed integer programming (MIP) models and their simulated annealing solver (SA). Furthermore, I develop three very large neighborhood solvers to combine the strengths MIP and SA. This repository contains the following algorithms that solve the _Unrelated Parallel Machine Scheduling problem_:
* **Mixed Integer Programing** (MIP): Use Gurobi and Google OR-Tools to solve a mixed integer program. It can compute schedules and prove that they are optimal. This method can make use of multiple CPUs, it is very slow and only provides good solutions for very small problem instances.
* **Simulated Annealing**(SA): Generates a schedule via a simple heuristic and then improves it by generating neighboring solutions. This heuristic runs on a single core and is both very fast and capable of creating very good solutions for even large instances.
* **Very Large Neighborhood Search** (VLNS): After creating an initial schedule via a simple heuristic it solves mixed integer programs with Gurobi to improve the schedules of subsets of machines. This algorithm can use many CPUs and is generally slow and unable to provide good solutions.
* **Hybrid Very Large Neighborhood Search** (H-VLNS): Works like VLNS, but uses SA to generate the initial schedule.  This algorithm can use many CPUs and is significantly better than VLNS but still worse than SA.
* **Parallel Very Large Neighborhood Search** (P-VLNS): Runs eight SA solvers in parallel to create an initial solution. Then improves this solution by running two VLNS solvers and a SA solver in parallel, these solvers update each other everytime the VLNS solver have solved on subproblem. Works on multiple CPUs and is very fast and able to provide the best solutions.

## Results

### Mixed Integer Programing
I evaluate my <strong>MIP model</strong> on a computer with Windows 10, a Ryzen 5 1600 CPU (6 cores with 3.2 GHz each) and 16 GB of RAM.
<img src="/Pictures/mip_comparison.png" alt="Results" width="800">

This image shows the result of this evalution, you can see that my solutions (orange) generally have a lower cost than Moser's (blue). This is most likely due do the newer version of Gurobi I used.

### Simulated Annealing and Very Large Neighborhood Search Algorithms
I tuned the parameters with [SMAC](https://automl.github.io/SMAC3/master/) on the set of training instances provided by Moser. Then I test my algorithms on the test set of instances provided by moser. For tuning and evaluations I used a computer with two Intel Xeon E5345 CPUs (4 cores each, max.  2.33GHz) and 48 GB of RAM.

<img src="/Pictures/box_VLNS.png" alt="Results" width="400">
This figure compares that tardiness of schedules obtained from SA, VLNS and H-VLNS. We can see that H-VLNS produces schedules that are orders of magnitudes worse than the solutions from SA and H-VLNS.

The following table compares the median and mean tardiness of the different algorithms. We can see that Moser's SA implementation achives an excellent median tardiness.

| Algorithm   | Median Tardiness | Mean Tardiness |
|-------------|------------------|----------------|
| VLNS 3m | 87305.0 | 737330.89 |
|	VLNS 30 min | 38867 | 629120.29 |
|	H-VLNS 3m | 123  | 870.76 |
|	H-VLNS 30m |127.0  | 942.18 |
|	P-VLNS 3m | 35.0  | 347.74 |
|	P-VLNS 30m | 13  | 28.51 |
|	SA 1* min (80s) | 73.5 | 662.44  |
|	SA 3 min | 71.5  | 347.93  |
|	SA 30 min | 7.0  | 216.63 |
|	Moser 1 min | 8.0  | 322.62  |

<img src="/Pictures/box_all.png" alt="Results" width="800">
This figure compares the tardiness of schedules obtained by all of my algorithms for different runtimes. The runtime of "1* min" for SA means, that this runtime is comparable to Moser's 1 min runtime for their SA implementation. As they used a faster computer I had to run my algorithm for slightly longer to make a fair comparison, this means 1* min corresponds to 80 seconds of wallclock time.
Observe that my SA implementation achieves very similar results to Moser's at a runtime of one minute. Interestingly, my SA algorithm achives *worse* results at a higher runtime. This is most likely because I tuned the parameters for the different runtimes separately and did not let tuned them for long enough, leading to suboptimal parameters. 
We can see that H-VLNS achieves worse results than SA and that P-VLNS achieves better results than my SA implementation. 

We can also evaluate which of the algorithms provide the best solutions to how many test instances:
| Algorithm                  | Nr. instances with best solution            | Nr. instances better than Moser |
|----------------------------|---------------------------------------------|---------------------------------|
| VLNS 3m                    | 0 (0.0\%)                                   | 0 (0.0\%)   |
| VLNS 30 min                | 0 (0.0\%)                                   | 0 (0.0\%)   |
| H-VLNS 3m                  | 0 (0.0\%)                                   | 21 (17.5\%) |
| H-VLNS 30m                 | 1 (0.8\%)                                   | 5 (4.2\%)   |
| P-VLNS 3m                  | 4 (3.3\%)                                   | 40 (33.3\%) |
| P-VLNS 30m                 | 10 (8.3\%)                                  | 51 (42.5\%) |
| SA 1* min                  | 7 (5.8\%)                                   | 16 (13.3\%) |
| SA 3 min                   | 6 (5.0\%)                                   | 52 (43.3\%) |
| SA 30 min                  | 78 (65.0\%)                                 | 91 (75.8\%) |
| **All my algorithms combined** | 98 (81.7\%)                                 | 98 (81.7\%) |

## Used Technologies
- C#
- Gurobi (9.1.1)
- Google OR-Tools
