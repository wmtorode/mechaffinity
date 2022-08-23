using System;
using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using Localize;
using Harmony;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MechAffinity;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(TooltipPrefab_Mech), "SetData", typeof(object))]
    class TooltipPrefab_Mech_SetData
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        
        public static void Postfix(TooltipPrefab_Mech __instance, object data, LocalizableText ___DetailsField)
        {
            
            if(data is MechDef mechDef)
            {
                Main.modLog.LogMessage($"finding mechdef affinity descriptor for {mechDef.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(mechDef);
                //Main.modLog.LogMessage(affinityDescriptors);
                ___DetailsField.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
            }
            else
            {
                Main.modLog.LogMessage("mechdef is null!");
            }
        }
    }
}
