using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Save;
using BattleTech.UI.Tooltips;
using HBS;
using HBS.Collections;
using Localize;
using MechAffinity;
using MechAffinity.Data;
using SVGImporter;
using UnityEngine;

namespace MechAffinity.Patches
{

    [HarmonyPatch(typeof(SimGameState), "Rehydrate", typeof(GameInstanceSave))]
    class SimGameState_RehydratePatch
    {
        public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
        {
            if (Main.settings.enablePilotAffinity)
            {
                PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
                PilotAffinityManager.Instance.setDataManager(__instance.DataManager);
                
                var mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value);
                foreach (MechDef mech in mechs)
                {
                    PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
                }
            }
            if (Main.settings.enablePilotQuirks)
            {
                PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
                foreach (Pilot pilot in __instance.PilotRoster.rootList)
                {
                    PilotQuirkManager.Instance.proccessPilot(pilot.pilotDef, true);
                    pilot.FromPilotDef(pilot.pilotDef);
                }
                // the commander is not part of the roster, so need to specifically call it.
                PilotQuirkManager.Instance.proccessPilot(__instance.Commander.pilotDef, true);
                __instance.Commander.FromPilotDef(__instance.Commander.pilotDef);
                PilotQuirkManager.Instance.forceMoraleInstanced();
                PilotQuirkManager.Instance.BlockFinanceScreenUpdate = false;
                PilotQuirkManager.Instance.ResetArgoCostCache();
            }

            if (Main.settings.enableMonthlyTechAdjustments)
            {
                MonthlyTechAdjustmentManager.Instance.setCompanyStats(__instance.CompanyStats, __instance);
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "_OnAttachUXComplete")]
    class SimGameState_OnAttachUXComplete
    {
        public static void Postfix(SimGameState __instance)
        {
            PilotUiManager.Instance.issueLoadRequests();
        }
    }

    [HarmonyPatch(typeof(SimGameState), "InitCompanyStats")]
    class SimGameState_InitCompanyStatsPatch
    {
        public static void Postfix(SimGameState __instance)
        {
            if (Main.settings.enablePilotAffinity)
            {
                PilotAffinityManager.Instance.setCompanyStats(__instance.CompanyStats);
                PilotAffinityManager.Instance.setDataManager(__instance.DataManager);
                
                var mechs = __instance.DataManager.MechDefs.Select(pair => pair.Value);
                foreach (MechDef mech in mechs)
                {
                    PilotAffinityManager.Instance.addToChassisPrefabLut(mech);
                }

            }

            if (Main.settings.enablePilotQuirks)
            {
                PilotQuirkManager.Instance.setCompanyStats(__instance.CompanyStats);
                // new career so this will be instanced automatically
                PilotQuirkManager.Instance.forceMoraleInstanced();
            }
            
            if (Main.settings.enableMonthlyTechAdjustments)
            {
                MonthlyTechAdjustmentManager.Instance.setCompanyStats(__instance.CompanyStats, __instance);
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
        public static bool Prepare()
        {
            return Main.settings.enablePilotAffinity;
        }
        public static void Prefix(ref bool __runOriginal, SimGameState __instance)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
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
        public static void Prefix(ref bool __runOriginal, SimGameState __instance)
        {

            if (!__runOriginal)
            {
                return;
            }
            
            PilotQuirkManager.Instance.BlockFinanceScreenUpdate = true;
        }

        public static void Postfix(SimGameState __instance)
        {
            int totalStolen = 0;
            foreach (Pilot pilot in __instance.PilotRoster.rootList)
            {
                if (Main.settings.enablePilotAffinity)
                {
                    bool decayed = PilotAffinityManager.Instance.onSimDayElapsed(pilot);
                    if (decayed)
                    {
                        __instance.RoomManager.ShipRoom.AddEventToast(new Text(
                            string.Format("{0} affinities decayed!", pilot.Callsign),
                            Array.Empty<object>()));
                    }
                }

                if (Main.settings.enablePilotQuirks)
                {
                   totalStolen += PilotQuirkManager.Instance.stealAmount(pilot, __instance);
                }
            }
            // commander is not part of the roaster, adding them to the list of pilots to do in the above loop
            // is more expensive than to just repeat this code here
            if (Main.settings.enablePilotAffinity)
            {
                bool decayed = PilotAffinityManager.Instance.onSimDayElapsed(__instance.Commander);
                if (decayed)
                {
                    __instance.RoomManager.ShipRoom.AddEventToast(new Text(
                        string.Format("{0} affinities decayed!", __instance.Commander.Callsign),
                        Array.Empty<object>()));
                }
            }

            if (Main.settings.enablePilotQuirks)
            {
                totalStolen += PilotQuirkManager.Instance.stealAmount(__instance.Commander, __instance);
            }

            if (totalStolen != 0)
            {
                PilotQuirkManager.Instance.BlockFinanceScreenUpdate = true;
                __instance.AddFunds(totalStolen, null, true);
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
            int flatCost;
            float multiplier = PilotQuirkManager.Instance.getPilotCostModifier(def, out flatCost);

            __result = (int)((__result + flatCost) * multiplier);
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
            int flatCost;
            float multiplier = PilotQuirkManager.Instance.getPilotCostModifier(def, out flatCost);

            __result = (int)((__result + flatCost) * multiplier);
        }
    }

    [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", typeof(PilotDef), typeof(bool), typeof(bool))]
    class SimGameState_AddPilotToRoster
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, PilotDef def, bool updatePilotDiscardPile = false)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            if (def != null)
            {
                PilotQuirkManager.Instance.ResetArgoCostCache();
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
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, Pilot p, ref bool __result)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            if (p != null && (__instance.PilotRoster.Contains(p)))
            {
                PilotDef def = p.pilotDef;
                if (def != null)
                {
                    // if the pilot is supposed to be killed, but is immortal, dont kill them
                    if (PilotQuirkManager.Instance.hasImmortality(def))
                    {
                        Main.modLog.Info?.Write($"Preventing death of pilot: {def.Description.Callsign}");
                        __result = true;
                        __runOriginal = false;
                        return;
                    }
                    PilotQuirkManager.Instance.ResetArgoCostCache();
                    PilotQuirkManager.Instance.proccessPilot(def, false);
                }
            }
            
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "DismissPilot", new Type[] {typeof(Pilot)})]
    public static class SimGameState_DismissPilot
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, Pilot p)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            if (p != null)
            {
                PilotDef def = p.pilotDef;
                if (def != null)
                {
                    PilotQuirkManager.Instance.ResetArgoCostCache();
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
        
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, bool refund)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);

