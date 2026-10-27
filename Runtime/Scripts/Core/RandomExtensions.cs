using System;
using System.Collections.Generic;

namespace HHG.Pseudorandom.Runtime
{
    public static class RandomExtensions
    {
        private const float floatUnit = 1f / 16777216f;             // 2^-24
        private const double doubleUnit = 1.0 / 9007199254740992.0; // 2^-53

        // [0, 1)
        public static float Value(this IRandom rng)
        {
            return (rng.NextUInt() >> 8) * floatUnit;
        }

        // [0, 1) with 53 bits of precision
        public static double NextDouble(this IRandom rng)
        {
            ulong a = rng.NextUInt() >> 5;
            ulong b = rng.NextUInt() >> 6;
            return (a * 67108864.0 + b) * doubleUnit;
        }

        // Lemire's method: unbiased, unlike NextUInt() % range
        public static int Range(this IRandom rng, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            uint range = (uint)((long)maxExclusive - minInclusive);
            ulong product = (ulong)rng.NextUInt() * range;
            uint low = (uint)product;

            if (low < range)
            {
                uint threshold = (0u - range) % range;

                while (low < threshold)
                {
                    product = (ulong)rng.NextUInt() * range;
                    low = (uint)product;
                }
            }

            return (int)(minInclusive + (long)(product >> 32));
        }

        // [min, max)
        public static float Range(this IRandom rng, float min, float max)
        {
            return min + rng.Value() * (max - min);
        }

        // Always consumes one roll, even for 0 or 1, so the stream stays in step
        public static bool Chance(this IRandom rng, float chance)
        {
            return rng.Value() < chance;
        }

        public static int Sign(this IRandom rng)
        {
            return (rng.NextUInt() & 1u) == 0 ? -1 : 1;
        }

        public static T Pick<T>(this IRandom rng, IList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            return list[rng.Range(0, list.Count)];
        }

        public static void Shuffle<T>(this IRandom rng, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        public static int WeightedIndex(this IRandom rng, IList<float> weights)
        {
            return WeightedIndex(rng, weights, w => w);
        }

        // Weights <= 0 are never picked; returns -1 if nothing has weight
        public static int WeightedIndex<T>(this IRandom rng, IList<T> items, Func<T, float> weight)
        {
            double total = 0;
            int lastPositive = -1;

            for (int i = 0; i < items.Count; i++)
            {
                float w = weight(items[i]);

                if (w > 0f)
                {
                    total += w;
                    lastPositive = i;
                }
            }

            if (lastPositive < 0)
            {
                return -1;
            }

            double roll = rng.NextDouble() * total;

            for (int i = 0; i < items.Count; i++)
            {
                float w = weight(items[i]);

                if (w > 0f)
                {
                    roll -= w;

                    if (roll < 0)
                    {
                        return i;
                    }
                }
            }

            // Floating point rounding can leave a sliver; it belongs to the last item
            return lastPositive;
        }

        public static T PickWeighted<T>(this IRandom rng, IList<T> items, Func<T, float> weight)
        {
            int index = WeightedIndex(rng, items, weight);
            return index < 0 ? default(T) : items[index];
        }
    }
}
