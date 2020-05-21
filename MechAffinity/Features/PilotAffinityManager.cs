using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechAffinity.Data;
using BattleTech;

namespace MechAffinity
{
    class PilotAffinityManager
    {
        private static readonly string MA_Deployment_Stat = "MA_DeployStat_";
        private static PilotAffinityManager instance;
        private StatCollection companyStats;

        public static PilotAffinityManager Instance
        {
            get
            {
                if (instance == null) instance = new PilotAffinityManager();
                return instance;
            }
        }

        public void initialize()
        {

        }

        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
        }
    }
}
