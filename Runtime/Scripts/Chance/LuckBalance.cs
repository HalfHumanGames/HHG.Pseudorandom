using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class LuckBalance
    {
        [SerializeField, Range(0f, 1f)] private float strength = 0.25f;

        [SerializeField, HideInInspector] private float balance;

        public float Balance => balance;

        public LuckBalance()
        {

        }

        public LuckBalance(float strength)
        {
            this.strength = strength;
        }

        public bool Roll(float chance, IRandom rng = null)
        {
            if (chance <= 0f)
            {
                return false;
            }

            if (chance >= 1f)
            {
                return true;
            }

            bool success = Randomizer.OrDefault(rng).Value() < NextChance(chance);

            // Weighted by the nominal chance so every roll has an expected change of zero,
            // which means no choice of what to roll can improve future odds
            balance += success ? -strength * (1f - chance) : strength * chance;

            return success;
        }

        public float NextChance(float chance)
        {
            float adjusted = chance + balance;
            return adjusted < 0f ? 0f : (adjusted > 1f ? 1f : adjusted);
        }

        public void Reset()
        {
            balance = 0f;
        }
    }
}
