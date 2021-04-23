using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechAffinity.Data
{
    class Settings
    {
        public bool debug = false;
        public List<AffinityLevel> globalAffinities = new List<AffinityLevel>();
        public List<ChassisSpecificAffinity> chassisAffinities = new List<ChassisSpecificAffinity>();
        public List<QuirkAffinity> quirkAffinities = new List<QuirkAffinity>();
        public List<TaggedAffinity> taggedAffinities = new List<TaggedAffinity>();
        public List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
        public int missionsBeforeDecay = -1;
        public int lowestPossibleDecay = 0;
        public int removeAffinityAfter = 100;
        public int maxAffinityPoints = 1000;
        public bool decayByModulo = false;
        public string debugForceTag = "";
        public int defaultDaysBeforeSimDecay = -1;
        public bool showQuirks = false;
        public bool showDescriptionsOnChassis = false;
        public bool trackSimDecayByStat = true;
        public bool trackLowestDecayByStat = false;
        public bool showAllPilotAffinities = true;
        public int topAffinitiesInTooltipCount = 3;
        
        public bool MechMaintenanceByCost = false;
        public float MMBC_PercentageOfMechCost = 0.003f;
        public bool MMBC_CostByTons = false;
        public int MMBC_cbillsPerTon = 500;
        public bool MMBC_TonsAdditive = false;
        
        public bool enablePilotQuirks = false;
        public List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
        public List<QuirkPool> quirkPools = new List<QuirkPool>();
        public bool playerQuirkPools = false;
        public bool pqArgoAdditive = true;
        public bool pqArgoMultiAutoAdjust = true;
        public float pqArgoMin = 0.0f;
    }
}
