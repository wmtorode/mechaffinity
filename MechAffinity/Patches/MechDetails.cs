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
    [HarmonyPatch(typeof(MechDetails), "SetDescriptions")]
    class MechDetails_SetDescriptions
    {
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Postfix(MechDetails __instance)
        {
            MechDef mech = __instance.activeMech;
            if (mech != null)
            {
                Main.modLog.Info?.Write($"finding mechdef affinity descriptor for {mech.Description.UIName}");
                string affinityDescriptors = PilotAffinityManager.Instance.getMechChassisAffinityDescription(mech);
                //Main.modLog.Info?.Write(affinityDescriptors);
                LocalizableText bioText = __instance.mechDescription;
                bioText.AppendTextAndRefresh(affinityDescriptors, Array.Empty<object>());
                __instance.mechDescription = bioText;           
            }
            else
            {
                Main.modLog.Info?.Write("mechdef is null!");
            }
        }
    }
}
