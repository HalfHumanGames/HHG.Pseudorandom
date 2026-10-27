using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class AdaptiveChance
    {
        [SerializeField, Range(0f, 1f)] private float baseChance = 0.4f;
        [SerializeField, Range(0f, 1f)] private float stepUp = 0.1f;
        [SerializeField] private bool symmetric = false;
        [SerializeField, Range(0f, 1f)] private float minChance = 0f;
        [SerializeField, Range(0f, 1f)] private float maxChance = 1f;

        [SerializeField, HideInInspector] private float offset;

        public float BaseChance => baseChance;
        public float CurrentChance => Clamp(baseChance + offset, minChance, maxChance);

        public AdaptiveChance()
        {

        }

        public AdaptiveChance(float baseChance, float stepUp, bool symmetric = false, float minChance = 0f, float maxChance = 1f)
        {
            this.baseChance = baseChance;
            this.stepUp = stepUp;
            this.symmetric = symmetric;
            this.minChance = minChance;
            this.maxChance = maxChance;
        }

        public bool Roll(IRandom rng = null)
        {
            return Roll(baseChance, rng);
        }

        public bool Roll(float nominalChance, IRandom rng = null)
        {
            float chance = Clamp(nominalChance + offset, minChance, maxChance);
            bool success = Randomizer.OrDefault(rng).Value() < chance;

            offset += success ? -StepDown(nominalChance) : stepUp;

            // Deliberately not clamped to min/max: a big down step gets paid back by a few
            // certain failures, which keeps the long-run rate exact. +/-2 only guards bad configs.
            offset = Clamp(offset, -2f, 2f);

            return success;
        }

        public void Reset()
        {
            offset = 0f;
        }

        private float StepDown(float nominalChance)
        {
            if (symmetric || nominalChance <= 0f)
            {
                return stepUp;
            }

            // Balances stepUp so the long-run success rate equals the nominal chance
            return stepUp * (1f - nominalChance) / nominalChance;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
