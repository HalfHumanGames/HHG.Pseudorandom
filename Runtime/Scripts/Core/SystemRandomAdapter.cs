using System;

namespace HHG.Pseudorandom.Runtime
{
    public sealed class SystemRandomAdapter : System.Random
    {
        private readonly IRandom source;

        public SystemRandomAdapter(IRandom source) : base(0)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override int Next()
        {
            return source.Range(0, int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return source.Range(0, maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            return source.Range(minValue, maxValue);
        }

        public override double NextDouble()
        {
            return source.NextDouble();
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)(source.NextUInt() >> 24);
            }
        }

        protected override double Sample()
        {
            return source.NextDouble();
        }
    }
}
