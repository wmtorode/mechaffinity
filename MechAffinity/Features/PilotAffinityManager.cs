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
        private static readonly string MA_Decay_Stat = "MaDecayStat=";
        private static readonly string MA_Lut_Stat = "MaLutStat";
        private static readonly string MA_DaysElapsedMod_Stat = "MaDaysSinceLastDecay=";
        private static readonly string MA_SimDaysDecayModulator_Stat = "MaSimDaysDecayModulator";
        private static readonly string MA_LowestDecayStat = "MaLowestDecay";
        private static readonly string MA_PilotDeployCountTag = "affinityLevel_";
        private static readonly string MA_NoAffinity = "No Affinity";
        private static PilotAffinityManager instance;
        private StatCollection companyStats;
        private Dictionary<string, List<AffinityLevel>> chassisAffinities;
        private Dictionary<string, List<string>> pilotStatMap;
        private Dictionary<string, string> chassisPrefabLut;
        private Dictionary<string, string> prefabOverrides;
        private Dictionary<string, DescriptionHolder> levelDescriptors;
        private Dictionary<string, List<string>> pilotNoDeployStatMap;
        private Dictionary<string, List<AffinityLevel>> quirkAffinities;
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
            quirkAffinities = new Dictionary<string, List<AffinityLevel>>();
            prefabOverrides = new Dictionary<string, string>();
            chassisPrefabLut = new Dictionary<string, string>();
            levelDescriptors = new Dictionary<string, DescriptionHolder>();
            foreach (ChassisSpecificAffinity chassisSpecific in Main.settings.chassisAffinities)
            {
                foreach (string chassisName in chassisSpecific.chassisNames)
                {
                    chassisAffinities.Add(chassisName, chassisSpecific.affinityLevels);
                }
                foreach (AffinityLevel affinityLevel in chassisSpecific.affinityLevels)
                {
                    levelDescriptors[affinityLevel.levelName] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
                    foreach (JObject jObject in affinityLevel.effectData)
                    {
                        EffectData effectData = new EffectData();
                        effectData.FromJSON(jObject.ToString());
                        affinityLevel.effects.Add(effectData);
                    }
                }
            }
            foreach (QuirkAffinity quirkAffinity in Main.settings.quirkAffinities)
            {
                foreach (string quirkName in quirkAffinity.quirkNames)
                {
                    quirkAffinities.Add(quirkName, quirkAffinity.affinityLevels);
                }
                foreach (AffinityLevel affinityLevel in quirkAffinity.affinityLevels)
                {
                    levelDescriptors[affinityLevel.levelName] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
                    foreach (JObject jObject in affinityLevel.effectData)
                    {
                        EffectData effectData = new EffectData();
                        effectData.FromJSON(jObject.ToString());
                        affinityLevel.effects.Add(effectData);
                    }
                }
            }
            foreach (AffinityLevel affinity in Main.settings.globalAffinities)
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
                levelDescriptors[affinityLevel.levelName] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
            }
            levelDescriptors[MA_NoAffinity] = new DescriptionHolder(MA_NoAffinity, "", 0);
        }

        public void addToChassisPrefabLut(MechDef mech)
        {
            
            string prefabId = getPrefabId(mech);
            chassisPrefabLut[prefabId] = mech.Chassis.Description.Name;
            Main.modLog.LogMessage($"adding to lut {prefabId} => {mech.Chassis.Description.Name}");
        }

        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
            pilotStatMap = new Dictionary<string, List<string>>();
            chassisPrefabLut = new Dictionary<string, string>();
            pilotNoDeployStatMap = new Dictionary<string, List<string>>();
            uid = 0;

            //find all mechs a given pilot has experience with and cache for later
            foreach (KeyValuePair<string, Statistic> keypair in companyStats)
            {
                if (keypair.Key.StartsWith(MA_Deployment_Stat))
                {
                    addtoPilotMap(keypair.Key);
                    addtoPilotDecayMap(convertAffinityStatToDecay(keypair.Key));
                }
                else
                {
                    if(keypair.Key.StartsWith(MA_Decay_Stat))
                    {
                        addtoPilotDecayMap(keypair.Key);
                    }
                }
            }
            if (companyStats.ContainsStatistic(MA_Lut_Stat))
            {
                companyStats.RemoveStatistic(MA_Lut_Stat);
            }
            if (!companyStats.ContainsStatistic(MA_SimDaysDecayModulator_Stat) && Main.settings.trackSimDecayByStat)
            {
                companyStats.AddStatistic<int>(MA_SimDaysDecayModulator_Stat, Main.settings.defaultDaysBeforeSimDecay);
            }
            if (!companyStats.ContainsStatistic(MA_LowestDecayStat) && Main.settings.trackLowestDecayByStat)
            {
                companyStats.AddStatistic<int>(MA_LowestDecayStat, Main.settings.lowestPossibleDecay);
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

        private void addtoPilotDecayMap(string statName)
        {
            string pilotId = statName.Split('=')[1];
            if (!pilotNoDeployStatMap.ContainsKey(pilotId))
            {
                pilotNoDeployStatMap[pilotId] = new List<string>();
            }
            if (!pilotNoDeployStatMap[pilotId].Contains(statName))
            {
                pilotNoDeployStatMap[pilotId].Add(statName);
            }
        }

        private string convertDecayStatToAffinity(string statName)
        {
            return statName.Replace(MA_Decay_Stat, MA_Deployment_Stat);
        }

        private string convertAffinityStatToDecay(string statName)
        {
            return statName.Replace(MA_Deployment_Stat, MA_Decay_Stat);
        }

        private string convertAffinityStatToSimDecay(string statName)
        {
            string pilotId = statName.Split('=')[1];
            return $"{MA_DaysElapsedMod_Stat}{pilotId}";
        }

        private string getPrefabId(ChassisDef chassis)
        {
            #if USE_CS_CC
                if (chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                    return a.PrefabID + "_" + chassis.Tonnage.ToString();
            #endif

            return $"{chassis.PrefabIdentifier}_{chassis.Tonnage}";
        }

        private string getPrefabId(MechDef mech)
        {
            return getPrefabId(mech.Chassis);
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

        private int getLowestDecay()
        {
            if (Main.settings.trackLowestDecayByStat)
            {
                return companyStats.GetValue<int>(MA_LowestDecayStat);
            }
            return Main.settings.lowestPossibleDecay;
        }

        private int getSimDecayDays()
        {
            if (Main.settings.trackSimDecayByStat)
            {
                return companyStats.GetValue<int>(MA_SimDaysDecayModulator_Stat);
            }
            return Main.settings.defaultDaysBeforeSimDecay;
        }

        private List<string> getPossibleQuirkAffinites(ChassisDef chassis)
        {
            List<string> quirks = new List<string>();
            if (chassis.FixedEquipment != null)
            {
                foreach (MechComponentRef fixedEquip in chassis.FixedEquipment)
                {
                    if (quirkAffinities.ContainsKey(fixedEquip.ComponentDefID))
                    {
                        if (!quirks.Contains(fixedEquip.ComponentDefID))
                        {
                            quirks.Add(fixedEquip.ComponentDefID);
                        }
                    }
                }
            }
            return quirks;
        }

        private List<string> getPossibleQuirkAffinites(MechDef mech)
        {
            return getPossibleQuirkAffinites(mech.Chassis);
        }

        private List<string> getPossibleQuirkAffinites(AbstractActor actor)
        {
            List<string> quirks = new List<string>();
            Mech mech = actor as Mech;
            if (mech != null)
            {
                return getPossibleQuirkAffinites(mech.MechDef);
            }

            return quirks;
        }

        private string getAffinityStatName(AbstractActor actor)
        {
            Pilot pilot = actor.GetPilot();
            if (pilot == null)
            {
                Main.modLog.LogMessage("Null Pilot found!");
                return $"{MA_Deployment_Stat}";
            }
            string prefab = getPrefabId(actor);
            if (String.IsNullOrEmpty(prefab))
            {
                Main.modLog.LogMessage("Null Prefab!");
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
        private bool shouldDecay(int decayCount)
        {
            if (Main.settings.missionsBeforeDecay != -1)
            {
                if (Main.settings.decayByModulo)
                {
                    if (decayCount != 0 && ((decayCount % Main.settings.missionsBeforeDecay) == 0))
                    {
                        return true;
                    }
                }
                else
                {
                    if(decayCount >= Main.settings.missionsBeforeDecay)
                    {
                        return true;
                    }
                }
            }
            return false;
            
        }

        private void decayAffinties(string decayStat)
        {
            if (Main.settings.missionsBeforeDecay != -1 || Main.settings.removeAffinityAfter != -1)
            {
                string pilotId = decayStat.Split('=')[1];
                List<string> decayList = pilotNoDeployStatMap[pilotId];
                List<string> toPurge = new List<string>();
                foreach (string decaying in decayList)
                {
                    string affinityStat = convertDecayStatToAffinity(decaying);
                    if (companyStats.ContainsStatistic(decaying))
                    {
                        int decayed = companyStats.GetValue<int>(decaying);
                        if (decaying == decayStat)
                        {
                            companyStats.Set<int>(decaying, 0);
                        }
                        else
                        {
                            decayed++;
                            if (Main.settings.removeAffinityAfter != -1 && decayed >= Main.settings.removeAffinityAfter)
                            {
                                companyStats.RemoveStatistic(decaying);
                                companyStats.RemoveStatistic(affinityStat);
                                toPurge.Add(decaying);
                                Main.modLog.LogMessage($"Removing Stat: {affinityStat}");
                            }
                            else
                            {
                                if (shouldDecay(decayed))
                                {
                                    if (companyStats.ContainsStatistic(affinityStat))
                                    {
                                        int deployCount = companyStats.GetValue<int>(affinityStat);
                                        if (deployCount > getLowestDecay())
                                        {
                                            deployCount--;
                                            companyStats.Set<int>(affinityStat, deployCount);
                                            Main.modLog.LogMessage($"decaying stat {affinityStat}, new value: {deployCount}");
                                        }
                                    }
                                    else
                                    {
                                        Main.modLog.LogError($"Failed to decay stat {affinityStat}");
                                    }
                                }
                                companyStats.Set<int>(decaying, decayed);
                            }
                        }
                    }
                    else
                    {
                        companyStats.AddStatistic<int>(decaying, 0);
                    }
                }
                foreach (string purge in toPurge)
                {
                    pilotNoDeployStatMap[pilotId].Remove(purge);
                }
            }
        }

        private bool simDayDecay(string pilotId)
        {
            if (pilotNoDeployStatMap.ContainsKey(pilotId))
            {
                List<string> decayList = pilotNoDeployStatMap[pilotId];
                foreach (string decaying in decayList)
                {
                    string affinityStat = convertDecayStatToAffinity(decaying);
                    if (companyStats.ContainsStatistic(affinityStat))
                    {
                        int deployCount = companyStats.GetValue<int>(affinityStat);
                        if (deployCount > getLowestDecay())
                        {
                            deployCount--;
                            companyStats.Set<int>(affinityStat, deployCount);
                            Main.modLog.LogMessage(
                                $"decaying stat {affinityStat}, due to no deployment, new value: {deployCount}");
                            return true;
                        }
                    }
                    else
                    {
                        Main.modLog.LogError($"Failed to decay stat {affinityStat}");
                    }
                }
            }

            return false;
        }

        public void incrementDeployCountWithMech(string statName)
        {
            Main.modLog.LogMessage($"Incrementing DeployCount stat {statName}");
            string decayStat = convertAffinityStatToDecay(statName);
            string simDecaystat = convertAffinityStatToSimDecay(statName);
            addtoPilotMap(statName);
            addtoPilotDecayMap(decayStat);
            if (companyStats.ContainsStatistic(statName))
            {
                int stat = companyStats.GetValue<int>(statName);
                stat++;
                companyStats.Set<int>(statName, stat);
            }
            else
            {
                // we dont have the stat yet so just set it to 1
                companyStats.AddStatistic<int>(statName, 1);
            }
            decayAffinties(decayStat);
            if (companyStats.ContainsStatistic(simDecaystat))
            {
                // pilot has deployed, reset their no deployment tracker
                companyStats.Set<int>(simDecaystat, 0);
            }


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

        private List<string> getHighestLevelName(string statName, string prefab)
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
            List<string> highest = new List<string>();
            highest.Add(ret);
            return highest;
        }

        private List<string> getAllLevels(string statName, string prefab)
        {
            List<string> ret = new List<string>();
            int deployCount = getDeploymentCountWithMech(statName);
            //Main.modLog.LogMessage($"Deployment Count: {deployCount}");

            foreach (AffinityLevel affinityLevel in Main.settings.globalAffinities)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    ret.Add(affinityLevel.levelName);
                }

            }
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (deployCount >= affinityLevel.missionsRequired)
                    {
                        ret.Add(affinityLevel.levelName);
                    }
                }
            }

            if (ret.Count == 0)
            {
                ret.Add(MA_NoAffinity);
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
                    List<string> levels;
                    if (Main.settings.showAllPilotAffinities)
                    {
                        levels = getAllLevels($"{MA_Deployment_Stat}{pilotId}={chassisId}", chassisId);
                    }
                    else
                    {
                        levels = getHighestLevelName($"{MA_Deployment_Stat}{pilotId}={chassisId}", chassisId);
                    }

                    foreach (string level in levels)
                    {
                        string chassisName = chassisId;
                        if (!string.IsNullOrEmpty(level))
                        {
                            if (!affinites.ContainsKey(level))
                            {
                                affinites[level] = new List<string>();
                            }

                            if (prefabOverrides.ContainsKey(chassisId))
                            {
                                chassisName = prefabOverrides[chassisId];
                            }
                            else
                            {
                                if (chassisPrefabLut.ContainsKey(chassisId))
                                {
                                    chassisName = chassisPrefabLut[chassisId];
                                }
                            }

                            affinites[level].Add(chassisName);
                        }
                    }
                }
            }
            string ret = "\n";
            foreach(KeyValuePair<string, List<string>> level in affinites)
            {
                string descript = levelDescriptors[level.Key].toString(false);
                string mechs = string.Join("\n", level.Value);
                descript += mechs;
                ret += descript + "\n\n";
            }

            return ret;
        }

        public string getPilotToolTip(Pilot pilot)
        {
            return "";
        }

        private void getDeploymentBonus(int deployCount, string chassisPrefab, string statName, List<string> possibleQuirks, out Dictionary<EAffinityType, int> bonuses, out List<EffectData> effects)
        {
            bonuses = new Dictionary<EAffinityType, int>();
            effects = new List<EffectData>();
            Main.modLog.LogMessage($"Processing Pilot/Mech Combo {statName}");

            foreach (AffinityLevel affinityLevel in Main.settings.globalAffinities)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Global Level {affinityLevel.levelName}");
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
                    }
                }
            }
            if (!String.IsNullOrEmpty(chassisPrefab))
            {
                if (chassisAffinities.ContainsKey(chassisPrefab))
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
            }
            foreach (string quirk in possibleQuirks)
            {
                List<AffinityLevel> affinityLevels = quirkAffinities[quirk];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (deployCount >= affinityLevel.missionsRequired)
                    {
                        Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Quirk Specific Level {affinityLevel.levelName}");
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
        }


        private void getDeploymentBonus(AbstractActor actor, out Dictionary<EAffinityType, int> bonuses, out List<EffectData> effects)
        {
            int deployCount = getDeploymentCountWithMech(actor);
            string chassisPrefab = getPrefabId(actor);
            string statName = getAffinityStatName(actor);
            List<string> possibleQuirks = getPossibleQuirkAffinites(actor);
            getDeploymentBonus(deployCount, chassisPrefab, statName, possibleQuirks, out bonuses, out effects);
        }

        public int getPilotDeployBonusByTag(AbstractActor actor)
        {
            int deployCount = 0;
            Pilot pilot = actor.GetPilot();
            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToArray().ToList();
                if (!String.IsNullOrEmpty(Main.settings.debugForceTag))
                {
                    tags.Add(Main.settings.debugForceTag);
                }
                foreach(string tag in tags)
                {
                    if (tag.StartsWith(MA_PilotDeployCountTag))
                    {
                        string count = tag.Replace(MA_PilotDeployCountTag, "");
                        if (!int.TryParse(count, out deployCount))
                        {
                            deployCount = 0;
                        }
                        break;
                    }
                }
            }
            return deployCount;
        }

        private void getAIBonuses(AbstractActor actor, out Dictionary<EAffinityType, int> bonuses, out List<EffectData> effects)
        {
            int deployCount = getPilotDeployBonusByTag(actor);
            string chassisPrefab = getPrefabId(actor);
            string statName = getAffinityStatName(actor);
            List<string> possibleQuirks = getPossibleQuirkAffinites(actor);
            getDeploymentBonus(deployCount, chassisPrefab, statName, possibleQuirks, out bonuses, out effects);
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
            List<EffectData> effects;
            Dictionary<EAffinityType, int> bonuses;
            if (actor.team == null || !actor.team.IsLocalPlayer)
            {
                // actor isnt part of our team, check if their pilot defines a bonus
                Main.modLog.LogMessage($"Processing AI actor: {getAffinityStatName(actor)}");
                getAIBonuses(actor, out bonuses, out effects);
            }
            else
            {
                getDeploymentBonus(actor, out bonuses, out effects);
            }
            if (Main.settings.debug)
            {
                foreach (KeyValuePair<EAffinityType, int> bonus in bonuses)
                {
                    Main.modLog.LogMessage($"Applying Bonus: {bonus.Key.ToString()} with strength: {bonus.Value}");
                }
            }
            applyStatBonuses(actor, bonuses);
            applyStatusEffects(actor, effects);
        }

        public bool onSimDayElapsed(Pilot pilot)
        {
            int modulator = getSimDecayDays();
            if (modulator == -1)
            {
                return false;
            }
            string pilotId = pilot.pilotDef.Description.Id;
            string statName = $"{MA_DaysElapsedMod_Stat}{pilotId}";
            if (!companyStats.ContainsStatistic(statName))
            {
                companyStats.AddStatistic<int>(statName, 1);
                Main.modLog.LogMessage($"Adding Sim Decay stat {statName}");
                return false;
            }
            int daysSinceLastDecay = companyStats.GetValue<int>(statName);
            
            daysSinceLastDecay++;
            daysSinceLastDecay %= modulator;
            companyStats.Set<int>(statName, daysSinceLastDecay);
            if (daysSinceLastDecay == 0)
            {
                Main.modLog.LogMessage($"Sim Decay detected for {statName}");
                return simDayDecay(pilotId);
            }
            return false;
        }

        
        public string getMechChassisAffinityDescription(MechDef mech)
        {
            return getMechChassisAffinityDescription(mech.Chassis);
        }


        public string getMechChassisAffinityDescription(ChassisDef chassis)
        {
            string prefab = getPrefabId(chassis);
            string ret = "\n";
            List<string> levels = new List<string>();
            Main.modLog.DebugMessage($"Found prefab: {prefab}");
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if(!levels.Contains(affinityLevel.levelName))
                    {
                        levels.Add(affinityLevel.levelName);
                        Main.modLog.DebugMessage($"adding chassis affinity descriptor for {affinityLevel.levelName}");
                    }
                }
            }
            if (Main.settings.showQuirks)
            {
                List<string> quirks = getPossibleQuirkAffinites(chassis);
                foreach(string quirk in quirks)
                {
                    Main.modLog.DebugMessage($"checking for a quirk affinity descriptor for {quirk}");
                    List<AffinityLevel> affinityLevels = quirkAffinities[quirk];
                    foreach (AffinityLevel affinityLevel in affinityLevels)
                    {
                        if (!levels.Contains(affinityLevel.levelName))
                        {
                            levels.Add(affinityLevel.levelName);
                            Main.modLog.DebugMessage($"adding quirk affinity descriptor for {affinityLevel.levelName}");
                        }
                    }
                }
            }
            if(levels.Count() > 0)
            {
                ret = "\n<b> Unlockable Affinities: </b>\n\n";
            }
            List<DescriptionHolder> descriptors = new List<DescriptionHolder>();
            foreach (string level in levels)
            {
                descriptors.Add(levelDescriptors[level]);
            }
            descriptors.Sort();
            foreach (DescriptionHolder descriptor in descriptors)
            {
                ret += descriptor.toString(true);
            }

            return ret;
        }

    }

}
