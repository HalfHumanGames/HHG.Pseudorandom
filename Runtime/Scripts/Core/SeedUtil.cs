using System;

namespace HHG.Pseudorandom.Runtime
{
    public static class SeedUtil
    {
        // SplitMix64 (Steele, Lea & Flood)
        public static ulong SplitMix64(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public static ulong Mix(ulong value)
        {
            ulong state = value;
            return SplitMix64(ref state);
        }

        // FNV-1a: stable across platforms and app launches, unlike string.GetHashCode
        public static ulong Hash(string text)
        {
            ulong hash = 0xCBF29CE484222325UL;

            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 0x100000001B3UL;
                }
            }

            return hash;
        }

        // Mixed nonlinearly, so streams from similar seeds or names aren't correlated
        public static ulong Derive(ulong seed, string streamName)
        {
            return Mix(seed ^ Mix(Hash(streamName)));
        }

        public static ulong FromString(string seedText)
        {
            return Mix(Hash(seedText));
        }

        public static ulong NewSeed()
        {
            byte[] bytes = Guid.NewGuid().ToByteArray();
            ulong a = BitConverter.ToUInt64(bytes, 0);
            ulong b = BitConverter.ToUInt64(bytes, 8);
            return Mix(a ^ b ^ (ulong)DateTime.UtcNow.Ticks);
        }
    }
}
