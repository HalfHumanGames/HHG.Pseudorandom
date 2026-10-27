using System;

namespace HHG.Pseudorandom.Runtime
{
    public static class DisplayedChance
    {
        public static float TrueChance(float shownChance, ChanceModel model)
        {
            float h = Clamp01(shownChance);

            switch (model)
            {
                case ChanceModel.TwoRollAverage:
                    // P((a + b) / 2 < h) for two uniform rolls
                    return h <= 0.5f ? 2f * h * h : 1f - 2f * (1f - h) * (1f - h);

                case ChanceModel.Hybrid:
                    if (h < 0.5f)
                    {
                        return h;
                    }

                    // Sine curve above 50%: h + (2/15) * h * sin((2h - 1) * PI)
                    return Clamp01(h + (40f / 300f) * h * (float)Math.Sin((2.0 * h - 1.0) * Math.PI));

                default:
                    return h;
            }
        }

        public static bool Roll(float shownChance, ChanceModel model, IRandom rng = null)
        {
            IRandom source = GameRandom.OrDefault(rng);

            if (model == ChanceModel.TwoRollAverage)
            {
                return (source.Value() + source.Value()) * 0.5f < shownChance;
            }

            return source.Value() < TrueChance(shownChance, model);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : (value > 1f ? 1f : value);
        }
    }
}
