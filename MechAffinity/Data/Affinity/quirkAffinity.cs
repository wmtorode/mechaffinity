using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechAffinity.Data
{
    public class LeveledAffinity {
      [JsonProperty(Order = -2, NullValueHandling = NullValueHandling.Ignore)]
      public string id;
      [JsonProperty(Order = -1)]
      public List<AffinityLevel> affinityLevels = new List<AffinityLevel>();

    }
    public class QuirkAffinity: LeveledAffinity {
        public List<string> quirkNames = new List<string>();
    }
}
