using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechAffinity.Data
{
    public class Affinity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EAffinityType type = EAffinityType.Tactics;

        public int bonus = 0;
    }
}
