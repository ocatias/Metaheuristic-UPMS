﻿using System;
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

        public WeightedItem(T value, long weight)
        {
            this.value = value;
            this.weight = weight;
        }

        public T getValue()
        {
            return value;
        }

        private static WeightedItem<T> ChooseWeightedItem(List<WeightedItem<T>> items, Random rnd)
        {
            long cumulSum = 0;
            long cnt = items.Count();

            for (int slot = 0; slot < cnt; slot++)
            {
                cumulSum += items[slot].weight;
                items[slot].cumulativeSum = cumulSum;
            }

            double divSpot = rnd.NextDouble() * cumulSum;
            return items.FirstOrDefault(i => i.cumulativeSum >= divSpot);
        }

        public static T Choose(List<WeightedItem<T>> items, Random rnd)
        {
            WeightedItem<T> chosen = ChooseWeightedItem(items, rnd);
            if (chosen == null) throw new Exception("No item chosen - there seems to be a problem with the probability distribution.");
            return chosen.value;
        }

        public static T ChooseAndRemove(ref List<WeightedItem<T>> items, Random rnd)
        {
            WeightedItem<T> chosen = ChooseWeightedItem(items, rnd);
            if (chosen == null) throw new Exception("No item chosen - there seems to be a problem with the probability distribution.");
            items.Remove(chosen);
            return chosen.value;
        }

        public static void Remove(ref List<WeightedItem<T>> items, T item)
        {
            WeightedItem<T> weightedItem = items.Where(i => i.value.Equals(item)).First();
            items.Remove(weightedItem);
        }

        public static void adaptWeight(ref List<WeightedItem<T>> items, T item, long weightChange)
        {
            WeightedItem<T> weightedItem = items.Where(i => i.value.Equals(item)).First();
            if (weightChange > 0 || weightedItem.weight + weightChange > 0)
                weightedItem.weight += weightChange;
            else
                weightedItem.weight = 1;
        }
    }
}
