using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    public class TabuList
    {
        private List<List<int>> tabuPairings = new List<List<int>>();
        private int nrTabuPairingsFound = 0;


        public void addPairing(List<int> pairing)
        {
            if (tabuPairings.FirstOrDefault(t => t.Count == pairing.Count && t.All(s => pairing.Contains(s))) == null)
                tabuPairings.Add(pairing);
        }

        public void addPairing(int pairing)
        {
            addPairing(new List<int> { pairing});
        }

        public void removePairings(List<int> pairing)
        {
            foreach(int m in pairing)
            {
                tabuPairings.RemoveAll(p => p.Contains(m));
            }
        }

        // A pairing is allowed if the pairing is not in the tabu list and it is not a subset of any pairing in the tabu list
        public bool isAllowedPairing(List<int> pairing)
        {
            //bool isAllowed = !(tabuPairings.Contains(pairing) || tabuPairings.Where(t => pairing.All(p => t.Contains(p))).Count() != 0);
            bool isAllowed = tabuPairings.FirstOrDefault(t => t.Count == pairing.Count && t.All(s => pairing.Contains(s))) == null;

            if (!isAllowed)
                nrTabuPairingsFound++;

            return isAllowed;
        }

        public bool isAllowedPairing(int pairing)
        {
            return isAllowedPairing(new List<int> { pairing });
        }


        public int nrOfTabuPairingsFound()
        {
            return nrTabuPairingsFound;
        }
    }
}
