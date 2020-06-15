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

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(MechBayChassisInfoWidget), "SetDescriptions")]
    class MechBayChassisInfoWidget_SetDescriptions
    {
        public static bool Prepare()
        {
            return Main.settings.showDescriptionsOnChassis;
        }
        public static void Postfix(MechBayChassisInfoWidget __instance, ChassisDef ___selectedChassis, LocalizableText ___mechDetails)
        {
            if (___selectedChassis != null)
            {
                Main.modLog.LogMessage($"finding chassisdef affinity descriptor for {___selectedChassis.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(___selectedChassis);
                //Main.modLog.LogMessage(affinityDescriptors);
                ___mechDetails.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
                //descriptor.SetValue(__instance, bioText);
                __instance.ForceRefreshImmediate();
            }
            else
            {
                Main.modLog.LogMessage("Chassisdef is null!");
            }
        }
    }
}
