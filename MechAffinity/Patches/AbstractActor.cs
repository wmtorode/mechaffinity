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

    [HarmonyPatch(typeof(AbstractActor), "get_HasBreachingShotAbility")]
    class AbstractActor_HasBreachingShotAbility
    {
        public static void Postfix(AbstractActor __instance, ref bool __result)
        {
            if (!__result)
            {
                __result = __instance.StatCollection.GetValue<bool>(AttackSequence_IsBreachingShot.superBreachingShot);
            }
        }
    }
    
    [HarmonyPatch(typeof(AbstractActor), "IsUsingBreachingShotAbility")]
    class AbstractActor_IsUsingBreachingShotAbility
    {
        public static void Postfix(AbstractActor __instance, ref bool __result)
        {
            if (!__result)
            {
                __result = __instance.StatCollection.GetValue<bool>(AttackSequence_IsBreachingShot.superBreachingShot);
            }
        }
    }
}