using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CO1
{
    // By Kevin: https://stackoverflow.com/questions/46735106/pick-random-element-from-list-with-probability
    public class WeightedItem<T>
    {
        private T value;
        private long weight;
        private long cumulativeSum;
        private static Random rndInst = new Random();

        public WeightedItem(T value, long weight)
        {
            this.value = value;
            this.weight = weight;
        }

        public static T Choose(List<WeightedItem<T>> items)
        {
            long cumulSum = 0;
            long cnt = items.Count();

            for (int slot = 0; slot < cnt; slot++)
            {
                cumulSum += items[slot].weight;
                items[slot].cumulativeSum = cumulSum;
            }

            double divSpot = rndInst.NextDouble() * cumulSum;
            WeightedItem<T> chosen = items.FirstOrDefault(i => i.cumulativeSum >= divSpot);
            if (chosen == null) throw new Exception("No item chosen - there seems to be a problem with the probability distribution.");
            return chosen.value;
        }
    }
}
