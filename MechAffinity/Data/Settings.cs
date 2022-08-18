using System.IO;
using Newtonsoft.Json;

namespace MechAffinity.Data
{
    public class Settings
    {
        //Logging Features
        public bool debug = false;
        
        // Feature Enables
        public bool enablePilotSelect = false;
        
        // Legacy Settings Debug data
        public LegacyData legacyData = new LegacyData();
        
        
        //Helpers
        internal static Settings FromLegacy(LegacySettings legacySettings)
        {
            Settings settings = new Settings();

            settings.debug = legacySettings.debug;
            settings.enablePilotSelect = legacySettings.enablePilotSelect;

            System.IO.Directory.CreateDirectory("AffinityDefs");
            foreach (var globalAffinity in legacySettings.globalAffinities)
            {
                AffinityDef affinityDef = new AffinityDef()
                {
                    id = "AffinityDef_global_" + globalAffinity.id,
                    affinityType = EAffinityDefType.Global
                };
                affinityDef.setAffinityData(globalAffinity);
                File.WriteAllText($"AffinityDefs/{affinityDef.id}.json",JsonConvert.SerializeObject(settings, Formatting.Indented));
                
            }

            int counter = 0;
            foreach (var chassisAffinity in legacySettings.chassisAffinities)
            {
                AffinityDef affinityDef = new AffinityDef()
                {
                    id = "AffinityDef_chassis_" + chassisAffinity.id,
                    affinityType = EAffinityDefType.Chassis
                };
                counter++;
                affinityDef.setAffinityData(chassisAffinity);
                File.WriteAllText($"AffinityDefs/{affinityDef.id}.json",JsonConvert.SerializeObject(settings, Formatting.Indented));
                
            }
            foreach (var quirkAffinity in legacySettings.quirkAffinities)
            {
                AffinityDef affinityDef = new AffinityDef()
                {
                    id = "AffinityDef_quirk_" + quirkAffinity.id,
                    affinityType = EAffinityDefType.Quirk
                };
                counter++;
                affinityDef.setAffinityData(quirkAffinity);
                File.WriteAllText($"AffinityDefs/{affinityDef.id}.json",JsonConvert.SerializeObject(settings, Formatting.Indented));
                
            }
            foreach (var taggedAffinity in legacySettings.taggedAffinities)
            {
                AffinityDef affinityDef = new AffinityDef()
                {
                    id = "AffinityDef_tagged_" + taggedAffinity.id,
                    affinityType = EAffinityDefType.Tag
                };
                counter++;
                affinityDef.setAffinityData(taggedAffinity);
                File.WriteAllText($"AffinityDefs/{affinityDef.id}.json",JsonConvert.SerializeObject(settings, Formatting.Indented));
                
            }

            return settings;
        }
    }
}