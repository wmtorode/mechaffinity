using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechAffinity.Data
{
    public class PilotTagStabilityEffect
    {
        public string tag = "";
        public float effect = 0f;
        [JsonConverter(typeof(StringEnumConverter))]
        public EStabilityEffectType type = EStabilityEffectType.Flat;
    }
}