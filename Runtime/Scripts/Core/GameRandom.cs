using System;
using System.Collections.Generic;

namespace HHG.Pseudorandom.Runtime
{
    public static class GameRandom
    {
        private const string defaultStreamName = "Default";

        private static readonly Dictionary<string, RandomStream> streams = new Dictionary<string, RandomStream>(StringComparer.Ordinal);
        private static RandomStream defaultStream;
        private static ulong seed;

        static GameRandom()
        {
            InitState(SeedUtil.NewSeed());
        }

        public static ulong Seed => seed;

        public static RandomStream Default => defaultStream;

        public static float Value => defaultStream.Value();

        public static void InitState(ulong newSeed)
        {
            seed = newSeed;
            defaultStream = new RandomStream(SeedUtil.Derive(newSeed, defaultStreamName));
            streams.Clear();
        }

        public static void InitState(int newSeed)
        {
            InitState((ulong)(uint)newSeed);
        }

        public static void InitState(string seedText)
        {
            InitState(SeedUtil.FromString(seedText));
        }

        // Streams are created lazily, each seeded from the main seed and its name,
        // so rolling on one stream never shifts the sequence of another
        public static RandomStream Stream(string name)
        {
            if (string.IsNullOrEmpty(name) || name == defaultStreamName)
            {
                return defaultStream;
            }

            RandomStream stream;

            if (!streams.TryGetValue(name, out stream))
            {
                stream = new RandomStream(SeedUtil.Derive(seed, name));
                streams.Add(name, stream);
            }

            return stream;
        }

        public static IRandom OrDefault(IRandom rng)
        {
            return rng ?? defaultStream;
        }

        // Reseeds one stream until the returned scope is disposed, then restores it
        public static IDisposable Reseed(string streamName, int scopedSeed)
        {
            RandomStream stream = Stream(streamName);
            RandomStreamState previous = stream.GetState();
            stream.InitState(scopedSeed);
            return new ReseedScope(stream, previous);
        }

        public static float Range(float min, float max)
        {
            return defaultStream.Range(min, max);
        }

        public static int Range(int min, int max)
        {
            return defaultStream.Range(min, max);
        }

        public static bool Chance(float chance)
        {
            return defaultStream.Chance(chance);
        }

        public static GameRandomState GetState()
        {
            GameRandomState state = new GameRandomState
            {
                Seed = seed,
                Default = defaultStream.GetState()
            };

            foreach (KeyValuePair<string, RandomStream> pair in streams)
            {
                state.Streams.Add(new NamedStreamState { Name = pair.Key, State = pair.Value.GetState() });
            }

            return state;
        }

        public static void SetState(GameRandomState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            seed = state.Seed;
            defaultStream = new RandomStream(state.Default);
            streams.Clear();

            if (state.Streams != null)
            {
                foreach (NamedStreamState named in state.Streams)
                {
                    if (!string.IsNullOrEmpty(named.Name))
                    {
                        streams[named.Name] = new RandomStream(named.State);
                    }
                }
            }
        }
    }
}
