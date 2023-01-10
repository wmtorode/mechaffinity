using System.Collections.Generic;

namespace MechAffinity.Data
{
    public class PilotAffinitySettings
    {
        public int missionsBeforeDecay = -1;
        public int lowestPossibleDecay = 0;
        public int removeAffinityAfter = 100;
        public int maxAffinityPoints = 1000;
        public bool decayByModulo = false;
        public string debugForceTag = "";
        public int defaultDaysBeforeSimDecay = -1;
        public bool showDescriptionsOnChassis = false;
        public bool trackSimDecayByStat = true;
        public bool trackLowestDecayByStat = false;
        public bool showAllPilotAffinities = true;
        public int topAffinitiesInTooltipCount = 3;
        public bool showQuirks = false;
        public bool treatDefaultsAsFixed = false;
        public List<AffinityGroup> affinityGroups = new List<AffinityGroup>();
        public List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
    }
}