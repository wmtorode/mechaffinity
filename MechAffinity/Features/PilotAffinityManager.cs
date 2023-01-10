using System;
using System.Collections.Generic;
using System.Linq;
using MechAffinity.Data;
using BattleTech;
using BattleTech.Data;
using Newtonsoft.Json.Linq;

#if USE_CS_CC
using CustomComponents;
using CustomSalvage;
#endif

#if USE_CS_CC && USE_LT
    using LewdableTanks;
#endif

// ReSharper disable once CheckNamespace
namespace MechAffinity
{
    public class PilotAffinityManager: BaseEffectManager
    {
        private const string MaDeploymentStat = "MaDeployStat=";
        private const string MaDecayStat = "MaDecayStat=";
        private const string MaLutStat = "MaLutStat";
        private const string MaDaysElapsedModStat = "MaDaysSinceLastDecay=";
        private const string MaSimDaysDecayModulatorStat = "MaSimDaysDecayModulator";
        private const string MaLowestDecayStat = "MaLowestDecay";
        private const string MaPilotDeployCountTag = "affinityLevel_";
        private const string MaNoAffinity = "No Affinity";
        private const string MaConsumableTag = "MaConsumableAffinity_";
        private const string MaPermaTag = "MaPermAffinity_";
        private static PilotAffinityManager _instance;
        private StatCollection companyStats;
        private Dictionary<string, List<AffinityLevel>> chassisAffinities;
        private Dictionary<string, List<AffinityLevel>> quirkAffinities;
        private Dictionary<string, List<AffinityLevel>> taggedAffinities;
        private Dictionary<string, AffinityLevel> globalAffinities;
        
        private Dictionary<string, List<string>> pilotStatMap;
        private Dictionary<string, string> chassisPrefabLut;
        private Dictionary<string, string> prefabOverrides;
        private Dictionary<string, DescriptionHolder> levelDescriptors;
        private Dictionary<string, List<string>> pilotNoDeployStatMap;
        
        private Dictionary<string, EIdType> overloads;
        private Dictionary<string, string> remappedIds;
        private List<string> tagsWithAffinities;
        private DataManager dataManager = null;
        private PilotAffinitySettings settings;

