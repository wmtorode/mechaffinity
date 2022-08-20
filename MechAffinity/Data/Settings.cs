using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    public class Settings
    {
        public int version = 2;
        //Logging Features
        public bool debug = false;
        
        // Feature Enables
        public bool enablePilotAffinity = true;
        public bool enablePilotSelect = false;
        public bool enablePilotQuirks = false;
        public bool enableMonthlyMoraleReset = false;
        public bool enableStablePiloting = false;
        
        
        // Feature Settings
        // Note: Monthly Morale has no settings, its handled by the Quirk Manager
        // but has no settings other than if its enabled or disabled
        public PilotAffinitySettings affinitySettings = new PilotAffinitySettings();
        public PilotQuirkSettings quirkSettings = new PilotQuirkSettings();
        public StablePilotingSettings stablePilotingSettings = new StablePilotingSettings();
        public PilotUiSettings pilotUiSettings = new PilotUiSettings();
        
        // Legacy Settings Debug data
        public LegacyData legacyData = new LegacyData();


        //Helpers
        private static string createId(string pattern) { return pattern.Replace(" ","_").Replace(".","_").Replace("\\","_").Replace("/","_").Replace("!","").Replace("@", "_").Replace("\"", "").Replace("(", "").Replace(")", ""); }
        internal static Settings FromLegacy(LegacySettings legacySettings, string modDirectory)
        {
            Settings settings = new Settings();

            settings.debug = legacySettings.debug;
            settings.enablePilotSelect = legacySettings.enablePilotSelect;
            settings.enablePilotQuirks = legacySettings.enablePilotQuirks;
            settings.enableStablePiloting = legacySettings.enableStablePiloting;
            settings.enableMonthlyMoraleReset = legacySettings.enableMonthlyMoraleReset;

            settings.affinitySettings.affinityGroups = legacySettings.affinityGroups;
            settings.affinitySettings.showQuirks = legacySettings.showQuirks;
            settings.affinitySettings.missionsBeforeDecay = legacySettings.missionsBeforeDecay;
            settings.affinitySettings.lowestPossibleDecay = legacySettings.lowestPossibleDecay;
            settings.affinitySettings.removeAffinityAfter = legacySettings.removeAffinityAfter;
            settings.affinitySettings.decayByModulo = legacySettings.decayByModulo;
            settings.affinitySettings.debugForceTag = legacySettings.debugForceTag;
            settings.affinitySettings.defaultDaysBeforeSimDecay = legacySettings.defaultDaysBeforeSimDecay;
            settings.affinitySettings.showDescriptionsOnChassis = legacySettings.showDescriptionsOnChassis;
            settings.affinitySettings.trackSimDecayByStat = legacySettings.trackSimDecayByStat;
            settings.affinitySettings.trackLowestDecayByStat = legacySettings.trackLowestDecayByStat;
            settings.affinitySettings.showAllPilotAffinities = legacySettings.showAllPilotAffinities;
            settings.affinitySettings.topAffinitiesInTooltipCount = legacySettings.topAffinitiesInTooltipCount;
            settings.affinitySettings.maxAffinityPoints = legacySettings.maxAffinityPoints;
            settings.affinitySettings.prefabOverrides = legacySettings.prefabOverrides;

            settings.quirkSettings.playerQuirkPools = legacySettings.playerQuirkPools;
            settings.quirkSettings.argoAdditive = legacySettings.pqArgoAdditive;
            settings.quirkSettings.argoMultiAutoAdjust = legacySettings.pqArgoMultiAutoAdjust;
            settings.quirkSettings.argoMin = legacySettings.pqArgoMin;
            settings.quirkSettings.quirkPools = legacySettings.quirkPools;
            settings.quirkSettings.tooltipTags = legacySettings.pqTooltipTags;
            settings.quirkSettings.addTags = legacySettings.addTags;

            settings.stablePilotingSettings = legacySettings.stablePilotingSettings;
            
            settings.pilotUiSettings.pilotIcons = legacySettings.iconColours;

            if (!Directory.Exists($"{modDirectory}/AffinityDefs"))
            {
                int counter = 0;
                System.IO.Directory.CreateDirectory($"{modDirectory}/AffinityDefs");
                
                foreach (var globalAffinity in legacySettings.globalAffinities)
                {
                    AffinityDef affinityDef = new AffinityDef()
                    {
                        id = createId("AffinityDef_global_" + $"{globalAffinity.levelName}"),
                        affinityType = EAffinityDefType.Global
                    };
                    if (File.Exists($"{modDirectory}/AffinityDefs/{affinityDef.id}.json"))
                        affinityDef.id += $"_{counter}";
                    counter++;
                    affinityDef.setAffinityData(globalAffinity);
                    File.WriteAllText($"{modDirectory}/AffinityDefs/{affinityDef.id}.json",
                        JsonConvert.SerializeObject(affinityDef, Formatting.Indented));

                }
                
                foreach (var chassisAffinity in legacySettings.chassisAffinities)
                {
                    AffinityDef affinityDef = new AffinityDef()
                    {
                        id = createId("AffinityDef_chassis_" + $"{chassisAffinity.affinityLevels.First().levelName}"),
                        affinityType = EAffinityDefType.Chassis
                    };
                    Main.modLog.LogMessage($"{affinityDef.id}");
                    if (File.Exists($"{modDirectory}/AffinityDefs/{affinityDef.id}.json"))
                        affinityDef.id += $"_{counter}";
                    counter++;
                    affinityDef.setAffinityData(chassisAffinity);
                    File.WriteAllText($"{modDirectory}/AffinityDefs/{affinityDef.id}.json",
                        JsonConvert.SerializeObject(affinityDef, Formatting.Indented));

                }

                foreach (var quirkAffinity in legacySettings.quirkAffinities)
                {
                    AffinityDef affinityDef = new AffinityDef()
                    {
                        id = createId("AffinityDef_quirk_" + $"{quirkAffinity.affinityLevels.First().levelName}"),
                        affinityType = EAffinityDefType.Quirk
                    };
                    if (File.Exists($"{modDirectory}/AffinityDefs/{affinityDef.id}.json"))
                        affinityDef.id += $"_{counter}";
                    counter++;
                    affinityDef.setAffinityData(quirkAffinity);
                    File.WriteAllText($"{modDirectory}/AffinityDefs/{affinityDef.id}.json",
                        JsonConvert.SerializeObject(affinityDef, Formatting.Indented));

                }

                foreach (var taggedAffinity in legacySettings.taggedAffinities)
                {
                    AffinityDef affinityDef = new AffinityDef()
                    {
                        id = createId("AffinityDef_tagged_" + $"{taggedAffinity.affinityLevels.First().levelName}"),
                        affinityType = EAffinityDefType.Tag
                    };
                    if (File.Exists($"{modDirectory}/AffinityDefs/{affinityDef.id}.json"))
                        affinityDef.id += $"_{counter}";
                    counter++;
                    affinityDef.setAffinityData(taggedAffinity);
                    File.WriteAllText($"{modDirectory}/AffinityDefs/{affinityDef.id}.json",
                        JsonConvert.SerializeObject(affinityDef, Formatting.Indented));

                }
            }

            if (!Directory.Exists($"{modDirectory}/QuirkDefs"))
            {
                int counter = 0;
                System.IO.Directory.CreateDirectory($"{modDirectory}/QuirkDefs");
                foreach (var pilotQuirk in legacySettings.pilotQuirks)
                {
                    if (string.IsNullOrEmpty(pilotQuirk.id))
                    {
                        pilotQuirk.id = $"pilotQuirkDef_{pilotQuirk.tag}";
                    }
                    if (File.Exists($"{modDirectory}/QuirkDefs/{pilotQuirk.id}.json"))
                        pilotQuirk.id += $"_{counter}";
                    counter++;
                    File.WriteAllText($"{modDirectory}/QuirkDefs/{pilotQuirk.id}.json",
                        JsonConvert.SerializeObject(pilotQuirk, Formatting.Indented));
                    
                }
            }

            return settings;
        }
    }
}