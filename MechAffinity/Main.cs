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

namespace MechAffinity
{
    public class Main
    {
        internal static Logger modLog;
        internal static LegacySettings legacySettings;
        internal static PilotSelectSettings pilotSelectSettings = new PilotSelectSettings();
        internal static string modDir;
        internal static readonly string AffinitiesDefinitionTypeName = "AffinitiesDef";
        public static void FinishedLoading(List<string> loadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources) {
            if (customResources != null)
            {
                foreach (var customResource in customResources)
                {
                    modLog.LogMessage("customResource:" + customResource.Key);
                    if (customResource.Key == AffinitiesDefinitionTypeName)
                    {
                        foreach (var custMechRep in customResource.Value)
                        {
                            try
                            {
                                modLog.LogMessage("Path:" + custMechRep.Value.FilePath);
                                LegacySettings addLegacySettings = JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText(custMechRep.Value.FilePath));
                                if (addLegacySettings.Check(custMechRep.Value.FilePath))
                                {
                                    File.WriteAllText(custMechRep.Value.FilePath,JsonConvert.SerializeObject(legacySettings, Formatting.Indented));
                                    addLegacySettings =JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText(custMechRep.Value.FilePath));
                                }
                                legacySettings.Merge(addLegacySettings);
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
                PilotAffinityManager.Instance.initialize();
                PilotQuirkManager.Instance.initialize();
            }
            catch (Exception ex)
            {
                modLog.LogException(ex);
            }
        }
        // public static void FinishedLoading(List<string> loadOrder) {
        //   try {
        //     PilotAffinityManager.Instance.initialize();
        //     PilotQuirkManager.Instance.initialize();
        //   }catch (Exception ex){
        //     modLog.LogException(ex);
        //   }
        // }

        public static void Init(string modDirectory, string settingsJSON)
        {

            modDir = modDirectory;
            modLog = new Logger(modDir, "MechAffinity", true);
            legacySettings = JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText($"{modDir}/settings.json")); //if we had failed to read settings it is useless to proceed. Better notify ModTek instead.
            if (legacySettings.Check($"{modDir}/settings.json")) {
                File.WriteAllText($"{modDir}/settings.loaded.json",JsonConvert.SerializeObject(legacySettings, Formatting.Indented));
                legacySettings = JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText($"{modDir}/settings.loaded.json"));
            }
            legacySettings.InitLookups();

            if (legacySettings.enablePilotSelect)
            {
                try
                {
                    using (StreamReader reader = new StreamReader($"{modDir}/pilotselectsettings.json"))
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
