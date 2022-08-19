using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using MechAffinity.Data;
using Harmony;
using System.Reflection;
using BattleTech;
using Newtonsoft.Json.Linq;

namespace MechAffinity
{
    public class Main
    {
        private const string SettingsFilePath = "settings.json";
        private const string LegacyFilePath = "settings.legacy.json";
        private const string PilotSelectSettingsFilePath = "pilotselectsettings.json";
        
        internal static Logger modLog;
        internal static LegacySettings legacySettings;
        internal static Settings settings;
        internal static PilotSelectSettings pilotSelectSettings = new PilotSelectSettings();
        internal static string modDir;
        internal static readonly string AffinitiesDefinitionTypeName = "AffinitiesDef";
        public static void FinishedLoading(List<string> loadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources)
        {
            List<AffinityDef> affinityDefs = new List<AffinityDef>();
            if (customResources != null)
            {
                foreach (var customResource in customResources)
                {
                    modLog.LogMessage("customResource:" + customResource.Key);
                    if (customResource.Key == AffinitiesDefinitionTypeName)
                    {
                        foreach (var affinityDefPath in customResource.Value)
                        {
                            try
                            {
                                modLog.LogMessage("Path:" + affinityDefPath.Value.FilePath);
                                AffinityDef affinityDef = JsonConvert.DeserializeObject<AffinityDef>(File.ReadAllText(affinityDefPath.Value.FilePath));
                                affinityDefs.Add(affinityDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.LogException(ex);
                            }
                        }
                    }
                }
            }
            legacySettings.InitLookups();
            try {
                if (settings.enablePilotAffinity) PilotAffinityManager.Instance.initialize(settings.affinitySettings, affinityDefs);
                PilotQuirkManager.Instance.initialize();
            }
            catch (Exception ex)
            {
                modLog.LogException(ex);
            }
        }

        private static void convertLegacyToSettings(bool throwError)
        {
            legacySettings = JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText($"{modDir}/{SettingsFilePath}"));
            Settings newSettings = Settings.FromLegacy(legacySettings, modDir);
            File.WriteAllText($"{modDir}/{SettingsFilePath}",JsonConvert.SerializeObject(newSettings, Formatting.Indented));
            File.WriteAllText($"{modDir}/{LegacyFilePath}",JsonConvert.SerializeObject(legacySettings, Formatting.Indented));
            if (throwError) throw new NotSupportedException("Legacy Settings File Converted, make sure mod.json has been updated!");
        }

        public static void Init(string modDirectory, string settingsJSON)
        {

            modDir = modDirectory;
            modLog = new Logger(modDir, "MechAffinity", true);

            var settingsData = JObject.Parse(File.ReadAllText($"{modDir}/{SettingsFilePath}"));
            JToken version;
            if (!settingsData.TryGetValue("version", out version)) convertLegacyToSettings(true);
            
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{modDir}/{SettingsFilePath}")); //if we had failed to read settings it is useless to proceed. Better notify ModTek instead.

            if (settings.legacyData.debug_convertFromLegacyData)
            {
                convertLegacyToSettings(false);
            }
            
            //ToDo: Convert to new Settings system            
            // legacySettings.InitLookups();

            if (settings.enablePilotSelect)
            {
                try
                {
                    using (StreamReader reader = new StreamReader($"{modDir}/{PilotSelectSettingsFilePath}"))
                    {
                        string jdata = reader.ReadToEnd();
                        pilotSelectSettings = JsonConvert.DeserializeObject<PilotSelectSettings>(jdata);
                    }
                }
                catch (Exception ex)
                {
                    modLog.LogException(ex);
                }
            }

            var harmony = HarmonyInstance.Create("ca.jwolf.MechAffinity");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }
    }
}
