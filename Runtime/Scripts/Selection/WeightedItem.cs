using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public struct WeightedItem<T>
    {
        [SerializeField] private T item;
        [SerializeField, Min(0f)] private float weight;

        public T Item => item;
        public float Weight => weight;

        public WeightedItem(T item, float weight)
        {
            this.item = item;
            this.weight = weight;
        }
    }
}
