using System.Collections.Generic;
using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace MechAffinity.Data;

public class LanceQuirkDef
{
    public string id = "";
    public List<string> tags = new List<string>();
    
    [JsonConverter(typeof(StringEnumConverter))]
    public ELanceQuirkSelector selector = ELanceQuirkSelector.All;
    public string quirkName = "";
    public string description = "";
    public string restrictionCategory = "";

    [JsonIgnore]
    public List<EffectData> effects = new List<EffectData>();
    public List<JObject> effectData = new List<JObject>();
    public List<QuirkEffect> quirkEffects = new List<QuirkEffect>();

    public void init()
    {
        foreach (JObject jObject in effectData)
        {
            EffectData effectData = new EffectData();
            effectData.FromJSON(jObject.ToString());
            effects.Add(effectData);
        }
    }

}