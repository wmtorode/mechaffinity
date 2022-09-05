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
}