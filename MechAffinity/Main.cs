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

namespace MechAffinity
{
    public class Main
    {
        internal static Logger modLog;
        internal static Settings settings;
        internal static string modDir;

        public static void Init(string modDirectory, string settingsJSON)
        {

            modDir = modDirectory;
            modLog = new Logger(modDir, "MechAffinity", true);

            try
            {
                using (StreamReader reader = new StreamReader($"{modDir}/settings.json"))
                {
                    string jdata = reader.ReadToEnd();
                    settings = JsonConvert.DeserializeObject<Settings>(jdata);
                }
                PilotAffinityManager.Instance.initialize();
                PilotQuirkManager.Instance.initialize();

            }

            catch (Exception ex)
            {
                modLog.LogException(ex);
            }

            var harmony = HarmonyInstance.Create("ca.jwolf.MechAffinity");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }
    }
}
