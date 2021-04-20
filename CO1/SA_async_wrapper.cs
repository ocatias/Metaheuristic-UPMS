using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CO1
{
    static class SA_async_wrapper
    {
        static int iterations_until_checking_token = 100;
        public static void run_until_cancelled(SimulatedAnnealingSolver solver, CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                for(int iteration = 0; iteration < iterations_until_checking_token; iteration++)
                    solver.single_iteration();
            }
        }
    }
}
