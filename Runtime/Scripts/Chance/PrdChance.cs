using System;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class PrdChance
    {
        [SerializeField] private int attempts;

        private static readonly Dictionary<int, float> constantCache = new Dictionary<int, float>();

        public int AttemptsSinceSuccess => attempts;

        public bool Roll(float nominalChance, IRandom rng = null)
        {
            if (nominalChance <= 0f)
            {
                return false;
            }

            if (nominalChance >= 1f)
            {
                attempts = 0;
                return true;
            }

            // Chance on the Nth attempt since the last success is C * N
            attempts++;
            float chance = ConstantFor(nominalChance) * attempts;
            bool success = Randomizer.OrDefault(rng).Value() < chance;

            if (success)
            {
                attempts = 0;
            }

            return success;
        }

        public float NextChance(float nominalChance)
        {
            if (nominalChance <= 0f)
            {
                return 0f;
            }

            if (nominalChance >= 1f)
            {
                return 1f;
            }

            return Math.Min(1f, ConstantFor(nominalChance) * (attempts + 1));
        }

        public void Reset()
        {
            attempts = 0;
        }

        // The C whose long-run rate equals the nominal chance, cached at 0.01% resolution
        public static float ConstantFor(float nominalChance)
        {
            int key = (int)Math.Round(Clamp(nominalChance, 0.0001f, 0.9999f) * 10000f);
            float c;

            if (!constantCache.TryGetValue(key, out c))
            {
                c = (float)SolveConstant(key / 10000.0);
                constantCache.Add(key, c);
            }

            return c;
        }

        // Long-run success rate produced by a given C
        public static double RateFor(double c)
        {
            double expectedAttempts = 0;
            double successSoFar = 0;
            int maxAttempts = (int)Math.Ceiling(1.0 / c);

            for (int n = 1; n <= maxAttempts; n++)
            {
                double successOnN = Math.Min(1.0, n * c) * (1.0 - successSoFar);
                successSoFar += successOnN;
                expectedAttempts += n * successOnN;
            }

            return 1.0 / expectedAttempts;
        }

        // Bisection works because RateFor rises with C, and C <= p
        private static double SolveConstant(double p)
        {
            double low = 0;
            double high = p;

            for (int i = 0; i < 64; i++)
            {
                double mid = (low + high) * 0.5;

                if (RateFor(mid) > p)
                {
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return (low + high) * 0.5;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
