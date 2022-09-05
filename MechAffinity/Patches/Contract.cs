using System;
using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;
using BattleTech.UI.TMProWrapper;
using Localize;
using Harmony;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MechAffinity;
using Object = UnityEngine.Object;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(Contract), "FinalizeKilledMechWarriors")]
    class Contract_FinalizeKilledMechWarriors
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static void Prefix(Contract __instance, SimGameState sim)
        {
            Main.modLog.LogMessage($"Contract Finalize Killed Pilots Starting");
        }
        
        public static void Postfix(Contract __instance, SimGameState sim)
        {
            Main.modLog.LogMessage($"Contract Finalize Killed Pilots done");
        }
    }
    
    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch
    {
        static void Postfix()
        {
            if (Main.settings.enablePilotAffinity)
            {
                PilotAffinityManager.Instance.ResetEffectCache();
            }

            if (Main.settings.enablePilotQuirks)
            {
                PilotQuirkManager.Instance.ResetEffectCache();
            }
            
        }
    }
    
    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    public static class Contract_GenerateSalvage
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        static void Postfix(Contract __instance)
        {
            int additionalSalvage = 0;
            int additionalSalvagePicks = 0;
            
            Main.modLog.LogMessage($"Generating Salvage picks Start: {__instance.FinalPrioritySalvageCount}/{__instance.FinalSalvageCount}");

            foreach (var unitResult in __instance.PlayerUnitResults)
            {
                PilotQuirkManager.Instance.additionalSalvage(unitResult.pilot.pilotDef, ref additionalSalvage, ref additionalSalvagePicks);
            }

            if (additionalSalvage >= 0)
            {
                Traverse.Create(__instance).Property("FinalSalvageCount").SetValue((int) (__instance.FinalSalvageCount + additionalSalvage));
            }

            if (additionalSalvagePicks >= 0)
            {
                // BT Salavage Screen UI cannot handle more than 7 priority picks, do not allow more
                Traverse.Create(__instance).Property("FinalPrioritySalvageCount").SetValue((int) Math.Min(__instance.FinalPrioritySalvageCount + additionalSalvagePicks, 7));
            }
            
            Main.modLog.LogMessage($"Generating Salvage picks Finish: {__instance.FinalPrioritySalvageCount}/{__instance.FinalSalvageCount}");
        }
    }
}