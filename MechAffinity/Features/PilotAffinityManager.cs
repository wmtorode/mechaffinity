using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechAffinity.Data;
using BattleTech;

#if USE_CS_CC
using CustomComponents;
using CustomSalvage;
#endif

namespace MechAffinity
{
    class PilotAffinityManager
    {
        private static readonly string MA_Deployment_Stat = "MaDeployStat_";
        private static PilotAffinityManager instance;
        private StatCollection companyStats;
        private Dictionary<string, List<AffinityLevel>> chassisAffinities;

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
            chassisAffinities = new Dictionary<string, List<AffinityLevel>>();
            foreach( ChassisSpecificAffinity chassisSpecific in Main.settings.chassisAffinities)
            {
                chassisAffinities.Add(chassisSpecific.chassisName, chassisSpecific.affinityLevels);
            }
        }

        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
        }

        private string getPrefabId(MechDef mech)
        {
                #if USE_CS_CC
                    if (mech.Chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                        return a.PrefabID + mech.MechDef.Chassis.Tonnage.ToString();
                #endif

            return $"{mech.Chassis.PrefabIdentifier}_{mech.Chassis.Tonnage}";
        }

        private string getPrefabId(AbstractActor actor)
        {
            Mech mech = actor as Mech;
            if (mech != null)
            {
                return getPrefabId(mech.MechDef);

            }
                return null;
        }

        private string getAffinityStatName(AbstractActor actor)
        {
            Pilot pilot = actor.GetPilot();
            if (pilot == null)
            {
                Main.modLog.DebugMessage("Null Pilot found!");
                return $"{MA_Deployment_Stat}";
            }
            string prefab = getPrefabId(actor);
            if (String.IsNullOrEmpty(prefab))
            {
                Main.modLog.DebugMessage("Null Prefab!");
                return $"{MA_Deployment_Stat}_{pilot.pilotDef.Description.Id}";
            }
            string statName = $"{MA_Deployment_Stat}_{pilot.pilotDef.Description.Id}_{getPrefabId(actor)}";
            return statName;
        }

        private string getAffinityStatName(UnitResult result)
        {
            string statName = $"{MA_Deployment_Stat}_{result.pilot.pilotDef.Description.Id}_{getPrefabId(result.mech)}";
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

        public void incrementDeployCountWithMech(string statName)
        {
            Main.modLog.LogMessage($"Incrementing DeployCount stat {statName}");
            if (companyStats.ContainsStatistic(statName))
            {
                int stat = companyStats.GetValue<int>(statName);
                stat++;
                companyStats.Set<int>(statName, stat);
            }
            companyStats.AddStatistic<int>(statName, 0);
        }

        public void incrementDeployCountWithMech(UnitResult result)
        {
            string statName = getAffinityStatName(result);
            incrementDeployCountWithMech(statName);
        }

        public void incrementDeployCountWithMech(AbstractActor actor)
        {
            string statName = getAffinityStatName(actor);
            incrementDeployCountWithMech(statName);
        }

        private Dictionary<EAffinityType, int> getDeploymentBonus(AbstractActor actor)
        {
            Dictionary<EAffinityType, int> bonuses = new Dictionary<EAffinityType, int>();
            int deployCount = getDeploymentCountWithMech(actor);
            string chassisPrefab = getPrefabId(actor);
            string statName = getAffinityStatName(actor);
            Main.modLog.LogMessage($"Processing Pilot/Mech Combo {statName}");

            foreach (AffinityLevel affinityLevel in Main.settings.globalAffinities)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Global Level {affinityLevel.levelName}");
                    foreach(Affinity affinity in affinityLevel.affinities)
                    {
                        if (bonuses.ContainsKey(affinity.type))
                        {
                            bonuses[affinity.type] += affinity.bonus;
                        }
                        else
                        {
                            bonuses.Add(affinity.type, affinity.bonus);
                        }
                    }
                }
            }
            if(chassisAffinities.ContainsKey(chassisPrefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[chassisPrefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (deployCount >= affinityLevel.missionsRequired)
                    {
                        Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Chassis Specific Level {affinityLevel.levelName}");
                        foreach (Affinity affinity in affinityLevel.affinities)
                        {
                            if (bonuses.ContainsKey(affinity.type))
                            {
                                bonuses[affinity.type] += affinity.bonus;
                            }
                            else
                            {
                                bonuses.Add(affinity.type, affinity.bonus);
                            }
                        }
                    }
                }
            }

            return bonuses;
        }

        private void applyStatBonuses(AbstractActor actor, Dictionary<EAffinityType, int> bonuses)
        {
            if (actor.GetPilot() == null)
            {
                Main.modLog.LogMessage("Cannot Apply Bonuses to target, no pilot available");
                return;
            }
            StatCollection pilotStats = actor.GetPilot().StatCollection;
            if (bonuses.ContainsKey(EAffinityType.Tactics))
            {
                Statistic stat = pilotStats.GetStatistic("Tactics");
                pilotStats.Int_Add(stat, bonuses[EAffinityType.Tactics]);
            }
            if (bonuses.ContainsKey(EAffinityType.Guts))
            {
                Statistic stat = pilotStats.GetStatistic("Guts");
                pilotStats.Int_Add(stat, bonuses[EAffinityType.Guts]);
            }
            if (bonuses.ContainsKey(EAffinityType.Gunnery))
            {
                Statistic stat = pilotStats.GetStatistic("Gunnery");
                pilotStats.Int_Add(stat, bonuses[EAffinityType.Gunnery]);
            }
            if (bonuses.ContainsKey(EAffinityType.Piloting))
            {
                Statistic stat = pilotStats.GetStatistic("Piloting");
                pilotStats.Int_Add(stat, bonuses[EAffinityType.Piloting]);
            }
        }

        public void applyBonuses(AbstractActor actor)
        {
            if (actor.team == null || !actor.team.IsLocalPlayer)
            {
                // actor isnt part of our team, dont record them
                Main.modLog.DebugMessage($"Skipping actor: {getAffinityStatName(actor)}");
                return;
            }
            Dictionary<EAffinityType, int> bonuses = getDeploymentBonus(actor);
            if (Main.settings.debug)
            {
                foreach (KeyValuePair<EAffinityType, int> bonus in bonuses)
                {
                    Main.modLog.DebugMessage($"Appling Bonus: {bonus.Key.ToString()} with strength: {bonus.Value}");
                }
            }
            applyStatBonuses(actor, bonuses);
        }

    }

}
