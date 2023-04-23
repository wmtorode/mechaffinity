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
    [HarmonyPatch(typeof(SGBarracksServicePanel), "SetPilot", typeof(Pilot))]
    class SGBarracksServicePanel_SetPilot
    {
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Postfix(SGBarracksServicePanel __instance, Pilot p)
        {
            string affinityDescriptors = PilotAffinityManager.Instance.getMechAffinityDescription(p);
            //Main.modLog.LogMessage(affinityDescriptors);
            LocalizableText bioText = __instance.biographyLabel;
            bioText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
            __instance.biographyLabel =bioText;
            __instance.RefreshPanel();
        }
    }
}
