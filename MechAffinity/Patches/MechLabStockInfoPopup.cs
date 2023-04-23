using System;
using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;
using BattleTech.UI.TMProWrapper;
using Localize;
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
        public static void Postfix(MechLabStockInfoPopup __instance, MechDef def)
        {
            if (def != null)
            {
                Main.modLog.Info?.Write($"finding mechdef affinity descriptor for {def.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(def);
                //Main.modLog.Info?.Write(affinityDescriptors);
                __instance.descriptionText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
                //descriptor.SetValue(__instance, bioText);
                __instance.ForceRefreshImmediate();
            }
            else
            {
                Main.modLog.Info?.Write("mechdef is null!");
            }
        }
    }
}
