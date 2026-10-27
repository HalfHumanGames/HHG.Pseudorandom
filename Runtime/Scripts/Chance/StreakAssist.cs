using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class StreakAssist
    {
        [SerializeField, Range(0f, 1f)] private float bonusPerMiss = 0.1f;
        [SerializeField, Range(0f, 1f)] private float minBaseChance = 0.5f;
        [SerializeField, Range(0f, 1f)] private float maxChance = 0.95f;

        [SerializeField, HideInInspector] private int misses;

        public int ConsecutiveMisses => misses;

        public StreakAssist()
        {

        }

        public StreakAssist(float bonusPerMiss, float minBaseChance = 0.5f, float maxChance = 0.95f)
        {
            this.bonusPerMiss = bonusPerMiss;
            this.minBaseChance = minBaseChance;
            this.maxChance = maxChance;
        }

        public float Adjust(float baseChance)
        {
            if (misses == 0 || baseChance < minBaseChance)
            {
                return baseChance;
            }

            float assisted = Math.Min(maxChance, baseChance + misses * bonusPerMiss);
            return Math.Max(baseChance, assisted);
        }

        public bool Roll(float baseChance, IRandom rng = null)
        {
            bool hit = Randomizer.OrDefault(rng).Value() < Adjust(baseChance);
            Report(hit);
            return hit;
        }

        public void Report(bool hit)
        {
            misses = hit ? 0 : misses + 1;
        }

        public void Reset()
        {
            misses = 0;
        }
    }
}
