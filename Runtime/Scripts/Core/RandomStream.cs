using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    // xoshiro128** (Blackman & Vigna)
    [Serializable]
    public class RandomStream : IRandom
    {
        [SerializeField] private uint s0;
        [SerializeField] private uint s1;
        [SerializeField] private uint s2;
        [SerializeField] private uint s3;

        [NonSerialized] private SystemRandomAdapter systemRandom;

        public RandomStream() : this(SeedUtil.NewSeed())
        {

        }

        public RandomStream(ulong seed)
        {
            InitState(seed);
        }

        public RandomStream(RandomStreamState state)
        {
            SetState(state);
        }

        public void InitState(ulong seed)
        {
            // Expand the 64-bit seed into 128 bits of well-mixed state
            ulong state = seed;
            ulong a = SeedUtil.SplitMix64(ref state);
            ulong b = SeedUtil.SplitMix64(ref state);
            s0 = (uint)a;
            s1 = (uint)(a >> 32);
            s2 = (uint)b;
            s3 = (uint)(b >> 32);
            EnsureNonZero();
        }

        public void InitState(int seed)
        {
            InitState((ulong)(uint)seed);
        }

        public uint NextUInt()
        {
            uint result = RotateLeft(s1 * 5, 7) * 9;
            uint t = s1 << 9;

            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = RotateLeft(s3, 11);

            return result;
        }

        public RandomStreamState GetState()
        {
            return new RandomStreamState { S0 = s0, S1 = s1, S2 = s2, S3 = s3 };
        }

        public void SetState(RandomStreamState state)
        {
            s0 = state.S0;
            s1 = state.S1;
            s2 = state.S2;
            s3 = state.S3;
            EnsureNonZero();
        }

        public RandomStream Clone()
        {
            return new RandomStream(GetState());
        }

        public System.Random AsSystemRandom()
        {
            if (systemRandom == null)
            {
                systemRandom = new SystemRandomAdapter(this);
            }

            return systemRandom;
        }

        // An all-zero state would only ever produce zeros
        private void EnsureNonZero()
        {
            if ((s0 | s1 | s2 | s3) == 0)
            {
                s0 = 0x9E3779B9;
            }
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
    }
}
