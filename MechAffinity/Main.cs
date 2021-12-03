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
        internal static Settings settings;
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
                                Settings add_settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(custMechRep.Value.FilePath));
                                if (add_settings.Check(custMechRep.Value.FilePath))
                                {
                                    File.WriteAllText(custMechRep.Value.FilePath,JsonConvert.SerializeObject(settings, Formatting.Indented));
                                    add_settings =JsonConvert.DeserializeObject<Settings>(File.ReadAllText(custMechRep.Value.FilePath));
                                }
                                settings.Merge(add_settings);
                            }
                            catch (Exception ex)
                            {
                                modLog.LogException(ex);
                            }
                        }
                    }
                }
            }
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
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{modDir}/settings.json")); //if we had failed to read settings it is useless to proceed. Better notify ModTek instead.
            if (settings.Check($"{modDir}/settings.json")) {
                File.WriteAllText($"{modDir}/settings.loaded.json",JsonConvert.SerializeObject(settings, Formatting.Indented));
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{modDir}/settings.loaded.json"));
            }

            if (settings.enablePilotSelect)
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
