using Newtonsoft.Json;
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

        public bool enablePilotQuirks = false;
        public bool playerQuirkPools = false;
        public bool pqArgoAdditive = true;
        public bool pqArgoMultiAutoAdjust = true;
        public float pqArgoMin = 0.0f;

        public bool enablePilotSelect = false;

        [JsonIgnore]
        private Dictionary<string, AffinityLevel> globalAffinities_dict = new Dictionary<string, AffinityLevel>();
        [JsonIgnore]
        private Dictionary<string, ChassisSpecificAffinity> chassisAffinities_dict = new Dictionary<string, ChassisSpecificAffinity>();
        [JsonIgnore]
        private Dictionary<string, QuirkAffinity> quirkAffinities_dict = new Dictionary<string, QuirkAffinity>();
        [JsonIgnore]
        private Dictionary<string, TaggedAffinity> taggedAffinities_dict = new Dictionary<string, TaggedAffinity>();
        [JsonIgnore]
        private Dictionary<string, PilotQuirk> pilotQuirks_dict = new Dictionary<string, PilotQuirk>();


        public List<QuirkPool> quirkPools = new List<QuirkPool>();
        public List<PilotTooltipTag> pqTooltipTags = new List<PilotTooltipTag>();

        public List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
        public List<AffinityLevel> globalAffinities = new List<AffinityLevel>();
        public List<ChassisSpecificAffinity> chassisAffinities = new List<ChassisSpecificAffinity>();
        public List<QuirkAffinity> quirkAffinities = new List<QuirkAffinity>();
        public List<TaggedAffinity> taggedAffinities = new List<TaggedAffinity>();

        public List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
    private static int unique_id_counter = 0;
    private static HashSet<string> used_unique_Ids = new HashSet<string>();
    public static string createId(string pattern) { return pattern.Replace(" ","_").Replace(".","_").Replace("!","_").Replace("!", "_").Replace("@", "_"); }
    public static string createUniqueId(string pattern = null) {
      if (string.IsNullOrEmpty(pattern)) { pattern = "please_fill_it_with_some_unique_id"; }
      else{ pattern = Settings.createId(pattern); }
      string result = pattern;
      while (used_unique_Ids.Contains(result)) { 
        result = string.Format("{0}_{1}", pattern, unique_id_counter);
        ++unique_id_counter;
      };
      used_unique_Ids.Add(result);
      return result;
    }
    public void Merge_globalAffinities(List<AffinityLevel> add_globalAffinities) {
      foreach (AffinityLevel new_lvl in add_globalAffinities) {
        if(this.globalAffinities_dict.TryGetValue(new_lvl.id, out AffinityLevel old_lvl)) {
          old_lvl.affinities.AddRange(new_lvl.affinities);
          old_lvl.levelName = new_lvl.levelName;
          old_lvl.decription = new_lvl.decription;
          old_lvl.effectData.AddRange(new_lvl.effectData);
        } else {
          this.globalAffinities.Add(new_lvl);
          this.globalAffinities_dict.Add(new_lvl.id, new_lvl);
        }
      }
    }
    public void Merge_chassisAffinities(List<ChassisSpecificAffinity> add_chassisAffinities) {
      foreach (ChassisSpecificAffinity new_affinity in add_chassisAffinities) {
        if (this.chassisAffinities_dict.TryGetValue(new_affinity.id, out ChassisSpecificAffinity old_affinity)) {
          old_affinity.Merge(new_affinity.affinityLevels);
          old_affinity.chassisNames.AddRange(new_affinity.chassisNames);
        } else {
          this.chassisAffinities.Add(new_affinity);
          this.chassisAffinities_dict.Add(new_affinity.id, new_affinity);
        }
      }
    }
    public void Merge_quirkAffinities(List<QuirkAffinity> add_quirkAffinities) {
      foreach (QuirkAffinity new_affinity in add_quirkAffinities) {
        if (this.quirkAffinities_dict.TryGetValue(new_affinity.id, out QuirkAffinity old_affinity)) {
          old_affinity.Merge(new_affinity.affinityLevels);
          old_affinity.quirkNames.AddRange(new_affinity.quirkNames);
        } else {
          this.quirkAffinities.Add(new_affinity);
          this.quirkAffinities_dict.Add(new_affinity.id, new_affinity);
        }
      }
    }
    public void Merge_taggedAffinities(List<TaggedAffinity> add_taggedAffinities) {
      foreach (TaggedAffinity new_affinity in add_taggedAffinities) {
        if (this.taggedAffinities_dict.TryGetValue(new_affinity.id, out TaggedAffinity old_affinity)) {
          old_affinity.Merge(new_affinity.affinityLevels);
          old_affinity.tag = new_affinity.tag;
        } else {
          this.taggedAffinities.Add(new_affinity);
          this.taggedAffinities_dict.Add(new_affinity.id, new_affinity);
        }
      }
    }
    public void Merge_pilotQuirks(List<PilotQuirk> add_pilotQuirks) {
      foreach (PilotQuirk new_affinity in add_pilotQuirks) {
        if (this.pilotQuirks_dict.TryGetValue(new_affinity.tag, out PilotQuirk old_affinity)) {
          old_affinity.quirkEffects.AddRange(new_affinity.quirkEffects);
          old_affinity.effectData.AddRange(new_affinity.effectData);
        } else {
          this.pilotQuirks.Add(new_affinity);
          this.pilotQuirks_dict.Add(new_affinity.tag, new_affinity);
        }
      }
    }
    public void Merge(Settings add_settings) {
      this.Merge_pilotQuirks(add_settings.pilotQuirks);
      this.Merge_globalAffinities(add_settings.globalAffinities);
      this.Merge_chassisAffinities(add_settings.chassisAffinities);
      this.Merge_quirkAffinities(add_settings.quirkAffinities);
      this.Merge_taggedAffinities(add_settings.taggedAffinities);
    }
    public bool Check(string filename) {
      bool result = false;
      foreach (PilotQuirk item in this.pilotQuirks) {
        if (string.IsNullOrEmpty(item.tag)) { result = true; item.tag = createUniqueId(item.tag); }
        if (globalAffinities_dict.ContainsKey(item.tag)) { throw new Exception("pilotQuirk id duplication detected " + item.tag + " in file " + filename); }
        pilotQuirks_dict.Add(item.tag, item);
      }
      foreach (AffinityLevel item in this.globalAffinities) {
        if (string.IsNullOrEmpty(item.id)) { result = true; item.id = createUniqueId(item.levelName); }
        if (globalAffinities_dict.ContainsKey(item.id)) { throw new Exception("globalAffinity id duplication detected "+ item.id + " in file " + filename); }
        globalAffinities_dict.Add(item.id, item);
      }
      foreach (ChassisSpecificAffinity item in this.chassisAffinities) {
        if (string.IsNullOrEmpty(item.id)) { result = true; item.id = createUniqueId(); }
        if (chassisAffinities_dict.ContainsKey(item.id)) { throw new Exception("chassisAffinity id duplication detected " + item.id + " in file " + filename); }
        chassisAffinities_dict.Add(item.id, item);
        if (item.Check(filename)) { result = true; }
      }
      foreach (QuirkAffinity item in this.quirkAffinities) {
        if (string.IsNullOrEmpty(item.id)) { result = true; item.id = createUniqueId(); }
        if (quirkAffinities_dict.ContainsKey(item.id)) { throw new Exception("quirkAffinities id duplication detected " + item.id + " in file " + filename); }
        quirkAffinities_dict.Add(item.id, item);
        if (item.Check(filename)) { result = true; }
      }
      foreach (TaggedAffinity item in this.taggedAffinities) {
        if (string.IsNullOrEmpty(item.id)) { result = true; item.id = createUniqueId(); }
        if (taggedAffinities_dict.ContainsKey(item.id)) { throw new Exception("taggedAffinities id duplication detected " + item.id + " in file " + filename); }
        taggedAffinities_dict.Add(item.id, item);
        if (item.Check(filename)) { result = true; }
      }
      return result;
    }
    }
}
