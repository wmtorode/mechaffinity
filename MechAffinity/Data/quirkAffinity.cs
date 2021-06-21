using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MechAffinity.Data
{
    class LeveledAffinity {
      [JsonProperty(Order = -2)]
      public string id;
      [JsonProperty(Order = -1)]
      public List<AffinityLevel> affinityLevels = new List<AffinityLevel>();
      [JsonIgnore]
      public Dictionary<string, AffinityLevel> affinityLevels_dict = new Dictionary<string, AffinityLevel>();
      public bool Check(string filename) {
        bool result = false;
        foreach (AffinityLevel lvl in this.affinityLevels) {
          if (string.IsNullOrEmpty(lvl.id)) { result = true; lvl.id = Settings.createUniqueId(lvl.levelName); }
          if (affinityLevels_dict.ContainsKey(lvl.id)) { throw new Exception("AffinityLevel id duplication detected " + lvl.id + " in file " + filename); }
          affinityLevels_dict.Add(lvl.id, lvl);
        }
        return result;
      }
      public void Merge(List<AffinityLevel> levels) {
        foreach (AffinityLevel new_lvl in levels) {
          if (this.affinityLevels_dict.TryGetValue(new_lvl.id, out AffinityLevel old_lvl)) {
            old_lvl.affinities.AddRange(new_lvl.affinities);
            old_lvl.levelName = new_lvl.levelName;
            old_lvl.decription = new_lvl.decription;
            old_lvl.effectData.AddRange(new_lvl.effectData);
          } else {
            this.affinityLevels.Add(new_lvl);
            this.affinityLevels_dict.Add(new_lvl.id, new_lvl);
          }
        }
      }
  }
    class QuirkAffinity: LeveledAffinity {
        public List<string> quirkNames = new List<string>();
    }
}
