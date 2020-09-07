using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using BattleTech;
using BattleTech.Save;
using Localize;
using MechAffinity;

namespace MechAffinity.Patches
{

    [HarmonyPatch(typeof(SimGameState), "Rehydrate", typeof(GameInstanceSave))]
    class SimGameState_RehydratePatch
    {
        public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
        {
                PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
                PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
                
                List<MechDef> mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value).ToList();
                foreach (MechDef mech in mechs)
                {
                    PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
                }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "InitCompanyStats")]
    class SimGameState_InitCompanyStatsPatch
    {
        public static void Postfix(SimGameState __instance)
        {

            PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
            PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
            
            List<MechDef> mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value).ToList();
            foreach (MechDef mech in mechs)
            {
                PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
    class SimGameState_ResolveCompleteContract
    {
        public static void Prefix(SimGameState __instance)
        {
            if (__instance.CompletedContract != null)
            {
                List<UnitResult> results = __instance.CompletedContract.PlayerUnitResults;
                List<Pilot> pilotList = new List<Pilot>((IEnumerable<Pilot>)__instance.PilotRoster);
                pilotList.Add(__instance.Commander);
                foreach (UnitResult result in results)
                {
                    foreach (Pilot pilot in pilotList)
                    {
                        if (result.pilot.pilotDef.Description.Id == pilot.pilotDef.Description.Id)
                        {
                            PilotAffinityManager.Instance.incrementDeployCountWithMech(result);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnDayPassed")]
    class SimGameState_OnDayPassed
    {
        public static void Postfix(SimGameState __instance)
        {
            List<Pilot> pilotList = new List<Pilot>((IEnumerable<Pilot>)__instance.PilotRoster);
            pilotList.Add(__instance.Commander);
            foreach (Pilot pilot in pilotList)
            {
                bool decayed = PilotAffinityManager.Instance.onSimDayElapsed(pilot);
                if (decayed)
                {                 
                    __instance.RoomManager.ShipRoom.AddEventToast(new Text(string.Format("{0} affinities decayed!", (object)pilot.Callsign), (object[])Array.Empty<object>()));
                }

                if (Main.settings.enablePilotQuirks)
                {
                    int stolen = PilotQuirkManager.Instance.stealAmount(pilot);
                    if (stolen > 0)
                    {
                        __instance.AddFunds(stolen, null, true);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetMechWarriorValue")]
    class SimGameState_GetMechWarriorValue
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(SimGameState __instance, PilotDef def, ref int __result)
        {
            float multiplier = PilotQuirkManager.Instance.getPilotCostMulitplier(def);

            __result = (int)(__result * multiplier);
        }
    }


    [HarmonyPatch(typeof(SimGameState), "GetMechWarriorHiringCost")]
    class SimGameState_GetMechWarriorHiringCost
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(SimGameState __instance, PilotDef def, ref int __result)
        {
            float multiplier = PilotQuirkManager.Instance.getPilotCostMulitplier(def);

            __result = (int)(__result * multiplier);
        }
    }

    [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", typeof(PilotDef), typeof(bool), typeof(bool))]
    class SimGameState_AddPilotToRoster
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false)
        {
            PilotQuirkManager.Instance.proccessPilot(def, true);
        }
    }

    [HarmonyPatch(typeof(SimGameState), "KillPilot",
        new Type[] {typeof(Pilot), typeof(bool), typeof(string), typeof(string)})]
    public static class SimGameState_KillPilot
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Prefix(SimGameState __instance, Pilot p)
        {
            if (p != null && (__instance.PilotRoster.Contains(p)))
            {
                PilotDef def = p.pilotDef;
                if (def != null)
                {
                    PilotQuirkManager.Instance.proccessPilot(def, false);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] {typeof(Pilot)})]
    public static class SimGameState_DismissPilot
    {
        public static void Prefix(SimGameState __instance, Pilot p)
        {
            if (p != null)
            {
                PilotDef def = p.pilotDef;
                if (def != null)
                {
                    PilotQuirkManager.Instance.proccessPilot(def, false);
                }
            }
        }
    }
}
