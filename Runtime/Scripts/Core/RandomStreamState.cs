using System;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public struct RandomStreamState
    {
        public uint S0;
        public uint S1;
        public uint S2;
        public uint S3;
    }
}
