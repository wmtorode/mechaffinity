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
        public bool enablePilotSelect = false;
        
        // Legacy Settings Debug data
        public LegacyData legacyData = new LegacyData();
        
        
        //Helpers
        private static string createId(string pattern) { return pattern.Replace(" ","_").Replace(".","_").Replace("\\","_").Replace("/","_").Replace("!","").Replace("@", "_").Replace("\"", "").Replace("(", "").Replace(")", ""); }
        internal static Settings FromLegacy(LegacySettings legacySettings, string modDirectory)
        {
            Settings settings = new Settings();

            settings.debug = legacySettings.debug;
            settings.enablePilotSelect = legacySettings.enablePilotSelect;

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

            return settings;
        }
    }
}