using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechAffinity.Data;
using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if USE_CS_CC
using CustomComponents;
using CustomSalvage;
#endif

namespace MechAffinity
{
    class PilotAffinityManager
    {
        private static readonly string MA_Deployment_Stat = "MaDeployStat=";
        private static readonly string MA_Lut_Stat = "MaLutStat";
        private static PilotAffinityManager instance;
        private StatCollection companyStats;
        private Dictionary<string, List<AffinityLevel>> chassisAffinities;
        private Dictionary<string, List<string>> pilotStatMap;
        private Dictionary<string, string> chassisPrefabLut;
        private Dictionary<string, string> prefabOverrides;
        private Dictionary<string, string> levelDescriptors;
        private int uid;

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
            prefabOverrides = new Dictionary<string, string>();
            chassisPrefabLut = new Dictionary<string, string>();
            levelDescriptors = new Dictionary<string, string>();
            foreach (ChassisSpecificAffinity chassisSpecific in Main.settings.chassisAffinities)
            {
                chassisAffinities.Add(chassisSpecific.chassisName, chassisSpecific.affinityLevels);
                foreach(AffinityLevel affinityLevel in chassisSpecific.affinityLevels)
                {
                    levelDescriptors[affinityLevel.levelName] = affinityLevel.decription;
                    foreach (JObject jObject in affinityLevel.effectData)
                    {
                        EffectData effectData = new EffectData();
                        effectData.FromJSON(jObject.ToString());
                        affinityLevel.effects.Add(effectData);
                    }
                }
            }
            foreach(AffinityLevel affinity in Main.settings.globalAffinities)
            {
                foreach (JObject jObject in affinity.effectData)
                {
                    EffectData effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    affinity.effects.Add(effectData);
                }
            }
            foreach (PrefabOverride overRide in Main.settings.prefabOverrides)
            {
                prefabOverrides[overRide.prefabId] = overRide.overrideName;
            }
            foreach (AffinityLevel affinityLevel in Main.settings.globalAffinities)
            {
                levelDescriptors[affinityLevel.levelName] = affinityLevel.decription;
            }
        }

        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
            pilotStatMap = new Dictionary<string, List<string>>();
            chassisPrefabLut = new Dictionary<string, string>();
            uid = 0;

