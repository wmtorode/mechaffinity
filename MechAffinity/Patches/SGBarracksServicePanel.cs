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
    [HarmonyPatch(typeof(SGBarracksServicePanel), "SetPilot", typeof(Pilot))]
    class SGBarracksServicePanel_SetPilot
    {
        private static FieldInfo finfo = AccessTools.Field(typeof(SGBarracksServicePanel), "biographyLabel");
        private static MethodInfo methodRefreshPanel = AccessTools.Method(typeof(SGBarracksServicePanel), "RefreshPanel");
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Postfix(SGBarracksServicePanel __instance, Pilot p)
        {
            string affinityDescriptors = PilotAffinityManager.Instance.getMechAffinityDescription(p);
            //Main.modLog.LogMessage(affinityDescriptors);
            LocalizableText bioText = (LocalizableText)finfo.GetValue(__instance);
            bioText.AppendTextAndRefresh(affinityDescriptors, (object[])Array.Empty<object>());
            finfo.SetValue(__instance, bioText);
            methodRefreshPanel.Invoke(__instance, new object[] { });
        }
    }
}
