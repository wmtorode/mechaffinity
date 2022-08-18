using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace MechAffinity.Data
{
    public class AffinityDef
    {
        public string id = "";
        [JsonConverter(typeof(StringEnumConverter))]
        public EAffinityDefType affinityType = EAffinityDefType.Global;
        public JObject affinityData = new JObject();
        

        public AffinityLevel getGlobalAffinity()
        {
            AffinityLevel affinity = JsonConvert.DeserializeObject<AffinityLevel>(affinityData.ToString());
            affinity.id = id;
            return affinity;
        }
        
        public ChassisSpecificAffinity getChassisAffinity()
        {
            ChassisSpecificAffinity affinity = JsonConvert.DeserializeObject<ChassisSpecificAffinity>(affinityData.ToString());
            affinity.id = id;
            return affinity;
        }
        
        public QuirkAffinity getQuirkAffinity()
        {
            QuirkAffinity affinity = JsonConvert.DeserializeObject<QuirkAffinity>(affinityData.ToString());
            affinity.id = id;
            return affinity;
        }
        
        public TaggedAffinity getTaggedAffinity()
        {
            TaggedAffinity affinity = JsonConvert.DeserializeObject<TaggedAffinity>(affinityData.ToString());
            affinity.id = id;
            return affinity;
        }

        public void setAffinityData(AffinityLevel affinityLevel)
        {
            affinityData = JObject.FromObject(affinityLevel);
        }
        
        public void setAffinityData(ChassisSpecificAffinity affinityLevel)
        {
            affinityData = JObject.FromObject(affinityLevel);
        }
        
        public void setAffinityData(QuirkAffinity affinityLevel)
        {
            affinityData = JObject.FromObject(affinityLevel);
        }
        
        public void setAffinityData(TaggedAffinity affinityLevel)
        {
            affinityData = JObject.FromObject(affinityLevel);
        }
    }
}