using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BattleTech;
using Newtonsoft.Json.Linq;

namespace MechAffinity.Data
{
    class AffinityLevel
    {
        public int missionsRequired = 0;
        public string levelName = "sample";
        public string decription = "";
        public List<Affinity> affinities = new List<Affinity>();
        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectData = new List<JObject>();
    }
}
