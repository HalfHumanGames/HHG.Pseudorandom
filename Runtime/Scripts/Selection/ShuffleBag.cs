using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class ShuffleBag<T>
    {
        [SerializeField] private List<BagEntry<T>> entries = new List<BagEntry<T>>();
        [SerializeField, Min(0)] private int refillAt = 0;
        [SerializeField] private bool avoidRepeatAcrossRefill = true;

        [SerializeField, HideInInspector] private List<int> bag = new List<int>();
        [SerializeField, HideInInspector] private int lastIndex = -1;

        public int Remaining => bag.Count;
        public IReadOnlyList<BagEntry<T>> Entries => entries;

        public ShuffleBag()
        {

        }

        public ShuffleBag(IEnumerable<T> items, int copiesEach = 1, int refillAt = 0, bool avoidRepeatAcrossRefill = true)
        {
            foreach (T item in items)
            {
                entries.Add(new BagEntry<T>(item, copiesEach));
            }

            this.refillAt = refillAt;
            this.avoidRepeatAcrossRefill = avoidRepeatAcrossRefill;
        }

        public void Add(T item, int copies = 1)
        {
            entries.Add(new BagEntry<T>(item, copies));
        }

        public T Next(IRandom rng = null)
        {
            rng = GameRandom.OrDefault(rng);

            if (bag.Count <= refillAt)
            {
                Refill(rng);
            }

            if (bag.Count == 0)
            {
                throw new InvalidOperationException("ShuffleBag has no entries with copies > 0.");
            }

            // Draws come off the end of the list
            int last = bag.Count - 1;
            int index = bag[last];
            bag.RemoveAt(last);
            lastIndex = index;
            return entries[index].Item;
        }

        public void Reset()
        {
            bag.Clear();
            lastIndex = -1;
        }

        private void Refill(IRandom rng)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                for (int c = 0; c < entries[i].Copies; c++)
                {
                    bag.Add(i);
                }
            }

            rng.Shuffle(bag);

            // Stop the previous bag's last item from coming up first in the new one
            int top = bag.Count - 1;

            if (avoidRepeatAcrossRefill && top > 0 && bag[top] == lastIndex)
            {
                int swapWith = FindOther(rng, lastIndex, top);

                if (swapWith >= 0)
                {
                    bag[top] = bag[swapWith];
                    bag[swapWith] = lastIndex;
                }
            }
        }

        // Random position below top holding a different entry, or -1
        private int FindOther(IRandom rng, int index, int top)
        {
            int start = rng.Range(0, top);

            for (int i = 0; i < top; i++)
            {
                int candidate = (start + i) % top;

                if (bag[candidate] != index)
                {
                    return candidate;
                }
            }

            return -1;
        }
    }
}