            //find all mechs a given pilot has experience with and cache for later
            foreach (KeyValuePair<string, Statistic> keypair in companyStats)
            {
                if (keypair.Key.StartsWith(MA_Deployment_Stat))
                {
                    addtoPilotMap(keypair.Key);
                }
            }
            if (companyStats.ContainsStatistic(MA_Lut_Stat))
            {
                chassisPrefabLut = JsonConvert.DeserializeObject<Dictionary<string, string>>(companyStats.GetValue<string>(MA_Lut_Stat));
            }
            else
            {
                companyStats.AddStatistic<string>(MA_Lut_Stat, JsonConvert.SerializeObject(chassisPrefabLut, Formatting.None));
            }
        }

        private void addtoPilotMap(string statName)
        {
            string pilotId = statName.Split('=')[1];
            string chassisId = statName.Split('=')[2];
            if (!pilotStatMap.ContainsKey(pilotId))
            {
                pilotStatMap[pilotId] = new List<string>();
            }
            if (!pilotStatMap[pilotId].Contains(chassisId))
            {
                pilotStatMap[pilotId].Add(chassisId);
            }
        }

        private string getPrefabId(MechDef mech)
        {
            #if USE_CS_CC
                if (mech.Chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                    return a.PrefabID + "_" + mech.Chassis.Tonnage.ToString();
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
                return $"{MA_Deployment_Stat}{pilot.pilotDef.Description.Id}";
            }
            string statName = $"{MA_Deployment_Stat}{pilot.pilotDef.Description.Id}={getPrefabId(actor)}";
            return statName;
        }

        private string getAffinityStatName(UnitResult result)
        {
            string prefabId = getPrefabId(result.mech);
            // chache the last known chassis name of the prefab in question
            bool needToUpdate = !chassisPrefabLut.ContainsKey(prefabId);
            chassisPrefabLut[prefabId] = result.mech.Chassis.Description.Name;
            string statName = $"{MA_Deployment_Stat}{result.pilot.pilotDef.Description.Id}={prefabId}";
            if (needToUpdate)
            {
                if (!prefabOverrides.ContainsKey(prefabId))
                {
                    companyStats.Set<string>(MA_Lut_Stat, JsonConvert.SerializeObject(chassisPrefabLut, Formatting.None));
                }
            }
            return statName;
        }

        public int getDeploymentCountWithMech(string statName)
        {
            if (companyStats.ContainsStatistic(statName))
            {
                return companyStats.GetValue<int>(statName);
            }
            return 0;
        }

        public int getDeploymentCountWithMech(AbstractActor actor)
        {
            string statName = getAffinityStatName(actor);
            return getDeploymentCountWithMech(statName);
            
        }

        public void incrementDeployCountWithMech(string statName)
        {
            Main.modLog.LogMessage($"Incrementing DeployCount stat {statName}");
            addtoPilotMap(statName);
            if (companyStats.ContainsStatistic(statName))
            {
                int stat = companyStats.GetValue<int>(statName);
                stat++;
                companyStats.Set<int>(statName, stat);
                return;
            }
            // we dont have the stat yet so just set it to 1
            companyStats.AddStatistic<int>(statName, 1);
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

        private string getHighestLevelName(string statName, string prefab)
        {
            string ret = "";
            int maxSoFar = 0;
            int deployCount = getDeploymentCountWithMech(statName);
            //Main.modLog.LogMessage($"Deployment Count: {deployCount}");

            foreach (AffinityLevel affinityLevel in Main.settings.globalAffinities)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    if (affinityLevel.missionsRequired >= maxSoFar)
                    {
                        maxSoFar = affinityLevel.missionsRequired;
                        ret = affinityLevel.levelName;
                    }
                }

            }
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (deployCount >= affinityLevel.missionsRequired)
                    {
                        if (affinityLevel.missionsRequired >= maxSoFar)
                        {
                            maxSoFar = affinityLevel.missionsRequired;
                            ret = affinityLevel.levelName;
                        }
                    }
                }
            }

            return ret;
        }

        public string getMechAffinityDescription(Pilot pilot)
        {
            string pilotId = pilot.pilotDef.Description.Id;
            Dictionary<string, List<string>> affinites = new Dictionary<string, List<string>>();
            if (pilotStatMap.ContainsKey(pilotId))
            {
                foreach(string chassisId in pilotStatMap[pilotId])
                {
                    string level = getHighestLevelName($"{MA_Deployment_Stat}{pilotId}={chassisId}", chassisId);
                    string chassisName = chassisId;
                    if (!string.IsNullOrEmpty(level))
                    {
                        if(!affinites.ContainsKey(level))
                        {
                            affinites[level] = new List<string>();
                        }
                        if(prefabOverrides.ContainsKey(chassisId))
                        {
                            chassisName = prefabOverrides[chassisId];
                        }
                        else
                        {
                            if(chassisPrefabLut.ContainsKey(chassisId))
                            {
                                chassisName = chassisPrefabLut[chassisId];
                            }
                        }
                        affinites[level].Add(chassisName);
                    }
                }
            }
            string ret = "\n";
            foreach(KeyValuePair<string, List<string>> level in affinites)
            {
                string descript = $"<b>{level.Key}</b>: {levelDescriptors[level.Key]}:\n";
                string mechs = string.Join("\n", level.Value);
                descript += mechs;
                ret += descript + "\n\n";
            }

            return ret;
        }


        private Dictionary<EAffinityType, int> getDeploymentBonus(AbstractActor actor, out List<EffectData> effects)
        {
            Dictionary<EAffinityType, int> bonuses = new Dictionary<EAffinityType, int>();
            effects = new List<EffectData>();
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
                    foreach(EffectData effect in affinityLevel.effects)
                    {
                        effects.Add(effect);
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
                        foreach (EffectData effect in affinityLevel.effects)
                        {
                            effects.Add(effect);
                            Main.modLog.LogMessage($"Found effect ID: {effect.Description.Id}, name: {effect.Description.Name}");
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

        private void applyStatusEffects(AbstractActor actor, List<EffectData> effects)
        {
            foreach (EffectData statusEffect in effects)
            {
                if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive)
                {
                    if (statusEffect.targetingData.effectTargetType == EffectTargetType.Creator)
                    {
                        string effectId = $"PassiveEffect_{actor.GUID}_{uid}";
                        uid++;
                        Main.modLog.LogMessage($"Applying affect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name}");
                        actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, (ICombatant)actor, (ICombatant)actor, new WeaponHitInfo(), 0, false);
                    }
                }
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
            List<EffectData> effects;
            Dictionary<EAffinityType, int> bonuses = getDeploymentBonus(actor, out effects);
            if (Main.settings.debug)
            {
                foreach (KeyValuePair<EAffinityType, int> bonus in bonuses)
                {
                    Main.modLog.DebugMessage($"Applying Bonus: {bonus.Key.ToString()} with strength: {bonus.Value}");
                }
            }
            applyStatBonuses(actor, bonuses);
            applyStatusEffects(actor, effects);
        }

    }

}
