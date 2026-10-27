using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class ContextualTable<T>
    {
        [SerializeField] private List<ContextBracket<T>> brackets = new List<ContextBracket<T>>();

        public List<ContextBracket<T>> Brackets => brackets;

        public void Add(float minContext, params WeightedItem<T>[] items)
        {
            brackets.Add(new ContextBracket<T>(minContext, items));
        }

        // Highest MinContext <= context, or the lowest bracket if context is below them all
        public ContextBracket<T> BracketFor(float context)
        {
            ContextBracket<T> best = null;
            ContextBracket<T> lowest = null;

            foreach (ContextBracket<T> bracket in brackets)
            {
                if (lowest == null || bracket.MinContext < lowest.MinContext)
                {
                    lowest = bracket;
                }

                if (bracket.MinContext <= context && (best == null || bracket.MinContext > best.MinContext))
                {
                    best = bracket;
                }
            }

            return best ?? lowest;
        }

        public T Pick(float context, IRandom rng = null)
        {
            ContextBracket<T> bracket = BracketFor(context);

            if (bracket == null)
            {
                throw new InvalidOperationException("ContextualTable has no brackets.");
            }

            return Randomizer.OrDefault(rng).PickWeighted(bracket.Items, item => item.Weight).Item;
        }
    }
}
