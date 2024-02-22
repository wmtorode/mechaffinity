﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using MechAffinity.Data;
using System.Reflection;
using BattleTech;
using MechAffinity.Data.PilotManagement;
using Newtonsoft.Json.Linq;

namespace MechAffinity
{
    public class Main
    {
        private const string SettingsFilePath = "settings.json";
        private const string LegacyFilePath = "settings.legacy.json";
        private const string PilotSelectSettingsFilePath = "pilotselectsettings.json";
        
        internal static DeferringLogger modLog;
        internal static Settings settings;
        internal static PilotSelectSettings pilotSelectSettings = new PilotSelectSettings();
        internal static string modDir;
        internal static readonly string AffinitiesDefinitionTypeName = "AffinitiesDef";
        internal static readonly string QuirkDefTypeName = "QuirkDef";
        internal static readonly string LanceQuirkDefTypeName = "LanceQuirkDef";
        internal static readonly string PilotRequirementsDefTypeName = "PilotRequirementsDef";
        internal static readonly string RoninSpawnModifierDefTypeName = "RoninSpawnModifierDef";
        internal static List<AffinityDef> affinityDefs = new List<AffinityDef>();
        internal static List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
        internal static List<LanceQuirkDef> LanceQuirks = new List<LanceQuirkDef>();
        internal static List<PilotRequirementsDef> PilotRequirementsDefs = new List<PilotRequirementsDef>();
        internal static List<RoninSpawnModifierDef> RoninSpawnModifiers = new List<RoninSpawnModifierDef>();

        public static void FinishedLoading(List<string> loadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> customResources)
        {
            if (customResources != null)
            {
                foreach (var customResource in customResources)
                {
                    modLog.Info?.Write("customResource:" + customResource.Key);
                    if (customResource.Key == AffinitiesDefinitionTypeName)
                    {
                        foreach (var affinityDefPath in customResource.Value)
                        {
                            try
                            {
                                modLog.Info?.Write("Path:" + affinityDefPath.Value.FilePath);
                                AffinityDef affinityDef = JsonConvert.DeserializeObject<AffinityDef>(File.ReadAllText(affinityDefPath.Value.FilePath));
                                affinityDefs.Add(affinityDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.Error?.Write(ex);
                            }
                        }
                    }
                    if (customResource.Key == QuirkDefTypeName)
                    {
                        foreach (var quirkDefPath in customResource.Value)
                        {
                            try
                            {
                                modLog.Info?.Write("Path:" + quirkDefPath.Value.FilePath);
                                PilotQuirk quirkDef = JsonConvert.DeserializeObject<PilotQuirk>(File.ReadAllText(quirkDefPath.Value.FilePath));
                                pilotQuirks.Add(quirkDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.Error?.Write(ex);
                            }
                        }
                    }
                    if (customResource.Key == LanceQuirkDefTypeName)
                    {
                        foreach (var quirkDefPath in customResource.Value)
                        {
                            try
                            {
                                modLog.Info?.Write("Path:" + quirkDefPath.Value.FilePath);
                                LanceQuirkDef quirkDef = JsonConvert.DeserializeObject<LanceQuirkDef>(File.ReadAllText(quirkDefPath.Value.FilePath));
                                LanceQuirks.Add(quirkDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.Error?.Write(ex);
                            }
                        }
                    }
                    if (customResource.Key == RoninSpawnModifierDefTypeName)
                    {
                        foreach (var roninSpawnDef in customResource.Value)
                        {
                            try
                            {
                                modLog.Info?.Write("Path:" + roninSpawnDef.Value.FilePath);
                                RoninSpawnModifierDef spawnDef = JsonConvert.DeserializeObject<RoninSpawnModifierDef>(File.ReadAllText(roninSpawnDef.Value.FilePath));
                                RoninSpawnModifiers.Add(spawnDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.Error?.Write(ex);
                            }
                        }
                    }
                    if (customResource.Key == PilotRequirementsDefTypeName)
                    {
                        foreach (var requirementsDefPath in customResource.Value)
                        {
                            try
                            {
                                modLog.Info?.Write("Path:" + requirementsDefPath.Value.FilePath);
                                // HBS was silly with TagSets and one of the deserialized fields needed is private, so have to use their silly Deserializer instead of a proper json one
                                // PilotRequirementsDef requirementsDef = JsonConvert.DeserializeObject<PilotRequirementsDef>(File.ReadAllText(requirementsDefPath.Value.FilePath));
                                PilotRequirementsDef requirementsDef = new PilotRequirementsDef();
                                requirementsDef.FromJSON(File.ReadAllText(requirementsDefPath.Value.FilePath));
                                PilotRequirementsDefs.Add(requirementsDef);
                            }
                            catch (Exception ex)
                            {
                                modLog.Error?.Write(ex);
                            }
                        }
                    }
                }
            }
            try {
                if (settings.legacyData.debug_writeLegacyAffinityData)
                {
                    File.WriteAllText($"{modDir}/{LegacyFilePath}",JsonConvert.SerializeObject(
                        settings.ToLegacy(affinityDefs, pilotQuirks), Formatting.Indented));
                }
                if (settings.enablePilotAffinity) PilotAffinityManager.Instance.initialize(settings.affinitySettings, affinityDefs);
                if (settings.enablePilotQuirks) PilotQuirkManager.Instance.initialize(settings.quirkSettings, pilotQuirks, LanceQuirks);
            }
            catch (Exception ex)
            {
                modLog.Error?.Write(ex);
            }
        }

        private static void convertLegacyToSettings(bool throwError)
        {
            LegacySettings legacySettings = JsonConvert.DeserializeObject<LegacySettings>(File.ReadAllText($"{modDir}/{SettingsFilePath}"));
            Settings newSettings = Settings.FromLegacy(legacySettings, modDir);
            File.WriteAllText($"{modDir}/{SettingsFilePath}",JsonConvert.SerializeObject(newSettings, Formatting.Indented));
            File.WriteAllText($"{modDir}/{LegacyFilePath}",JsonConvert.SerializeObject(legacySettings, Formatting.Indented));
            if (throwError) throw new NotSupportedException("Legacy Settings File Converted, make sure mod.json has been updated!");
        }

        public static void Init(string modDirectory, string settingsJSON)
        {

            modDir = modDirectory;
            modLog = new DeferringLogger(modDir, "MechAffinity", true);

            var settingsData = JObject.Parse(File.ReadAllText($"{modDir}/{SettingsFilePath}"));
            JToken version;
            if (!settingsData.TryGetValue("version", out version)) convertLegacyToSettings(true);
            
            //if we fail to read settings it is useless to proceed. Better notify ModTek instead, by allowing the exception
            // to be raised
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{modDir}/{SettingsFilePath}"));
            modLog.setDebug(settings.debug);

            if (settings.enablePilotSelect)
            {
                // Keep Pilot Select Settings separate, potential for allowing players to do custom player starts in RT
                // which would necessitate leaving the settings separate to exclude only them from launcher protections
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
                    modLog.Error?.Write(ex);
                }
            }
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "ca.jwolf.MechAffinity");

        }
    }
}
