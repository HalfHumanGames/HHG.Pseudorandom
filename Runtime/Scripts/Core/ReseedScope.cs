using System;

namespace HHG.Pseudorandom.Runtime
{
    internal sealed class ReseedScope : IDisposable
    {
        private RandomStream stream;
        private readonly RandomStreamState previous;

        public ReseedScope(RandomStream stream, RandomStreamState previous)
        {
            this.stream = stream;
            this.previous = previous;
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.SetState(previous);
                stream = null;
            }
        }
    }
}
