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
using UnityEngine;

namespace MechAffinity.Patches
{

    [HarmonyPatch(typeof(SimGameState), "Rehydrate", typeof(GameInstanceSave))]
    class SimGameState_RehydratePatch
    {
        public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
        {
                PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
                PilotAffinityManager.Instance.setDataManager(__instance.DataManager);
                
                PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
                
                List<MechDef> mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value).ToList();
                foreach (MechDef mech in mechs)
                {
                    PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
                }

                if (Main.settings.enablePilotQuirks)
                {
                    foreach (Pilot pilot in __instance.PilotRoster.ToList())
                    {
                        PilotQuirkManager.Instance.proccessPilot(pilot.pilotDef, true);
                        pilot.FromPilotDef(pilot.pilotDef);
                    }
                    // the commander is not part of the roster, so need to specifically call it.
                    PilotQuirkManager.Instance.proccessPilot(__instance.Commander.pilotDef, true);
                    __instance.Commander.FromPilotDef(__instance.Commander.pilotDef);
                    PilotQuirkManager.Instance.forceMoraleInstanced();
                }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "InitCompanyStats")]
    class SimGameState_InitCompanyStatsPatch
    {
        public static void Postfix(SimGameState __instance)
        {

            PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
            PilotAffinityManager.Instance.setDataManager(__instance.DataManager);
            
            PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
            // new career so this will be instanced automatically
            PilotQuirkManager.Instance.forceMoraleInstanced();
            
            List<MechDef> mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value).ToList();
            foreach (MechDef mech in mechs)
            {
                PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "OnCareerModeCharacterCreationComplete")]
    class SimGameState_OnCareerModeCharacterCreationComplete
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(SimGameState __instance, Pilot p)
        {
            PilotQuirkManager.Instance.proccessPilot(__instance.Commander.pilotDef, true);
            __instance.Commander.FromPilotDef(__instance.Commander.pilotDef);
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "OnCharacterCreationComplete")]
    class SimGameState_OnCharacterCreationComplete
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Postfix(SimGameState __instance, Pilot p)
        {
            PilotQuirkManager.Instance.proccessPilot(__instance.Commander.pilotDef, true);
            __instance.Commander.FromPilotDef(__instance.Commander.pilotDef);
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
                    PilotQuirkManager.Instance.stealAmount(pilot, __instance);
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
        public static void Prefix(SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false)
        {
            if (def != null)
            {
                PilotQuirkManager.Instance.proccessPilot(def, true);
            }
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
    
    [HarmonyPatch(typeof(SimGameState), "CancelArgoUpgrade")]
    public static class SimGameState_CancelArgoUpgrade
    {
        private static int originalCost = 0;
        
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static void Prefix(SimGameState __instance, bool refund)
        {
            ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);

            float multiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.PilotRoster.ToList(),
                shipModuleUpgrade.Description.Id, false);
            
            if (refund)
            {
                originalCost = shipModuleUpgrade.PurchaseCost;
                Traverse.Create(shipModuleUpgrade).Property("PurchaseCost").SetValue((int)(originalCost * multiplier));
            }
        }
        public static void Postfix(SimGameState __instance, bool refund)
        {
            if (refund)
            {
                ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);
                Traverse.Create(shipModuleUpgrade).Property("PurchaseCost").SetValue(originalCost);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures", new Type[] {typeof(EconomyScale), typeof(bool)})]
    public static class SimGameState_GetExpenditures
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static bool Prefix(SimGameState __instance, EconomyScale expenditureLevel, bool proRate, int  ___ProRateRefund, ref int __result)
        {
            FinancesConstantsDef finances = __instance.Constants.Finances;
            int baseMaintenanceCost = __instance.GetShipBaseMaintenanceCost();
            for (int index = 0; index < __instance.ShipUpgrades.Count; ++index)
            {
                float pilotQurikModifier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.PilotRoster.ToList(),
                        __instance.ShipUpgrades[index].Description.Id, true);
                float baseCost = (float) __instance.ShipUpgrades[index].AdditionalCost * pilotQurikModifier;
                baseMaintenanceCost += Mathf.CeilToInt(baseCost * __instance.Constants.CareerMode.ArgoMaintenanceMultiplier);
            }
            foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                baseMaintenanceCost += finances.MechCostPerQuarter;
            for (int index = 0; index < __instance.PilotRoster.Count; ++index)
                baseMaintenanceCost += __instance.GetMechWarriorValue(__instance.PilotRoster[index].pilotDef);
            float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
            __result = Mathf.CeilToInt((float) (baseMaintenanceCost - (proRate ? ___ProRateRefund : 0)) * expenditureCostModifier);
            return false;
        }
    }

    [HarmonyPatch(typeof(SimGameState), "FirstTimeInitializeDataFromDefs")]
    public static class SimGameState_FirstTimeInitializeDataFromDefs
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotSelect;
        }
        public static void Postfix(SimGameState __instance)
        {
            PilotRandomizerManager.Instance.setStartingRonin(__instance);
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "OnNewQuarterBegin")]
    public static class OnNewQuarterBeginSimGameStateBattleTechPatch
    {
        public static bool Prepare()
        {
            return Main.settings.enableMonthlyMoraleReset;
        }
        public static void Postfix(SimGameState __instance)
        {
            PilotQuirkManager.Instance.resetMorale(__instance);
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "AddMorale")]
    public static class AddMoralePatch
    {
        public static bool Prepare()
        {
            return Main.settings.enableMonthlyMoraleReset;
        }
        public static bool Prefix(SimGameState __instance, int val, string sourceID)
        {
            if (sourceID == null)
                sourceID = nameof (SimGameState);
            if (__instance.CompanyStats.ContainsStatistic("Morale"))
                __instance.CompanyStats.ModifyStat<int>(sourceID, 0, "Morale", StatCollection.StatOperation.Int_Add, val);
            else
            {
                __instance.CompanyStats.AddStatistic<int>("Morale", val,
                    new Statistic.Validator<int>(__instance.MinimumZeroMaximumFiftyValidator<int>));
                //now that morale exists as a stat correctly set/calculate it
                PilotQuirkManager.Instance.resetMorale(__instance);
            }
            __instance.RoomManager.RefreshDisplay();
            return false;
        }
    }

}
