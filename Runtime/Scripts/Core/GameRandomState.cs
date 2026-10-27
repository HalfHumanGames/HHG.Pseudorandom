using System;
using System.Collections.Generic;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class GameRandomState
    {
        public ulong Seed;
        public RandomStreamState Default;
        public List<NamedStreamState> Streams = new List<NamedStreamState>();
    }
}
