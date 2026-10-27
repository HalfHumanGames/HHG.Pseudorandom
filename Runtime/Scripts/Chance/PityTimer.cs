using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class PityTimer
    {
        [SerializeField, Range(0f, 1f)] private float baseChance = 0.05f;

        [SerializeField, Min(0)] private int softPityStart = 0;
        [SerializeField, Range(0f, 1f)] private float softPityStep = 0f;

        [SerializeField, Range(-1f, 1f)] private float initialBonus = 0f;
        [SerializeField, Range(0f, 1f)] private float maxBonus = 1f;

        [SerializeField, Min(0)] private int hardPity = 0;
        [SerializeField, Min(0)] private int firstSuccessBy = 0;

        [SerializeField, HideInInspector] private int failures;
        [SerializeField, HideInInspector] private bool hasSucceeded;

        public int FailuresSinceSuccess => failures;
        public bool HasSucceeded => hasSucceeded;

        public PityTimer()
        {

        }

        public PityTimer(float baseChance, int hardPity = 0, int softPityStart = 0, float softPityStep = 0f, float initialBonus = 0f, float maxBonus = 1f, int firstSuccessBy = 0)
        {
            this.baseChance = baseChance;
            this.hardPity = hardPity;
            this.softPityStart = softPityStart;
            this.softPityStep = softPityStep;
            this.initialBonus = initialBonus;
            this.maxBonus = maxBonus;
            this.firstSuccessBy = firstSuccessBy;
        }

        public float NextChance()
        {
            return NextChance(baseChance);
        }

        public float NextChance(float baseChanceOverride)
        {
            // 1-based, counted since the last success
            int attempt = failures + 1;

            if (hardPity > 0 && attempt >= hardPity)
            {
                return 1f;
            }

            if (!hasSucceeded && firstSuccessBy > 0 && attempt >= firstSuccessBy)
            {
                return 1f;
            }

            float bonus = initialBonus;

            if (softPityStart > 0 && attempt >= softPityStart)
            {
                bonus += (attempt - softPityStart + 1) * softPityStep;
            }

            bonus = Math.Min(bonus, maxBonus);
            return Clamp01(baseChanceOverride + bonus);
        }

        public bool Roll(IRandom rng = null)
        {
            return Roll(baseChance, rng);
        }

        public bool Roll(float baseChanceOverride, IRandom rng = null)
        {
            bool success = GameRandom.OrDefault(rng).Value() < NextChance(baseChanceOverride);

            if (success)
            {
                RecordSuccess();
            }
            else
            {
                RecordFailure();
            }

            return success;
        }

        public void RecordSuccess()
        {
            failures = 0;
            hasSucceeded = true;
        }

        public void RecordFailure()
        {
            failures++;
        }

        public void Reset()
        {
            failures = 0;
            hasSucceeded = false;
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}
