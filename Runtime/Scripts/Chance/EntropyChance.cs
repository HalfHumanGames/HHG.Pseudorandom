using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class EntropyChance
    {
        // Negative means not started; the first roll seeds it with a random value
        [SerializeField] private float entropy = -1f;

        public bool Roll(float chance, IRandom rng = null)
        {
            if (chance <= 0f)
            {
                return false;
            }

            if (chance >= 1f)
            {
                return true;
            }

            if (entropy < 0f)
            {
                entropy = Randomizer.OrDefault(rng).Value();
            }

            // Exactly one success each time the accumulated chance passes 1
            entropy += chance;

            if (entropy >= 1f)
            {
                entropy -= 1f;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            entropy = -1f;
        }
    }
}
