using System;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public struct NamedStreamState
    {
        public string Name;
        public RandomStreamState State;
    }
}
