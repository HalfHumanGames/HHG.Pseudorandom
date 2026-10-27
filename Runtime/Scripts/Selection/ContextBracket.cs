using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class ContextBracket<T>
    {
        [SerializeField] private float minContext;
        [SerializeField] private List<WeightedItem<T>> items = new List<WeightedItem<T>>();

        public float MinContext => minContext;
        public List<WeightedItem<T>> Items => items;

        public ContextBracket()
        {

        }

        public ContextBracket(float minContext, IEnumerable<WeightedItem<T>> items)
        {
            this.minContext = minContext;
            this.items.AddRange(items);
        }
    }
}
