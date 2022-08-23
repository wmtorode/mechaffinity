using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    public class ChassisSpecificAffinity: LeveledAffinity {
        public List<string> chassisNames = new List<string> ();
        
        [JsonConverter(typeof(StringEnumConverter))]
        public EIdType idType = EIdType.AssemblyVariant;

        public List<ChassisTypeMap> altMaps = new List<ChassisTypeMap>();
    }
}
