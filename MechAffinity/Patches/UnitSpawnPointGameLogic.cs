using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "initializeActor", typeof(AbstractActor))]
    class UnitSpawnPointGameLogic_initializeActor
    {
        public static void Postfix(SimGameState __instance, AbstractActor actor)
        {
            PilotAffinityManager.Instance.applyBonuses(actor);
        }
    }
}
