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
    [HarmonyPatch(typeof(Mech), "AddInstability")]
    class Mech_AddInstability
    {
        public static bool Prepare()
        {
            return Main.settings.enableStablePiloting;
        }
        
        public static void Prefix(ref bool __runOriginal, Mech __instance, ref float amt)
        {
            if (!__runOriginal)
            {
                return;
            }
            
            amt *= StablePilotingManager.Instance.getStabilityModifier(__instance.pilot);
        }
    }
}