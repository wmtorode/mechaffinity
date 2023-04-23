using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using UnityEngine.UI;
using BattleTech.UI.TMProWrapper;
using Localize;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData", new Type[] {typeof(EconomyScale), typeof(bool)})]
    public static class SGCaptainsQuartersStatusScreen_RefreshData
    {
      public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks || Main.settings.enableMonthlyTechAdjustments;
        }
        
        public static void Prefix(ref bool __runOriginal, SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange)
        {
          
          if (!__runOriginal)
          {
            return;
          }
          
          if (__instance == null || __instance.simState == null || !Main.settings.enablePilotQuirks)
          {
            return;
          }
          float expenditureCostModifier = __instance.simState.GetExpenditureCostModifier(expenditureLevel);
          int expLevel = __instance.GetExpendetureLevelIndexNormalized(expenditureLevel);
          __instance.ExpenditureLevelIndicatorWidget.SetDifficulty(expLevel * 2);
          __instance.SetField(__instance.ExpenditureLevelField, string.Format("{0}", expenditureLevel));
          __instance.SetField(__instance.SectionOneExpenseLevel, string.Format("{0}", expenditureLevel));
          __instance.SetField(__instance.SectionTwoExpenseLevel, string.Format("{0}", expenditureLevel));
          __instance.FinanceWidget.RefreshData(expenditureLevel);
          int num1 = __instance.simState.ExpenditureMoraleValue[expenditureLevel];
          if (Main.settings.enableMonthlyTechAdjustments)
          {
            int medExpAdjust, mechExpAdjust;
            MonthlyTechAdjustmentManager.Instance.getTechAdjustments(expenditureLevel, out mechExpAdjust, out medExpAdjust);
            string moraleText = $"{num1}, {mechExpAdjust}/{medExpAdjust} Techs";
            __instance.MoraleValueField.fontSize = Main.settings.monthlyTechSettings.UiFontSize;
            __instance.SetField(__instance.MoraleValueField, moraleText);
            Main.modLog.Debug?.Write($"Font: {__instance.MoraleValueField.fontSize}");
              
          }
          else
          {
            __instance.SetField(__instance.MoraleValueField, string.Format("{0}{1}", num1 > 0 ? "+" : "", num1));
          }
            
          if (showMoraleChange)
          {
            int morale = __instance.simState.Morale;
            __instance.MoralBar.ShowMoraleChange(morale, morale + num1);
          }
          else
            __instance.MoralBar.ShowCurrentMorale();
          __instance.ClearListLineItems(__instance.SectionOneExpensesList);
          List<KeyValuePair<string, int>> keyValuePairList = new List<KeyValuePair<string, int>>();
          int ongoingUpgradeCosts = 0;
          string key = __instance.simState.CurDropship == DropshipType.Leopard ? Strings.T("Bank Loan Interest Payment") : Strings.T("Argo Operating Costs");
          int num2 = Mathf.RoundToInt(expenditureCostModifier * (float) __instance.simState.GetShipBaseMaintenanceCost());
          keyValuePairList.Add(new KeyValuePair<string, int>(key, num2));
          foreach (ShipModuleUpgrade shipUpgrade in __instance.simState.ShipUpgrades)
          {
            float pilotQurikModifier = PilotQuirkManager.Instance.getArgoUpgradeCostModifier(__instance.simState.PilotRoster.rootList,
              shipUpgrade.Description.Id, true);
            float baseCost = (float) shipUpgrade.AdditionalCost * pilotQurikModifier;
            if (__instance.simState.CurDropship == DropshipType.Argo && Mathf.CeilToInt((float) baseCost * __instance.simState.Constants.CareerMode.ArgoMaintenanceMultiplier) > 0)
            {
              string name = shipUpgrade.Description.Name;
              int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) Mathf.CeilToInt((float) baseCost * __instance.simState.Constants.CareerMode.ArgoMaintenanceMultiplier));
              keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
            }
          }
          foreach (MechDef mechDef in __instance.simState.ActiveMechs.Values)
          {
            string name = mechDef.Name;
            int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) __instance.simState.Constants.Finances.MechCostPerQuarter);
            keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
          }
          keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>) ((a, b) => b.Value.CompareTo(a.Value)));
          keyValuePairList.ForEach((Action<KeyValuePair<string, int>>) (entry =>
          {
            ongoingUpgradeCosts += entry.Value;
            __instance.AddListLineItem(__instance.SectionOneExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value));
          }));
          __instance.SetField(__instance.SectionOneExpensesField, SimGameState.GetCBillString(ongoingUpgradeCosts));
          keyValuePairList.Clear();
          __instance.ClearListLineItems(__instance.SectionTwoExpensesList);
          int ongoingMechWariorCosts = 0;
          foreach (Pilot pilot in __instance.simState.PilotRoster)
          {
            string displayName = pilot.pilotDef.Description.DisplayName;
            int num3 = Mathf.CeilToInt(expenditureCostModifier * (float) __instance.simState.GetMechWarriorValue(pilot.pilotDef));
            keyValuePairList.Add(new KeyValuePair<string, int>(displayName, num3));
          }
          keyValuePairList.Sort( (a, b) => b.Value.CompareTo(a.Value));
          keyValuePairList.ForEach(entry =>
          {
            ongoingMechWariorCosts += entry.Value;
            __instance.AddListLineItem(__instance.SectionTwoExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value));
          });
          __instance.SetField(__instance.SectionTwoExpensesField, SimGameState.GetCBillString(ongoingMechWariorCosts));
          __instance.SetField(__instance.EndOfQuarterFunds, SimGameState.GetCBillString(__instance.simState.Funds + __instance.simState.GetExpenditures(false)));
          __instance.SetField(__instance.QuarterOperatingExpenses, SimGameState.GetCBillString(__instance.simState.GetExpenditures(false)));
          __instance.SetField(__instance.CurrentFunds, SimGameState.GetCBillString(__instance.simState.Funds));
          int medAdjust, mechAdjust, index = 0;
          string newText;
          foreach (KeyValuePair<EconomyScale, int> keyValuePair in __instance.simState.ExpenditureMoraleValue)
          {
            if (Main.settings.enableMonthlyTechAdjustments)
            {
              MonthlyTechAdjustmentManager.Instance.getTechAdjustments(keyValuePair.Key, out mechAdjust, out medAdjust);
              newText = $"{keyValuePair.Value}, {mechAdjust}/{medAdjust} Techs";
              __instance.ExpenditureLvlBtnMoraleFields[index].fontSize = Main.settings.monthlyTechSettings.UiFontSize;
              __instance.ExpenditureLvlBtnMoraleFields[index].SetText(newText, Array.Empty<object>());
                
            }
            else
            {
              __instance.ExpenditureLvlBtnMoraleFields[index].SetText(string.Format("{0}", keyValuePair.Value), Array.Empty<object>());
            }
              
            __instance.ExpenditureLvlBtnCostFields[index].SetText(SimGameState.GetCBillString(__instance.simState.GetExpenditures(keyValuePair.Key, false)),  Array.Empty<object>());
            ++index;
          }

          __runOriginal = false;
        }
      
    }
}