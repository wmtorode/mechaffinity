using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class StablePilotingSettings
    {
        public float reductionPerPiloting = 0.02f;
        public float increasePerInjury = 0.05f;
        public List<PilotTagStabilityEffect> tagEffects = new List<PilotTagStabilityEffect>();
        public int InverseMax = 20;
    }
}