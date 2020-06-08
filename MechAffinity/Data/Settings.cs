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
        public List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
        public int missionsBeforeDecay = -1;
        public int lowestPossibleDecay = 0;
        public int removeAffinityAfter = 100;
        public bool decayByModulo = false;
    }
}
