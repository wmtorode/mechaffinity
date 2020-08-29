using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using MechAffinity.Data;

namespace MechAffinity
{
    class PilotQuirkManager
    {
        private static PilotQuirkManager _instance;
        private StatCollection companyStats;
        private Dictionary<string, List<PilotQuirk>> quirks;

        public static PilotQuirkManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotQuirkManager();
                return _instance;
            }
        }

        public void initialize()
        {
        }
    }
}
