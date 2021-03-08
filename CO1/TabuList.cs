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

        public int Count()
        {
            return tabuPairings.Count;
        }
        
        public void addPairing(List<int> pairing)
        {
            if (tabuPairings.FirstOrDefault(t => t.Count == pairing.Count && t.All(s => pairing.Contains(s))) == null)
                tabuPairings.Add(pairing);
        }

        public void addPairing(int pairing)
        {
            addPairing(new List<int> { pairing});
        }

        public void clean(TabuList optimalList)
        {
            tabuPairings = new List<List<int>>();
            foreach (List<int> list in optimalList.tabuPairings)
                tabuPairings.Add(new List<int>(list));
        }

        public void removePairings(List<int> pairing)
        {
            foreach(int m in pairing)
            {
                tabuPairings.RemoveAll(p => p.Contains(m));
            }
        }

        // A pairing is allowed if the pairing is not in the tabu list and it is not a subset of any pairing in the tabu list
        public bool isNotATabuPairing(List<int> pairing)
        {
            //bool isAllowed = !(tabuPairings.Contains(pairing) || tabuPairings.Where(t => pairing.All(p => t.Contains(p))).Count() != 0);
            bool isAllowed = tabuPairings.FirstOrDefault(t => t.Count == pairing.Count && t.All(s => pairing.Contains(s))) == null;

            if (!isAllowed)
                nrTabuPairingsFound++;

            return isAllowed;
        }

        // Returns true if there exists no pairing which is a superset of it (or the same set)
        public bool isNotSubsetOfATabuPairing(List<int> pairing)
        {
            bool isAllowed = tabuPairings.Where(t => pairing.All(p => t.Contains(p))).Count() == 0;

            if (!isAllowed)
                nrTabuPairingsFound++;

            return isAllowed;
        }

        public bool isNotSubsetOfATabuPairing(int pairing)
        {
            return isNotSubsetOfATabuPairing(new List<int> { pairing });
        }

        public bool isAllowedPairing(int pairing)
        {
            return isNotATabuPairing(new List<int> { pairing });
        }


        public int nrOfTabuPairingsFound()
        {
            return nrTabuPairingsFound;
        }

        public string outputTabulist()
        {
            string output = "";
            foreach(List<int> pairing in tabuPairings.OrderBy(p => p.Count))
            {
                output += String.Join(", ", pairing) + "\n";
            }
            return output;
        }
    }
}
