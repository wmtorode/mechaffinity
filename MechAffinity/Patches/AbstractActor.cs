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
    [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
    class AbstractActor_InitEffectStats
    {
        static void Postfix(AbstractActor __instance)
        {
            __instance.StatCollection.AddStatistic<bool>(AttackSequence_IsBreachingShot.superBreachingShot, false);
        }
    }
}