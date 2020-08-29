using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BattleTech;
using Newtonsoft.Json.Linq;

namespace MechAffinity.Data
{
    class PilotQuirk
    {
        public string tag = "";
        public string quirkName = "";
        public string description = "";
 
        [JsonIgnore]
        public List<EffectData> effects = new List<EffectData>();
        public List<JObject> effectData = new List<JObject>();
        public List<QuirkEffect> quirkEffects = new List<QuirkEffect>();
    }
}
