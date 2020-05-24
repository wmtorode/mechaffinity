using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechAffinity.Data;
using BattleTech;
using CustomComponents;
using CustomSalvage;

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

        private string getPrefabId(AbstractActor actor)
        {
            Mech mech = actor as Mech;
            if (mech != null)
            {
                if (mech.MechDef.Chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                    return a.PrefabID + mech.MechDef.Chassis.Tonnage.ToString();

                return mech.MechDef.Chassis.PrefabIdentifier + mech.MechDef.Chassis.Tonnage.ToString();

            }
                return null;
        }

        private string getAffinityStatName(AbstractActor actor)
        {
            Pilot pilot = actor.GetPilot();
            if (pilot == null)
            {
                Main.modLog.DebugMessage("Null Pilot found!");
                return null;
            }
            string statName = $"{MA_Deployment_Stat}_{pilot.GUID}_{getPrefabId(actor)}";
            return statName;
        }

        public int getDeploymentCountWithMech(AbstractActor actor)
        {
            string statName = getAffinityStatName(actor);
            if (companyStats.ContainsStatistic(statName))
            {
                return companyStats.GetValue<int>(statName);
            }
            companyStats.AddStatistic<int>(statName, 0);
            return 0;
        }

        public void incrementDeployCountWithMech(AbstractActor actor)
        {
            string statName = getAffinityStatName(actor);
            if (companyStats.ContainsStatistic(statName))
            {
                int stat = companyStats.GetValue<int>(statName);
                stat++;
                companyStats.Set<int>(statName, stat);
            }
            companyStats.AddStatistic<int>(statName, 0);
        }
    }
}
