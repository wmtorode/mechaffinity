using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    class AffinityLevel
    {
        public int missionsRequired = 0;
        public string levelName = "sample";
        public List<Affinity> affinities = new List<Affinity>();
    }
}
