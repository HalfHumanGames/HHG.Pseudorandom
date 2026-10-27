using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class HistoryPicker<T>
    {
        [SerializeField] private List<WeightedItem<T>> items = new List<WeightedItem<T>>();
        [SerializeField, Min(0)] private int historySize = 1;
        [SerializeField] private HistoryPickerMode mode = HistoryPickerMode.Exclude;
        [SerializeField, Min(0)] private int rerolls = 4;

        [SerializeField, HideInInspector] private List<int> history = new List<int>();

        [NonSerialized] private List<float> weightBuffer;

        public HistoryPicker()
        {

        }

        public HistoryPicker(IEnumerable<T> items, int historySize = 1, HistoryPickerMode mode = HistoryPickerMode.Exclude, int rerolls = 4)
        {
            foreach (T item in items)
            {
                this.items.Add(new WeightedItem<T>(item, 1f));
            }

            this.historySize = historySize;
            this.mode = mode;
            this.rerolls = rerolls;
        }

        public void Add(T item, float weight = 1f)
        {
            items.Add(new WeightedItem<T>(item, weight));
        }

        public T Next(IRandom rng = null)
        {
            rng = GameRandom.OrDefault(rng);

            int index = mode == HistoryPickerMode.Exclude ? PickExcluding(rng) : PickWithRerolls(rng);

            if (index < 0)
            {
                throw new InvalidOperationException("HistoryPicker has no items with weight > 0.");
            }

            Remember(index);
            return items[index].Item;
        }

        public void ClearHistory()
        {
            history.Clear();
        }

        private int PickExcluding(IRandom rng)
        {
            List<float> weights = Weights(true, history.Count);
            int index = rng.WeightedIndex(weights);

            // Everything is excluded: allow all but the most recent pick, then anything
            if (index < 0)
            {
                weights = Weights(true, Math.Min(1, history.Count));
                index = rng.WeightedIndex(weights);
            }

            if (index < 0)
            {
                weights = Weights(false, 0);
                index = rng.WeightedIndex(weights);
            }

            return index;
        }

        private int PickWithRerolls(IRandom rng)
        {
            List<float> weights = Weights(false, 0);
            int index = rng.WeightedIndex(weights);

            for (int i = 0; i < rerolls && index >= 0 && history.Contains(index); i++)
            {
                index = rng.WeightedIndex(weights);
            }

            return index;
        }

        // Item weights, with the most recent recentCount picks zeroed when excluding
        private List<float> Weights(bool exclude, int recentCount)
        {
            if (weightBuffer == null)
            {
                weightBuffer = new List<float>(items.Count);
            }

            weightBuffer.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                bool recent = exclude && history.LastIndexOf(i) >= history.Count - recentCount;
                weightBuffer.Add(recent ? 0f : items[i].Weight);
            }

            return weightBuffer;
        }

        private void Remember(int index)
        {
            if (historySize <= 0)
            {
                return;
            }

            history.Add(index);

            while (history.Count > historySize)
            {
                history.RemoveAt(0);
            }
        }
    }
}
