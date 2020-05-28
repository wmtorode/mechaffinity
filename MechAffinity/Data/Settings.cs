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
        public List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
    }
}
