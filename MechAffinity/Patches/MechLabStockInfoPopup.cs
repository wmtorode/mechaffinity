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
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Postfix(MechLabStockInfoPopup __instance, MechDef def, LocalizableText ___descriptionText)
        {
            if (def != null)
            {
                Main.modLog.LogMessage($"finding mechdef affinity descriptor for {def.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(def);
                //Main.modLog.LogMessage(affinityDescriptors);
                ___descriptionText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
                //descriptor.SetValue(__instance, bioText);
                __instance.ForceRefreshImmediate();
            }
            else
            {
                Main.modLog.LogMessage("mechdef is null!");
            }
        }
    }
}
