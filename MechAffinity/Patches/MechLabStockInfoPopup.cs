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
    [HarmonyPatch(typeof(MechLabStockInfoPopup), "StockMechDefLoaded", typeof(string), typeof(MechDef))]
    class MechLabStockInfoPopup_StockMechDefLoaded
    {
        private static FieldInfo descriptor = AccessTools.Field(typeof(MechLabStockInfoPopup), "descriptionText");
        public static void Postfix(MechDetails __instance, MechDef def)
        {
            if (def != null)
            {
                Main.modLog.LogMessage($"finding mechdef affinity descriptor for {def.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(def);
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
