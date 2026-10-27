using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public struct VarianceRoll
    {
        [SerializeField, Min(1)] private int rolls;

        public int Rolls => Math.Max(1, rolls);

        public VarianceRoll(int rolls)
        {
            this.rolls = rolls;
        }

        public float Sample01(IRandom rng = null)
        {
            return Average01(Rolls, rng);
        }

        public float Range(float min, float max, IRandom rng = null)
        {
            return min + Sample01(rng) * (max - min);
        }

        public float Around(float center, float halfWidth, IRandom rng = null)
        {
            return center + (Sample01(rng) * 2f - 1f) * halfWidth;
        }

        public static float Average01(int rolls, IRandom rng = null)
        {
            IRandom source = Randomizer.OrDefault(rng);
            int count = Math.Max(1, rolls);
            float sum = 0f;

            for (int i = 0; i < count; i++)
            {
                sum += source.Value();
            }

            return sum / count;
        }

        public static float Normal(float mean, float standardDeviation, IRandom rng = null)
        {
            // Box-Muller; u1 is in (0, 1] so Log never sees 0
            IRandom source = Randomizer.OrDefault(rng);
            double u1 = 1.0 - source.NextDouble();
            double u2 = source.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + (float)(z * standardDeviation);
        }
    }
}
