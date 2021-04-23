﻿using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;
using UnityEngine;
using UnityEngine.UI;
using BattleTech.UI.TMProWrapper;
using Localize;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData", new Type[] {typeof(EconomyScale), typeof(bool)})]
    public static class SGCaptainsQuartersStatusScreen_RefreshData
    {
        private static MethodInfo methodAddLineItem = AccessTools.Method(typeof(SGCaptainsQuartersStatusScreen), "AddListLineItem");
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
        
        public static bool Prefix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange, SimGameState ___simState,
          SGDifficultyIndicatorWidget ___ExpenditureLevelIndicatorWidget, LocalizableText ___ExpenditureLevelField, LocalizableText ___SectionOneExpenseLevel,
          LocalizableText ___SectionTwoExpenseLevel, SGFinancialForecastWidget ___FinanceWidget, LocalizableText ___MoraleValueField, SGMoraleBar ___MoralBar,
          Transform ___SectionOneExpensesList, LocalizableText ___SectionOneExpensesField, LocalizableText ___SectionTwoExpensesField, 
          Transform ___SectionTwoExpensesList, LocalizableText ___EndOfQuarterFunds, LocalizableText ___QuarterOperatingExpenses, 
          LocalizableText ___CurrentFunds, List<LocalizableText> ___ExpenditureLvlBtnMoraleFields, List<LocalizableText> ___ExpenditureLvlBtnCostFields)
        {
          if (__instance == null || ___simState == null)
          {
            return true;
          }
            float expenditureCostModifier = ___simState.GetExpenditureCostModifier(expenditureLevel);
            Traverse methodSetField = Traverse.Create(__instance)
              .Method("SetField", new Type[] {typeof(LocalizableText), typeof(string)});
            int expLevel = (int)Traverse.Create(__instance)
              .Method("GetExpendetureLevelIndexNormalized", new object[] {expenditureLevel}).GetValue();
            ___ExpenditureLevelIndicatorWidget.SetDifficulty(expLevel * 2);
            methodSetField.GetValue(new object[] {___ExpenditureLevelField, string.Format("{0}", (object) expenditureLevel)});
            methodSetField.GetValue(new object[] {___SectionOneExpenseLevel, string.Format("{0}", (object) expenditureLevel)});
            methodSetField.GetValue(new object[] {___SectionTwoExpenseLevel, string.Format("{0}", (object) expenditureLevel)});
            ___FinanceWidget.RefreshData(expenditureLevel);
            int num1 = ___simState.ExpenditureMoraleValue[expenditureLevel];
            methodSetField.GetValue(new object[] {___MoraleValueField, string.Format("{0}{1}", num1 > 0 ? (object) "+" : (object) "", (object) num1)});
            if (showMoraleChange)
            {
              int morale = ___simState.Morale;
              ___MoralBar.ShowMoraleChange(morale, morale + num1);
            }
            else
              ___MoralBar.ShowCurrentMorale();
            Traverse.Create(__instance).Method("ClearListLineItems",new object[] {___SectionOneExpensesList}).GetValue();
            List<KeyValuePair<string, int>> keyValuePairList = new List<KeyValuePair<string, int>>();
            int ongoingUpgradeCosts = 0;
            string key = ___simState.CurDropship == DropshipType.Leopard ? Strings.T("Bank Loan Interest Payment") : Strings.T("Argo Operating Costs");
            int num2 = Mathf.RoundToInt(expenditureCostModifier * (float) ___simState.GetShipBaseMaintenanceCost());
            keyValuePairList.Add(new KeyValuePair<string, int>(key, num2));
            foreach (ShipModuleUpgrade shipUpgrade in ___simState.ShipUpgrades)
            {
                float baseCost = shipUpgrade.AdditionalCost;
                if (Main.settings.enablePilotQuirks)
                {
                    float pilotQurikModifier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(___simState.PilotRoster.ToList(), shipUpgrade.Description.Id, true);											  
                    baseCost = (float)shipUpgrade.AdditionalCost * pilotQurikModifier;
                }
                if (___simState.CurDropship == DropshipType.Argo && Mathf.CeilToInt((float) baseCost * ___simState.Constants.CareerMode.ArgoMaintenanceMultiplier) > 0)
                {
                    string name = shipUpgrade.Description.Name;
                    int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) Mathf.CeilToInt((float) baseCost * ___simState.Constants.CareerMode.ArgoMaintenanceMultiplier));
                    keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
                }
            }
            foreach (MechDef mechDef in ___simState.ActiveMechs.Values)
            {
                string name = mechDef.Name;
                int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) ___simState.Constants.Finances.MechCostPerQuarter);
                if(Main.settings.MechMaintenanceByCost)
                {
                    if (Main.settings.MMBC_CostByTons)
                    {
                        num3 = Mathf.RoundToInt(expenditureCostModifier * (float)mechDef.Chassis.Tonnage * Main.settings.MMBC_cbillsPerTon);
                        if (Main.settings.MMBC_TonsAdditive)
                            num3 += Mathf.RoundToInt(expenditureCostModifier * CalculateCBillValue(mechDef) * Main.settings.MMBC_PercentageOfMechCost);
                    }
                    else
                        num3 = Mathf.RoundToInt(expenditureCostModifier * CalculateCBillValue(mechDef) * Main.settings.MMBC_PercentageOfMechCost);
                }
                keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
            }
            keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>) ((a, b) => b.Value.CompareTo(a.Value)));
            keyValuePairList.ForEach((Action<KeyValuePair<string, int>>) (entry =>
            {
              ongoingUpgradeCosts += entry.Value;
              methodAddLineItem.Invoke(__instance,new object[] {___SectionOneExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value)});
            }));
            methodSetField.GetValue(new object[] {___SectionOneExpensesField, SimGameState.GetCBillString(ongoingUpgradeCosts)});
            keyValuePairList.Clear();
            Traverse.Create(__instance).Method("ClearListLineItems",new object[] {___SectionTwoExpensesList}).GetValue();
            int ongoingMechWariorCosts = 0;
            foreach (Pilot pilot in ___simState.PilotRoster)
            {
              string displayName = pilot.pilotDef.Description.DisplayName;
              int num3 = Mathf.CeilToInt(expenditureCostModifier * (float) ___simState.GetMechWarriorValue(pilot.pilotDef));
              keyValuePairList.Add(new KeyValuePair<string, int>(displayName, num3));
            }
            keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>) ((a, b) => b.Value.CompareTo(a.Value)));
            keyValuePairList.ForEach((Action<KeyValuePair<string, int>>) (entry =>
            {
              ongoingMechWariorCosts += entry.Value;
              methodAddLineItem.Invoke(__instance,new object[] {___SectionTwoExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value)});
            }));
            methodSetField.GetValue(new object[] {___SectionTwoExpensesField, SimGameState.GetCBillString(ongoingMechWariorCosts)});
            methodSetField.GetValue(new object[] {___EndOfQuarterFunds, SimGameState.GetCBillString(___simState.Funds + ___simState.GetExpenditures(false))});
            methodSetField.GetValue(new object[] {___QuarterOperatingExpenses, SimGameState.GetCBillString(___simState.GetExpenditures(false))});
            methodSetField.GetValue(new object[] {___CurrentFunds, SimGameState.GetCBillString(___simState.Funds)});
            int index = 0;
            foreach (KeyValuePair<EconomyScale, int> keyValuePair in ___simState.ExpenditureMoraleValue)
            {
              ___ExpenditureLvlBtnMoraleFields[index].SetText(string.Format("{0}", (object) keyValuePair.Value), (object[]) Array.Empty<object>());
              ___ExpenditureLvlBtnCostFields[index].SetText(SimGameState.GetCBillString(___simState.GetExpenditures(keyValuePair.Key, false)), (object[]) Array.Empty<object>());
              ++index;
            }

            return false;
        }
    }
}
