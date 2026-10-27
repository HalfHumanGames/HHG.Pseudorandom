using System;
using UnityEngine;

namespace HHG.Pseudorandom.Runtime
{
    [Serializable]
    public class LossGuarantee
    {
        [SerializeField, Range(0f, 1f)] private float wantedChance = 0.5f;
        [SerializeField, Min(1)] private int lossesBeforeGuarantee = 1;

        [SerializeField, HideInInspector] private int losses;

        public bool IsGuaranteed => losses >= lossesBeforeGuarantee;

        public LossGuarantee()
        {

        }

        public LossGuarantee(float wantedChance, int lossesBeforeGuarantee = 1)
        {
            this.wantedChance = wantedChance;
            this.lossesBeforeGuarantee = lossesBeforeGuarantee;
        }

        public bool RollWanted(IRandom rng = null)
        {
            bool wanted = IsGuaranteed || Randomizer.OrDefault(rng).Value() < wantedChance;
            losses = wanted ? 0 : losses + 1;
            return wanted;
        }

        public void Reset()
        {
            losses = 0;
        }
    }
}
