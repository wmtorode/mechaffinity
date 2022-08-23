using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MechAffinity.Data
{
    public class QuirkEffect
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EQuirkEffectType type = EQuirkEffectType.MedTech;
        public float modifier = 0;
        public float secondaryModifier = 0;
        public List<string> affectedIds = new List<string>();
    }
}