            float multiplier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.PilotRoster.rootList,
                shipModuleUpgrade.Description.Id, false);
            
            if (refund)
            {
                originalCost = shipModuleUpgrade.PurchaseCost;
                shipModuleUpgrade.PurchaseCost = (int)(originalCost * multiplier);
            }
        }
        public static void Postfix(SimGameState __instance, bool refund)
        {
            if (refund)
            {
                ShipModuleUpgrade shipModuleUpgrade = __instance.DataManager.ShipUpgradeDefs.Get(__instance.CurrentUpgradeEntry.upgradeID);
                shipModuleUpgrade.PurchaseCost = originalCost;
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
        
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, EconomyScale expenditureLevel, bool proRate, ref int __result)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            FinancesConstantsDef finances = __instance.Constants.Finances;
            int baseMaintenanceCost = __instance.GetShipBaseMaintenanceCost();
            for (int index = 0; index < __instance.ShipUpgrades.Count; ++index)
            {
                float pilotQurikModifier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.PilotRoster.rootList,
                        __instance.ShipUpgrades[index].Description.Id, true);
                float baseCost = (float) __instance.ShipUpgrades[index].AdditionalCost * pilotQurikModifier;
                baseMaintenanceCost += Mathf.CeilToInt(baseCost * __instance.Constants.CareerMode.ArgoMaintenanceMultiplier);
            }
            foreach (MechDef mechDef in __instance.ActiveMechs.Values)
                baseMaintenanceCost += finances.MechCostPerQuarter;
            for (int index = 0; index < __instance.PilotRoster.Count; ++index)
                baseMaintenanceCost += __instance.GetMechWarriorValue(__instance.PilotRoster[index].pilotDef);
            float expenditureCostModifier = __instance.GetExpenditureCostModifier(expenditureLevel);
            __result = Mathf.CeilToInt((float) (baseMaintenanceCost - (proRate ? __instance.ProRateRefund : 0)) * expenditureCostModifier);
            __runOriginal = false;
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
    public static class SimGameState_OnNewQuarterBegin
    {
        public static bool Prepare()
        {
            return Main.settings.enableMonthlyMoraleReset || Main.settings.enableMonthlyTechAdjustments;
        }
        public static void Postfix(SimGameState __instance)
        {
            if (Main.settings.enableMonthlyMoraleReset) PilotQuirkManager.Instance.resetMorale(__instance);
            if (Main.settings.enableMonthlyTechAdjustments) MonthlyTechAdjustmentManager.Instance.resetTechLevels();
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "AddMorale")]
    public static class SimGameState_AddMorale
    {
        public static bool Prepare()
        {
            return Main.settings.enableMonthlyMoraleReset;
        }
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, int val, string sourceID)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
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
            __runOriginal = false;
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "SetExpenditureLevel")]
    public static class SimGameState_SetExpenditureLevel
    {
        public static bool Prepare()
        {
            return Main.settings.enableMonthlyTechAdjustments;
        }
        public static void Postfix(SimGameState __instance, EconomyScale value, bool updateMorale)
        {
            if (updateMorale)
            {
                MonthlyTechAdjustmentManager.Instance.adjustTechLevels(value);
                __instance.RoomManager.RefreshDisplay();
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "GetPilotRoninIcon")]
    class SimGameState_GetPilotRoninIcon
    {
        public static void Postfix(SimGameState __instance, Pilot p, ref SVGAsset __result)
        {
            PilotIcon pilotIcon = PilotUiManager.Instance.GetPilotIcon(p);
            if (pilotIcon != null && pilotIcon.HasIcon())
            {
                __result = PilotUiManager.Instance.GetSvgAsset(pilotIcon.svgAssetId);
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "SetupRoninTooltip")]
    class SimGameState_SetupRoninTooltip
    {
        public static void Postfix(SimGameState __instance, HBSTooltip RoninTooltip, Pilot pilot)
        {
            PilotIcon pilotIcon = PilotUiManager.Instance.GetPilotIcon(pilot);
            if (pilotIcon != null && pilotIcon.HasDescription())
            {
                BaseDescriptionDef def = PilotUiManager.Instance.GetDescriptionDef(pilotIcon.descriptionDefId);
                if (def != null) RoninTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject((object)def));
            }
        }
    }
    
    [HarmonyPatch(typeof(SimGameState), "ApplySimGameEventResult", new Type[] {typeof(SimGameEventResult), typeof(List<object>), typeof(SimGameEventTracker)})]
    public static class SimGameState_ApplySimGameEventResult
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        public static void Prefix(ref bool __runOriginal, SimGameState __instance, SimGameEventResult result, List<object> objects)
        {
            
            if (!__runOriginal)
            {
                return;
            }
            
            SimGameState simulation = SceneSingletonBehavior<UnityGameInstance>.Instance.Game.Simulation;
            SimGameReport.ReportEntry log = (SimGameReport.ReportEntry) null;
            for (var i = 0; i < objects.Count; i++)
            {
                Pilot target = null;
                TagSet tagSet;
                switch (result.Scope)
                {
                    case EventScope.MechWarrior:
                    case EventScope.AllMechWarriors:
                    case EventScope.SecondaryMechWarrior:
                    case EventScope.TertiaryMechWarrior:
                        target = (Pilot) objects[i];
                        tagSet = target.pilotDef.PilotTags;
                        break;
                    case EventScope.Commander:
                        target = simulation.Commander;
                        tagSet = simulation.CommanderTags;
                        break;
                    default:
                        continue;
                }

                if (result.Requirements == null || simulation.MeetsRequirements(result.Requirements, log))
                {
                    if (result.AddedTags != null)
                    {
                        foreach (string addedTag in result.AddedTags)
                        {
                            if (!tagSet.Contains(addedTag))
                            {
                                PilotQuirkManager.Instance.ResetArgoCostCache();
                                PilotQuirkManager.Instance.processTagChange(target, addedTag, true);
                            }
                        }
                    }

                    if (result.RemovedTags != null)
                    {
                        foreach (string removedTag in result.RemovedTags)
                        {
                            if (tagSet.Contains(removedTag))
                            {
                                PilotQuirkManager.Instance.ResetArgoCostCache();
                                PilotQuirkManager.Instance.processTagChange(target, removedTag, false);
                            }
                            
                        }
                    }
                }

            }
        }
    }

}
