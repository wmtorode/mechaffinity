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
        private static MethodInfo reportLog = AccessTools.Method(typeof(Contract), "ReportLog");
        private static MethodInfo popReport = AccessTools.Method(typeof(Contract), "PopReport");
        private static MethodInfo pushReport = AccessTools.Method(typeof(Contract), "PushReport");
        
        public static bool Prepare()
        {
            return Main.legacySettings.enablePilotQuirks;
        }
        
        public static bool Prefix(Contract __instance, SimGameState sim)
        {
            pushReport.Invoke(__instance, new object[] {"MechWarriorFinalizeKill" });
            foreach (UnitResult playerUnitResult in __instance.PlayerUnitResults)
            {
                Pilot pilot = playerUnitResult.pilot;
                PilotDef pilotDef = pilot.pilotDef;
                bool immortal = PilotQuirkManager.Instance.isPilotImmortal(pilot);
                Main.modLog.DebugMessage($"Pilot: {pilot.Callsign}, Immortal: {immortal}");
                if (!playerUnitResult.pilot.IsIncapacitated || immortal)
                {
                    pilotDef?.SetRecentInjuryDamageType(DamageType.NOT_SET);
                }
                else
                {
                    float num1 = Mathf.Max(0.0f, (pilot.LethalInjuries ? sim.Constants.Pilot.LethalDeathChance : sim.Constants.Pilot.IncapacitatedDeathChance) - sim.Constants.Pilot.GutsDeathReduction * (float) pilot.Guts);
                    float num2 = sim.NetworkRandom.Float();
                    reportLog.Invoke(__instance, new object[] {string.Format("Pilot {0} needs to roll above {1} to survive. They roll {2} resulting in {3}", (object) pilot.Name, (object) num1, (object) num2, (double) num2 < (double) num1 ? (object) "DEATH" : (object) "LIFE")});
                    if ((double) num2 < (double) num1)
                        __instance.KilledPilots.Add(pilot);
                    else
                        pilotDef?.SetRecentInjuryDamageType(DamageType.NOT_SET);
                }
            }
            popReport.Invoke(__instance, new object[] { });
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract_Patch
    {
        static void Postfix()
        {
            PilotAffinityManager.Instance.ResetEffectCache();
            if (Main.legacySettings.enablePilotQuirks)
            {
                PilotQuirkManager.Instance.ResetEffectCache();
            }
            
        }
    }
}