        public static PilotAffinityManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotAffinityManager();
                if (!_instance.hasInitialized) _instance.initialize(Main.settings.affinitySettings, Main.affinityDefs);
                return _instance;
            }
        }

        public void initialize(PilotAffinitySettings pilotAffinitySettings, List<AffinityDef> affinityDefs)
        {
            if(hasInitialized) return;
            settings = pilotAffinitySettings;
            chassisAffinities = new Dictionary<string, List<AffinityLevel>>();
            quirkAffinities = new Dictionary<string, List<AffinityLevel>>();
            taggedAffinities = new Dictionary<string, List<AffinityLevel>>();
            globalAffinities = new Dictionary<string, AffinityLevel>();
            
            prefabOverrides = new Dictionary<string, string>();
            chassisPrefabLut = new Dictionary<string, string>();
            levelDescriptors = new Dictionary<string, DescriptionHolder>();
            tagsWithAffinities = new List<string>();
            pilotStatMap = new Dictionary<string, List<string>>();
            pilotNoDeployStatMap = new Dictionary<string, List<string>>();
            overloads = new Dictionary<string, EIdType>();
            remappedIds = new Dictionary<string, string>();

            foreach (var affinityDef in affinityDefs)
            {
                switch (affinityDef.affinityType)
                {
                    case EAffinityDefType.Global:
                        var affinity = affinityDef.getGlobalAffinity();
                        globalAffinities.Add(affinityDef.id, affinityDef.getGlobalAffinity());
                        break;
                    case EAffinityDefType.Chassis:
                        var chassisSpecific = affinityDef.getChassisAffinity();
                        Main.modLog.LogMessage(" id:"+ chassisSpecific.id);
                        foreach (string chassisName in chassisSpecific.chassisNames)
                        {
                            chassisAffinities.Add(chassisName, chassisSpecific.affinityLevels);
                            if (chassisSpecific.idType != EIdType.AssemblyVariant)
                            {
                                overloads.Add(chassisName, chassisSpecific.idType);
                            }
                        }

                        foreach (var altMap in chassisSpecific.altMaps)
                        {
                            foreach (string chassisName in altMap.chassisIds)
                            {
                                chassisAffinities.Add(chassisName, chassisSpecific.affinityLevels);
                                if (altMap.idType != EIdType.AssemblyVariant)
                                {
                                    overloads.Add(chassisName, altMap.idType);
                                }
                            }
                        }
                        foreach (AffinityLevel affinityLevel in chassisSpecific.affinityLevels)
                        {
                            Main.modLog.LogMessage("  level:" + affinityLevel.id);
                            levelDescriptors[affinityLevel.id] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
                        }
                        break;
                    case EAffinityDefType.Quirk:
                        var quirkAffinity = affinityDef.getQuirkAffinity();
                        Main.modLog.LogMessage(" id:"+ quirkAffinity.id);
                        foreach (string quirkName in quirkAffinity.quirkNames)
                        {
                            quirkAffinities.Add(quirkName, quirkAffinity.affinityLevels);
                        }
                        foreach (AffinityLevel affinityLevel in quirkAffinity.affinityLevels)
                        {
                            Main.modLog.LogMessage("  level:" + affinityLevel.id);
                            levelDescriptors[affinityLevel.id] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
                        }
                        break;
                    case EAffinityDefType.Tag:
                        var tagged = affinityDef.getTaggedAffinity();
                        tagsWithAffinities.Add(tagged.tag);
                        Main.modLog.LogMessage(" id:"+ tagged.id);
                        foreach (string chassisName in tagged.chassisNames)
                        {
                            taggedAffinities.Add($"{tagged.tag}={chassisName}", tagged.affinityLevels);
                            if (tagged.idType != EIdType.AssemblyVariant)
                            {
                                if (!overloads.ContainsKey(chassisName))
                                {
                                    overloads.Add(chassisName, tagged.idType);
                                }
                            }
                        }
                        foreach (var altMap in tagged.altMaps)
                        {
                            foreach (string chassisName in altMap.chassisIds)
                            {
                                taggedAffinities.Add($"{tagged.tag}={chassisName}", tagged.affinityLevels);
                                if (altMap.idType != EIdType.AssemblyVariant)
                                {
                                    if (!overloads.ContainsKey(chassisName))
                                    {
                                        overloads.Add(chassisName, altMap.idType);
                                    }
                                }
                            }
                        }
                        foreach (AffinityLevel affinityLevel in tagged.affinityLevels)
                        {
                            levelDescriptors[affinityLevel.id] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
                            Main.modLog.LogMessage("  level:" + affinityLevel.id);
                        }
                        break;
                }
            }
            
            Main.modLog.LogMessage("chassisAffinities:"+ chassisAffinities.Count);
            Main.modLog.LogMessage("taggedAffinities:" + taggedAffinities.Count);
            Main.modLog.LogMessage("quirkAffinities:" + quirkAffinities.Count);
            Main.modLog.LogMessage("globalAffinities:" + globalAffinities.Count);

            foreach (PrefabOverride overRide in settings.prefabOverrides)
            {
                prefabOverrides[overRide.prefabId] = overRide.overrideName;
            }
            foreach (AffinityLevel affinityLevel in globalAffinities.Values)
            {
                levelDescriptors[affinityLevel.id] = new DescriptionHolder(affinityLevel.levelName, affinityLevel.decription, affinityLevel.missionsRequired);
            }
            levelDescriptors[MaNoAffinity] = new DescriptionHolder(MaNoAffinity, "", 0);
            foreach (AffinityGroup affinityGroup in settings.affinityGroups)
            {
                foreach (string id in affinityGroup.assemblyGroup)
                {
                    remappedIds[id] = affinityGroup.affinityId;
                }
            }
            hasInitialized = true;
        }

        public void addToChassisPrefabLut(MechDef mech)
        {
            
            string variantId = getPrefabId(mech, EIdType.AssemblyVariant);
            string prefabId = getPrefabId(mech, EIdType.PrefabId);
            string chassisId = getPrefabId(mech, EIdType.ChassisId);
            chassisPrefabLut[variantId] = mech.Chassis.Description.Name;
            chassisPrefabLut[chassisId] = mech.Chassis.Description.Name;
            if (variantId != prefabId)
            {
                chassisPrefabLut[prefabId] = mech.Chassis.Description.Name;
            }
            //Main.modLog.LogMessage($"adding to lut {prefabId} => {mech.Chassis.Description.Name}");
        }

        public void setDataManager(DataManager dManager)
        {
            if (dManager != null)
            {
                dataManager = dManager;
            }
            else
            {
                dataManager = UnityGameInstance.BattleTechGame.DataManager;
            }

        }

        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
            pilotStatMap = new Dictionary<string, List<string>>();
            chassisPrefabLut = new Dictionary<string, string>();
            pilotNoDeployStatMap = new Dictionary<string, List<string>>();
            UidManager.reset();

            //find all mechs a given pilot has experience with and cache for later
            foreach (KeyValuePair<string, Statistic> keypair in companyStats)
            {
                if (keypair.Key.StartsWith(MaDeploymentStat))
                {
                    addtoPilotMap(keypair.Key);
                    addtoPilotDecayMap(convertAffinityStatToDecay(keypair.Key));
                }
                else
                {
                    if(keypair.Key.StartsWith(MaDecayStat))
                    {
                        addtoPilotDecayMap(keypair.Key);
                    }
                }
            }
            if (companyStats.ContainsStatistic(MaLutStat))
            {
                companyStats.RemoveStatistic(MaLutStat);
            }
            if (!companyStats.ContainsStatistic(MaSimDaysDecayModulatorStat) && settings.trackSimDecayByStat)
            {
                companyStats.AddStatistic<int>(MaSimDaysDecayModulatorStat, settings.defaultDaysBeforeSimDecay);
            }
            if (!companyStats.ContainsStatistic(MaLowestDecayStat) && settings.trackLowestDecayByStat)
            {
                companyStats.AddStatistic<int>(MaLowestDecayStat, settings.lowestPossibleDecay);
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
            return statName.Replace(MaDecayStat, MaDeploymentStat);
        }

        private string convertAffinityStatToDecay(string statName)
        {
            return statName.Replace(MaDeploymentStat, MaDecayStat);
        }

        private string convertAffinityStatToSimDecay(string statName)
        {
            string pilotId = statName.Split('=')[1];
            return $"{MaDaysElapsedModStat}{pilotId}";
        }

        private string getPrefabIdInternal(ChassisDef chassis, EIdType idType)
        {
            if (idType == EIdType.ChassisId)
            {
                return chassis.Description.Id;
            }
            #if USE_CS_CC
                if (idType == EIdType.AssemblyVariant)
                {
                    if (chassis.Is<AssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                        return a.PrefabID + "_" + chassis.Tonnage.ToString();
                }
            #endif

            return $"{chassis.PrefabIdentifier}_{chassis.Tonnage}";
        }

        private string getPrefabId(ChassisDef chassis, EIdType idType)
        {
            #if USE_CS_CC && USE_LT
                if (chassis.IsVehicle())
                {
                    if (dataManager == null)
                    {
                        setDataManager(null);
                    }
                    VehicleChassisDef vehicle = dataManager.VehicleChassisDefs.Get(chassis.Description.Id);
                    return getPrefabId(vehicle, idType);
                }
            #endif
            string prefab = getPrefabIdInternal(chassis, idType);
            if (remappedIds.ContainsKey(prefab))
            {
                return remappedIds[prefab];
            }

            return prefab;
        }

        private string getPrefabIdInternal(VehicleChassisDef chassis, EIdType idType)
        {
            if (idType == EIdType.ChassisId)
            {
                return chassis.Description.Id;
            }
            #if USE_CS_CC && USE_LT
                if (idType == EIdType.AssemblyVariant)
                {
                    if (chassis.Is<VAssemblyVariant>(out var a) && !string.IsNullOrEmpty(a.PrefabID))
                        return a.PrefabID + "_" + chassis.Tonnage.ToString();
                }
            #endif
            return $"{chassis.PrefabIdentifier}_{chassis.Tonnage}";
        }
        
        private string getPrefabId(VehicleChassisDef chassis, EIdType idType)
        {
            string prefab = getPrefabIdInternal(chassis, idType);
            if (remappedIds.ContainsKey(prefab))
            {
                return remappedIds[prefab];
            }

            return prefab;
        }

        private string getPrefabId(VehicleDef vehicle, EIdType idType)
        {
            return getPrefabId(vehicle.Chassis, idType);
        }

        private string getPrefabId(MechDef mech, EIdType idType)
        {
            #if USE_CS_CC && USE_LT
                if (mech.IsVehicle())
                {
                    if (dataManager == null)
                    {
                        setDataManager(null);
                    }
                    VehicleDef vehicle = dataManager.VehicleDefs.Get(mech.Description.Id);
                    return getPrefabId(vehicle, idType);
                }
            #endif
            return getPrefabId(mech.Chassis, idType);
        }

        private string getPrefabId(AbstractActor actor, EIdType idType)
        {
            Mech mech = actor as Mech;
            if (mech != null)
            {
                return getPrefabId(mech.MechDef, idType);
            }
            Vehicle vehicle = actor as Vehicle;
            if (vehicle != null)
            {
                return getPrefabId(vehicle.VehicleDef, idType);
            }
            return null;
        }

        private int getLowestDecay()
        {
            if (settings.trackLowestDecayByStat)
            {
                if (companyStats != null)
                {
                    return companyStats.GetValue<int>(MaLowestDecayStat);
                }
            }
            return settings.lowestPossibleDecay;
        }

        private int getSimDecayDays()
        {
            if (settings.trackSimDecayByStat)
            {
                if (companyStats != null)
                {
                    return companyStats.GetValue<int>(MaSimDaysDecayModulatorStat);
                }
            }
            return settings.defaultDaysBeforeSimDecay;
        }

        private List<string> getDefaultEquipment(MechDef mechDef)
        {
            List<string> defaults = new List<string>();
            #if USE_CS_CC
                if (settings.treatDefaultsAsFixed)
                {
                    foreach (var component in mechDef.Inventory)
                    {
                        if (component.Flags<CCFlags>().Default)
                        {
                            defaults.Add(component.ComponentDefID);
                        }
                    }
                }
            #endif
            return defaults;
        }

        private List<string> getPossibleQuirkAffinites(ChassisDef chassis, List<string> defaultComponents)
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

            if (defaultComponents != null)
            {
                foreach (var defaultComponent in defaultComponents)
                {
                    if (quirkAffinities.ContainsKey(defaultComponent))
                    {
                        if (!quirks.Contains(defaultComponent))
                        {
                            quirks.Add(defaultComponent);
                        }
                    }
                }
            }
            return quirks;
        }

        private List<string> getPossibleQuirkAffinites(MechDef mech)
        {
            
            return getPossibleQuirkAffinites(mech.Chassis, getDefaultEquipment(mech));
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

        private List<string> getPossibleTaggedAffinities(Pilot pilot)
        {
            List<string> possibleTags = new List<string>();
            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToList();
                foreach (string tag in tags)
                {
                    if (tagsWithAffinities.Contains(tag))
                    {
                        possibleTags.Add(tag);
                    }
                }
            }
            return possibleTags;
        }

        private List<string> getPossibleTaggedAffinities(AbstractActor actor)
        {
            return getPossibleTaggedAffinities(actor.GetPilot());
        }

        private string getTaggedAffinityLookup(string tag, string prefabId)
        {
            return $"{tag}={prefabId}";
        }

        private void addToMapIfNeeded(Pilot pilot)
        {
            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToList();
                foreach (string tag in tags)
                {
                    //Main.modLog.LogMessage($"Processing tag: {tag}");
                    if (tag.StartsWith(MaPermaTag))
                    {
                        string tagPrefab = tag.Split('=')[1];
                        string statName = getAffinityStatName(pilot, tagPrefab);
                        addtoPilotMap(statName);

                    }
                }
            }

        }

        private string getAffinityStatName(AbstractActor actor)
        {
            Pilot pilot = actor.GetPilot();
            if (pilot == null)
            {
                Main.modLog.LogMessage("Null Pilot found!");
                return $"{MaDeploymentStat}";
            }
            string prefab = getPrefabId(actor, EIdType.ChassisId);
            if (String.IsNullOrEmpty(prefab))
            {
                Main.modLog.LogMessage("Null Prefab!");
                return $"{MaDeploymentStat}{pilot.pilotDef.Description.Id}";
            }
            if (!overloads.ContainsKey(prefab))
            {
                prefab = getPrefabId(actor, EIdType.PrefabId);
                if (!overloads.ContainsKey(prefab))
                {
                    prefab = getPrefabId(actor, EIdType.AssemblyVariant);
                }
            }
            return $"{MaDeploymentStat}{pilot.pilotDef.Description.Id}={prefab}";
        }

        private string getAffinityStatName(UnitResult result)
        {    
            string prefabId = getPrefabId(result.mech, EIdType.ChassisId);
            if (!overloads.ContainsKey(prefabId))
            {
                prefabId = getPrefabId(result.mech, EIdType.PrefabId);
                if (!overloads.ContainsKey(prefabId))
                {
                    prefabId = getPrefabId(result.mech, EIdType.AssemblyVariant);
                }
            }
            chassisPrefabLut[prefabId] = result.mech.Chassis.Description.Name;
            string statName = $"{MaDeploymentStat}{result.pilot.pilotDef.Description.Id}={prefabId}";
            return statName;
        }

        private string getAffinityStatName(Pilot pilot, string prefabId)
        {
            return $"{MaDeploymentStat}{pilot.pilotDef.Description.Id}={prefabId}";
        }

        public int getStatDeploymentCountWithMech(string statName)
        {
            if (companyStats != null)
            {
                if (companyStats.ContainsStatistic(statName))
                {
                    return companyStats.GetValue<int>(statName);
                }
            }
            return 0;
        }

        public int getTaggedDeploymentCountWithMech(Pilot pilot, string prefabId)
        {
            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToList();
                foreach (string tag in tags)
                {
                    //Main.modLog.LogMessage($"Processing tag: {tag}");
                    if (tag.StartsWith(MaPermaTag))
                    {
                        int deployCount;
                        string tagPrefab = tag.Split('=')[1];
                        if (prefabId == tagPrefab)
                        {
                            string count = tag.Split('=')[0].Replace(MaPermaTag, "");
                            if (!int.TryParse(count, out deployCount))
                            {
                                deployCount = 0;
                            }

                            return deployCount;
                        }
                    }
                }
            }

            return 0;
        }

        public int getDeploymentCountWithMech(AbstractActor actor)
        {
            string statName = getAffinityStatName(actor);
            string prefab = getPrefabId(actor, EIdType.ChassisId);
            if (!overloads.ContainsKey(prefab))
            {
                prefab = getPrefabId(actor, EIdType.PrefabId);
                if (!overloads.ContainsKey(prefab))
                {
                    prefab = getPrefabId(actor, EIdType.AssemblyVariant);
                }
            }
            return getStatDeploymentCountWithMech(statName) + getTaggedDeploymentCountWithMech(actor.GetPilot(), prefab);
            
        }

        public int getDeploymentCountWithMech(Pilot pilot, MechDef mechDef)
        {
            string prefab = getPrefabId(mechDef, EIdType.ChassisId);
            if (!overloads.ContainsKey(prefab))
            {
                prefab = getPrefabId(mechDef, EIdType.PrefabId);
                if (!overloads.ContainsKey(prefab))
                {
                    prefab = getPrefabId(mechDef, EIdType.AssemblyVariant);
                }
            }
            string statName = getAffinityStatName(pilot, prefab);
            return getStatDeploymentCountWithMech(statName) + getTaggedDeploymentCountWithMech(pilot, prefab);
        }

        public int getDeploymentCountWithMech(Pilot pilot, string prefabId)
        {
            string statName = getAffinityStatName(pilot, prefabId);
            return getStatDeploymentCountWithMech(statName) + getTaggedDeploymentCountWithMech(pilot, prefabId);

        }

        private bool shouldDecay(int decayCount)
        {
            if (settings.missionsBeforeDecay != -1)
            {
                if (settings.decayByModulo)
                {
                    if (decayCount != 0 && ((decayCount % settings.missionsBeforeDecay) == 0))
                    {
                        return true;
                    }
                }
                else
                {
                    if(decayCount >= settings.missionsBeforeDecay)
                    {
                        return true;
                    }
                }
            }
            return false;
            
        }

        private void decayAffinties(string decayStat)
        {
            if (companyStats == null)
            {
                return;
            }
            if (settings.missionsBeforeDecay != -1 || settings.removeAffinityAfter != -1)
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
                            if (settings.removeAffinityAfter != -1 && decayed >= settings.removeAffinityAfter)
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

        public void incrementDeployCountWithMech(string statName ,int incrementBy=1, bool decay=true)
        {
            if (companyStats == null)
            {
                return;
            }
            Main.modLog.LogMessage($"Incrementing DeployCount stat {statName}");
            string decayStat = convertAffinityStatToDecay(statName);
            string simDecaystat = convertAffinityStatToSimDecay(statName);
            addtoPilotMap(statName);
            addtoPilotDecayMap(decayStat);
            if (companyStats.ContainsStatistic(statName))
            {
                int stat = companyStats.GetValue<int>(statName);
                stat+= incrementBy;
                stat = Math.Min(stat, settings.maxAffinityPoints);
                companyStats.Set<int>(statName, stat);
            }
            else
            {
                // we dont have the stat yet so just set it to 1
                companyStats.AddStatistic<int>(statName, incrementBy);
            }
            if (decay) decayAffinties(decayStat);
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

        private List<string> getHighestLevelName(string prefab, Pilot pilot)
        {
            string ret = "";
            int maxSoFar = 0;
            int deployCount = getDeploymentCountWithMech(pilot, prefab);
            //Main.modLog.LogMessage($"Deployment Count: {deployCount}");

            foreach (AffinityLevel affinityLevel in globalAffinities.Values)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    if (affinityLevel.missionsRequired >= maxSoFar)
                    {
                        maxSoFar = affinityLevel.missionsRequired;
                        ret = affinityLevel.id;
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
                            ret = affinityLevel.id;
                        }
                    }
                }
            }

            List<string> highest = new List<string>();
            highest.Add(ret);
            return highest;
        }

        private List<string> getAllLevels(Pilot pilot, string prefab, bool withCounts)
        {
            List<string> ret = new List<string>();
            int deployCount = getDeploymentCountWithMech(pilot, prefab);
            //Main.modLog.LogMessage($"Deployment Count: {deployCount}");
            List<string> tags = getPossibleTaggedAffinities(pilot);

            foreach (AffinityLevel affinityLevel in globalAffinities.Values)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    string toAdd = affinityLevel.id;
                    if (withCounts) toAdd += $" ({deployCount}/{affinityLevel.missionsRequired})";
                    ret.Add(toAdd);
                }

            }
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (deployCount >= affinityLevel.missionsRequired)
                    {
                        string toAdd = affinityLevel.id;
                        if (withCounts) toAdd += $" ({deployCount}/{affinityLevel.missionsRequired})";
                        ret.Add(toAdd);
                    }
                }
            }
            foreach (string tag in tags)
            {
                string lookup = getTaggedAffinityLookup(tag, prefab);
                if (taggedAffinities.ContainsKey(lookup))
                {
                    List<AffinityLevel> affinityLevels = taggedAffinities[lookup];
                    foreach (AffinityLevel affinityLevel in affinityLevels)
                    {
                        if (deployCount >= affinityLevel.missionsRequired)
                        {
                            string toAdd = affinityLevel.id;
                            if (withCounts) toAdd += $" ({deployCount}/{affinityLevel.missionsRequired})";
                            ret.Add(toAdd);
                        }
                    }
                }
            }

            if (ret.Count == 0)
            {
                ret.Add(MaNoAffinity);
            }
            return ret;
        }

        private List<string> getAllLevelsToolTip(Pilot pilot, string prefab)
        {
            List<string> ret = new List<string>();
            int deployCount = getDeploymentCountWithMech(pilot, prefab);
            //Main.modLog.LogMessage($"Deployment Count: {deployCount}");
            List<AffinityLevel> levels = new List<AffinityLevel>();

            foreach (AffinityLevel affinityLevel in globalAffinities.Values)
            {
                levels.Add(affinityLevel);
            }
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    levels.Add(affinityLevel);
                }
            }
            List<string> tags = getPossibleTaggedAffinities(pilot);
            foreach (string tag in tags)
            {
                string lookup = getTaggedAffinityLookup(tag, prefab);
                if (taggedAffinities.ContainsKey(lookup))
                {
                    List<AffinityLevel> affinityLevels = taggedAffinities[lookup];
                    foreach (AffinityLevel affinityLevel in affinityLevels)
                    {
                        levels.Add(affinityLevel);
                    }
                }
            }

            levels = levels.OrderBy(d => d.missionsRequired).ToList();
            foreach (AffinityLevel level in levels)
            {
                if (deployCount >= level.missionsRequired)
                {
                    ret.Add($"- <color=#1bab05>{level.levelName}</color> ({level.missionsRequired}/{level.missionsRequired})");
                }
                else
                {
                    ret.Add($"- <color=#de0202>{level.levelName}</color> ({deployCount}/{level.missionsRequired})");
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
                    List<string> levels;
                    if (settings.showAllPilotAffinities)
                    {
                        levels = getAllLevels(pilot, chassisId, false);
                    }
                    else
                    {
                        levels = getHighestLevelName(chassisId, pilot);
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
            if (pilot != null)
            {
                addToMapIfNeeded(pilot);
                string pilotId = pilot.pilotDef.Description.Id;
                Dictionary<string, List<string>> affinites = new Dictionary<string, List<string>>();
                if (pilotStatMap.ContainsKey(pilotId))
                {
                    Dictionary<string, int> chassisValues = new Dictionary<string, int>();
                    foreach (string chassisId in pilotStatMap[pilotId])
                    {
                        chassisValues[chassisId] = getDeploymentCountWithMech(pilot, chassisId);
                    }

                    int toShow = Math.Min(chassisValues.Count, settings.topAffinitiesInTooltipCount);
                    List<KeyValuePair<string, int>> sortedCounts = chassisValues.OrderByDescending(d => d.Value).ToList();
                    for (int i = 0; i < toShow; i++)
                    {
                        List<string> levels;

                        levels = getAllLevelsToolTip(pilot, sortedCounts[i].Key);
                        affinites[sortedCounts[i].Key] = levels;

                    }
                }
                string ret = "\n";
                foreach (KeyValuePair<string, List<string>> level in affinites)
                {
                    string chassisName = level.Key;
                    if (prefabOverrides.ContainsKey(level.Key))
                    {
                        chassisName = prefabOverrides[level.Key];
                    }
                    else
                    {
                        if (chassisPrefabLut.ContainsKey(level.Key))
                        {
                            chassisName = chassisPrefabLut[level.Key];
                        }
                    }
                    string unit = $"<b>{chassisName}</b>\n";
                    string levels = string.Join("\n", level.Value);
                    unit += levels;
                    ret += unit + "\n\n";
                }
                return ret;
            }
            return "";
        }

        private void getDeploymentBonus(int deployCount, string chassisPrefab, string statName, List<string> possibleQuirks, List<string> possibleTags, out Dictionary<EAffinityType, int> bonuses, out List<EffectData> effects)
        {
            bonuses = new Dictionary<EAffinityType, int>();
            effects = new List<EffectData>();
            Main.modLog.LogMessage($"Processing Pilot/Mech Combo {statName}");

            foreach (AffinityLevel affinityLevel in globalAffinities.Values)
            {
                if (deployCount >= affinityLevel.missionsRequired)
                {
                    Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Global Level {affinityLevel.id}");
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
                            Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Chassis Specific Level {affinityLevel.id}");
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
                foreach (string tag in possibleTags)
                {
                    string lookup = getTaggedAffinityLookup(tag, chassisPrefab);
                    if (taggedAffinities.ContainsKey(lookup))
                    {
                        List<AffinityLevel> affinityLevels = taggedAffinities[lookup];
                        foreach (AffinityLevel affinityLevel in affinityLevels)
                        {
                            if (deployCount >= affinityLevel.missionsRequired)
                            {
                                Main.modLog.LogMessage(
                                    $"Pilot/Mech Combo {statName} has achieved Tagged Level {affinityLevel.id}");
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
                                    Main.modLog.LogMessage(
                                        $"Found effect ID: {effect.Description.Id}, name: {effect.Description.Name}");
                                }
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
                        Main.modLog.LogMessage($"Pilot/Mech Combo {statName} has achieved Quirk Specific Level {affinityLevel.id}");
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
            Main.modLog.LogMessage($"Pilot Natural Affinity: {deployCount}");
            deployCount += getPilotDeployBonusByTag(actor);
            string chassisPrefab = getPrefabId(actor, EIdType.ChassisId);
            Main.modLog.LogMessage($"Pilot Total Affinity: {deployCount}");
            if (String.IsNullOrEmpty(chassisPrefab))
            {
                Main.modLog.LogMessage("Null Prefab on player unit!!");
                bonuses = new Dictionary<EAffinityType, int>();
                effects = new List<EffectData>();
                return;
            }
            if (!overloads.ContainsKey(chassisPrefab))
            {
                chassisPrefab = getPrefabId(actor, EIdType.PrefabId);
                if (!overloads.ContainsKey(chassisPrefab))
                {
                    chassisPrefab = getPrefabId(actor, EIdType.AssemblyVariant);
                }
            }
            string statName = getAffinityStatName(actor);
            List<string> possibleQuirks = getPossibleQuirkAffinites(actor);
            List<string> possibleTags = getPossibleTaggedAffinities(actor);
            getDeploymentBonus(deployCount, chassisPrefab, statName, possibleQuirks, possibleTags, out bonuses, out effects);
        }

        public int getPilotDeployBonusByTag(AbstractActor actor)
        {
            int deployCount = 0;
            Pilot pilot = actor.GetPilot();
            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToArray().ToList();
                if (!String.IsNullOrEmpty(settings.debugForceTag))
                {
                    tags.Add(settings.debugForceTag);
                }
                foreach(string tag in tags)
                {
                    if (tag.StartsWith(MaPilotDeployCountTag))
                    {
                        string count = tag.Replace(MaPilotDeployCountTag, "");
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
            string chassisPrefab = getPrefabId(actor, EIdType.ChassisId);
            if (String.IsNullOrEmpty(chassisPrefab))
            {
                //probably a turret
                bonuses = new Dictionary<EAffinityType, int>();
                effects = new List<EffectData>();
                return;
            }
            if (!overloads.ContainsKey(chassisPrefab))
            {
                chassisPrefab = getPrefabId(actor, EIdType.PrefabId);
                if (!overloads.ContainsKey(chassisPrefab))
                {
                    chassisPrefab = getPrefabId(actor, EIdType.AssemblyVariant);
                }
            }
            string statName = getAffinityStatName(actor);
            List<string> possibleQuirks = getPossibleQuirkAffinites(actor);
            List<string> possibleTags = getPossibleTaggedAffinities(actor);
            getDeploymentBonus(deployCount, chassisPrefab, statName, possibleQuirks, possibleTags, out bonuses, out effects);
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

        private void consumeAffinityTags(Pilot pilot)
        {
            List<string> tags = pilot.pilotDef.PilotTags.ToList();
            foreach (string tag in tags)
            {
                //Main.modLog.LogMessage($"Processing tag: {tag}");
                if (tag.StartsWith(MaConsumableTag))
                {
                    int deployCount;
                    string prefabId = tag.Split('=')[1];
                    string count = tag.Split('=')[0].Replace(MaConsumableTag, "");
                    if (!int.TryParse(count, out deployCount))
                    {
                        deployCount = 0;
                    }
                    string statName = getAffinityStatName(pilot, prefabId);
                    incrementDeployCountWithMech(statName, deployCount, false);
                    Main.modLog.LogMessage($"Consuming affinity tag for pilot: {pilot.pilotDef.Description.Id}, unit: {prefabId}, modifier: {deployCount}");
                    pilot.pilotDef.PilotTags.Remove(tag);
                }
            }
        }

        public bool onSimDayElapsed(Pilot pilot)
        {
            consumeAffinityTags(pilot);
            int modulator = getSimDecayDays();
            if (modulator == -1)
            {
                return false;
            }
            string pilotId = pilot.pilotDef.Description.Id;
            string statName = $"{MaDaysElapsedModStat}{pilotId}";
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
            return getMechChassisAffinityDescription(mech.Chassis, getDefaultEquipment(mech));
        }


        public string getMechChassisAffinityDescription(ChassisDef chassis, List<string> defaultEquipment = null)
        {
            string prefab = getPrefabId(chassis, EIdType.ChassisId);
            if (!overloads.ContainsKey(prefab))
            {
                prefab = getPrefabId(chassis, EIdType.PrefabId);
                if (!overloads.ContainsKey(prefab))
                {
                    prefab = getPrefabId(chassis, EIdType.AssemblyVariant);
                }
            }

            string ret = "\n";
            List<string> levels = new List<string>();
            Main.modLog.DebugMessage($"Found prefab: {prefab}");
            if (chassisAffinities.ContainsKey(prefab))
            {
                List<AffinityLevel> affinityLevels = chassisAffinities[prefab];
                foreach (AffinityLevel affinityLevel in affinityLevels)
                {
                    if (!levels.Contains(affinityLevel.id))
                    {
                        levels.Add(affinityLevel.id);
                        Main.modLog.DebugMessage($"adding chassis affinity descriptor for {affinityLevel.id}");
                    }
                }
            }

            if (settings.showQuirks)
            {
                List<string> quirks = getPossibleQuirkAffinites(chassis, defaultEquipment);
                foreach (string quirk in quirks)
                {
                    Main.modLog.DebugMessage($"checking for a quirk affinity descriptor for {quirk}");
                    List<AffinityLevel> affinityLevels = quirkAffinities[quirk];
                    foreach (AffinityLevel affinityLevel in affinityLevels)
                    {
                        if (!levels.Contains(affinityLevel.id))
                        {
                            levels.Add(affinityLevel.id);
                            Main.modLog.DebugMessage($"adding quirk affinity descriptor for {affinityLevel.id}");
                        }
                    }
                }
            }

            if (levels.Count() > 0)
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
