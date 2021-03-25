using System;
using System.Collections.Generic;
using System.Text;

namespace CO1
{
    public class SolutionCost
    {
        public long makeSpan, tardiness;
        public int makeSpanMachine;
        public List<long> tardinessPerMachine, makeSpanPerMachine;
        //public List<int>[] tardinessPerJob;

        public SolutionCost(SolutionCost prev, bool doNotStoreCostPerMachine = false)
        {
            this.makeSpan =  prev.makeSpan;
            this.tardiness = prev.tardiness;
            this.makeSpanMachine = prev.makeSpanMachine;
            if (!doNotStoreCostPerMachine)
            {
                this.tardinessPerMachine = new List<long>(prev.tardinessPerMachine);
                this.makeSpanPerMachine = new List<long>(prev.makeSpanPerMachine);
                //this.tardinessPerJob = new List<int>[prev.tardinessPerJob.Length];
                //for (int i = 0; i < prev.tardinessPerJob.Length; i++)
                //    if(prev.tardinessPerJob[i] != null)
                //        this.tardinessPerJob[i] = new List<int>(prev.tardinessPerJob[i]);
            }
        }


        public SolutionCost(int nrOfMachines)
        {
            this.tardinessPerMachine = new List<long>();
            this.makeSpanPerMachine = new List<long>();
            for(int i = 0; i < nrOfMachines; i++)
            {
                tardinessPerMachine.Add(0);
                makeSpanPerMachine.Add(0);
            }
            //this.tardinessPerJob = new List<int>[nrOfMachines];
        }

        public void updateMakeSpan()
        {
            makeSpan = -1;
            for(int i = 0; i < makeSpanPerMachine.Count; i++)
            {
                if(makeSpanPerMachine[i] > makeSpan)
                {
                    makeSpanMachine = i;
                    makeSpan = makeSpanPerMachine[i];
                }
            }
        }

        public void updateTardiness()
        {
            tardiness = 0;
            foreach(long tardinessOnMachine in tardinessPerMachine)
            {
                tardiness += tardinessOnMachine;
            }
        }
        
        // returns true if this object has a lower cost than the object in the parameter
        public bool isBetterThan(SolutionCost otherCost)
        {
            return (this.tardiness < otherCost.tardiness || (this.tardiness == otherCost.tardiness && this.makeSpan < otherCost.makeSpan));
        }
    }
}
