using System;
using System.Collections.Generic;

namespace HHG.Pseudorandom.Runtime
{
    public static class SpreadShuffle
    {
        public static List<T> Spread<T, TKey>(IEnumerable<T> items, Func<T, TKey> category, IRandom rng = null, float jitter = 0.1f)
        {
            rng = GameRandom.OrDefault(rng);

            // Grouped in first-seen order so results are deterministic per seed
            Dictionary<TKey, List<T>> groups = new Dictionary<TKey, List<T>>();
            List<TKey> order = new List<TKey>();

            foreach (T item in items)
            {
                TKey key = category(item);
                List<T> group;

                if (!groups.TryGetValue(key, out group))
                {
                    group = new List<T>();
                    groups.Add(key, group);
                    order.Add(key);
                }

                group.Add(item);
            }

            List<KeyValuePair<double, T>> placed = new List<KeyValuePair<double, T>>();

            foreach (TKey key in order)
            {
                List<T> group = groups[key];
                int count = group.Count;
                // Even spacing per category, with a random start offset and a little jitter
                double spacing = 1.0 / count;
                double offset = rng.NextDouble() * spacing;

                rng.Shuffle(group);

                for (int i = 0; i < count; i++)
                {
                    double drift = (rng.NextDouble() * 2.0 - 1.0) * jitter * spacing;
                    placed.Add(new KeyValuePair<double, T>(offset + i * spacing + drift, group[i]));
                }
            }

            placed.Sort((a, b) => a.Key.CompareTo(b.Key));

            List<T> result = new List<T>(placed.Count);

            foreach (KeyValuePair<double, T> pair in placed)
            {
                result.Add(pair.Value);
            }

            return result;
        }
    }
}
