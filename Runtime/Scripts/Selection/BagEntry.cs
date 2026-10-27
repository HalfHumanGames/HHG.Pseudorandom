using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public struct BagEntry<T>
    {
        [SerializeField] private T item;
        [SerializeField, Min(0)] private int copies;

        public T Item => item;
        public int Copies => copies;

        public BagEntry(T item, int copies)
        {
            this.item = item;
            this.copies = copies;
        }
    }
}
