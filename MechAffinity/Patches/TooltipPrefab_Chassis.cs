using System;
using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using Localize;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MechAffinity;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(TooltipPrefab_Chassis), "SetData", typeof(object))]
    class TooltipPrefab_Chassis_SetData
    {
        public static bool Prepare()
        {
            return Main.settings.affinitySettings.showDescriptionsOnChassis && Main.settings.enablePilotAffinity;
        }

        public static void Postfix(TooltipPrefab_Chassis __instance, object data)
        {

            if (data is ChassisDef chassisDef)
            {
                Main.modLog.Info?.Write($"finding chassisdef affinity descriptor for {chassisDef.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(chassisDef);
                //Main.modLog.Info?.Write(affinityDescriptors);
                __instance.descriptionText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
            }
            else
            {
                Main.modLog.Info?.Write("chassisdef is null!");
            }
        }
    }
}
