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
    [HarmonyPatch(typeof(MechDetails), "SetDescriptions")]
    class MechDetails_SetDescriptions
    {
        private static FieldInfo descriptor = AccessTools.Field(typeof(MechDetails), "mechDescription");
        private static FieldInfo mechdef = AccessTools.Field(typeof(MechDetails), "activeMech");
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Postfix(MechDetails __instance)
        {
            MechDef mech = (MechDef)mechdef.GetValue(__instance);
            if (mech != null)
            {
                Main.modLog.LogMessage($"finding mechdef affinity descriptor for {mech.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(mech);
                //Main.modLog.LogMessage(affinityDescriptors);
                LocalizableText bioText = (LocalizableText)descriptor.GetValue(__instance);
                bioText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
                descriptor.SetValue(__instance, bioText);           
            }
            else
            {
                Main.modLog.LogMessage("mechdef is null!");
            }
        }
    }
}
