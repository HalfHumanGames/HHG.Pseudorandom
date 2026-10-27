using System;
using System.Collections.Generic;

namespace HHG.Pseudorandom.Runtime
{
    public static class DuplicateProtection
    {
        public static List<T> Eligible<T>(IList<T> pool, Func<T, int> ownedCount, int maxCopies = 1)
        {
            List<T> eligible = new List<T>(pool.Count);

            for (int i = 0; i < pool.Count; i++)
            {
                if (ownedCount(pool[i]) < maxCopies)
                {
                    eligible.Add(pool[i]);
                }
            }

            // Everything is capped: fall back to the full pool
            if (eligible.Count == 0)
            {
                eligible.AddRange(pool);
            }

            return eligible;
        }

        public static T Pick<T>(IList<T> pool, Func<T, int> ownedCount, int maxCopies = 1, IRandom rng = null)
        {
            return Randomizer.OrDefault(rng).Pick(Eligible(pool, ownedCount, maxCopies));
        }

        public static T PickWeighted<T>(IList<T> pool, Func<T, float> weight, Func<T, int> ownedCount, int maxCopies = 1, IRandom rng = null)
        {
            List<T> eligible = Eligible(pool, ownedCount, maxCopies);
            IRandom source = Randomizer.OrDefault(rng);
            int index = source.WeightedIndex(eligible, weight);

            // Every eligible item has weight 0: fall back to the full pool by weight
            return index >= 0 ? eligible[index] : source.PickWeighted(pool, weight);
        }
    }
}
