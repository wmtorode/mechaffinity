using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    class ChassisSpecificAffinity
    {
        public List<string> chassisNames = new List<string> ();
        public List<AffinityLevel> affinityLevels = new List<AffinityLevel>();
        
        [JsonConverter(typeof(StringEnumConverter))]
        public EIdType idType = EIdType.AssemblyVariant;
    }
}
