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
                    }
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
        private static float CalculateCBillValue(MechDef mech)
        {
            float num = 10000f;
            float currentCBillValue = (float)mech.Chassis.Description.Cost;
            float num2 = 0f;
            num2 += mech.Head.CurrentArmor;
            num2 += mech.CenterTorso.CurrentArmor;
            num2 += mech.CenterTorso.CurrentRearArmor;
            num2 += mech.LeftTorso.CurrentArmor;
            num2 += mech.LeftTorso.CurrentRearArmor;
            num2 += mech.RightTorso.CurrentArmor;
            num2 += mech.RightTorso.CurrentRearArmor;
            num2 += mech.LeftArm.CurrentArmor;
            num2 += mech.RightArm.CurrentArmor;
            num2 += mech.LeftLeg.CurrentArmor;
            num2 += mech.RightLeg.CurrentArmor;
            num2 *= UnityGameInstance.BattleTechGame.MechStatisticsConstants.CBILLS_PER_ARMOR_POINT;
            currentCBillValue += num2;
            for (int i = 0; i < mech.Inventory.Length; i++)
            {
                MechComponentRef mechComponentRef = mech.Inventory[i];
                currentCBillValue += (float)mechComponentRef.Def.Description.Cost;
            }
            currentCBillValue = Mathf.Round(currentCBillValue / num) * num;
            return currentCBillValue;
        }
        public static bool Prepare()
        {
            if (Main.settings.enablePilotQuirks || Main.settings.MechMaintenanceByCost)
                return true;
            else
                return false;
        }
        
        public static bool Prefix(SimGameState __instance, EconomyScale expenditureLevel, bool proRate, int  ___ProRateRefund, ref int __result)
        {
            FinancesConstantsDef finances = __instance.Constants.Finances;
            int baseMaintenanceCost = __instance.GetShipBaseMaintenanceCost();
            float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
            for (int index = 0; index < __instance.ShipUpgrades.Count; ++index)
            {
                float baseCost = (float)__instance.ShipUpgrades[index].AdditionalCost;
                if (Main.settings.enablePilotQuirks)
                {
                    float pilotQurikModifier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.PilotRoster.ToList(),
                        __instance.ShipUpgrades[index].Description.Id, true);
                    baseCost = (float)__instance.ShipUpgrades[index].AdditionalCost * pilotQurikModifier;
                }
                baseMaintenanceCost += Mathf.CeilToInt(baseCost * __instance.Constants.CareerMode.ArgoMaintenanceMultiplier);
            }
            foreach (MechDef mechDef in __instance.ActiveMechs.Values)
            {
                if(Main.settings.MechMaintenanceByCost)
                {
                    if (Main.settings.MMBC_CostByTons)
                    {
                        baseMaintenanceCost += Mathf.RoundToInt((float)mechDef.Chassis.Tonnage * Main.settings.MMBC_cbillsPerTon * expenditureCostModifier);
                        if (Main.settings.MMBC_TonsAdditive)
                            baseMaintenanceCost += Mathf.RoundToInt(CalculateCBillValue(mechDef) * Main.settings.MMBC_PercentageOfMechCost * expenditureCostModifier);
                    }
                    else
                        baseMaintenanceCost += Mathf.RoundToInt(CalculateCBillValue(mechDef) * Main.settings.MMBC_PercentageOfMechCost * expenditureCostModifier);
                }
                else
                    baseMaintenanceCost += finances.MechCostPerQuarter;
            }
            for (int index = 0; index < __instance.PilotRoster.Count; ++index)
                baseMaintenanceCost += __instance.GetMechWarriorValue(__instance.PilotRoster[index].pilotDef);
            __result = Mathf.CeilToInt((float) (baseMaintenanceCost - (proRate ? ___ProRateRefund : 0)) * expenditureCostModifier);
            return false;
        }
    }
    
}
