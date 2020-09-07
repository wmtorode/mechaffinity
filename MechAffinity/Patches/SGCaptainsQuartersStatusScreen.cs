using System;
using System.Reflection;
using BattleTech;
using BattleTech.UI;
using Harmony;
using UnityEngine;

namespace MechAffinity.Patches
{
    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData", new Type[] {typeof(EconomyScale), typeof(bool)})]
    public class SGCaptainsQuartersStatusScreen_RefreshData
    {
        public static bool Prepare()
        {
            return Main.settings.enablePilotQuirks;
        }
        
        public static bool Prefix(SGCaptainsQuartersStatusScreen __instance, EconomyScale expenditureLevel, bool showMoraleChange, SimGameState ___simState)
        {
            float expenditureCostModifier = ___simState.GetExpenditureCostModifier(expenditureLevel);
            this.ExpenditureLevelIndicatorWidget.SetDifficulty(__instance.GetExpendetureLevelIndexNormalized(expenditureLevel) * 2);
            this.SetField(this.ExpenditureLevelField, string.Format("{0}", (object) expenditureLevel));
            this.SetField(this.SectionOneExpenseLevel, string.Format("{0}", (object) expenditureLevel));
            this.SetField(this.SectionTwoExpenseLevel, string.Format("{0}", (object) expenditureLevel));
            this.FinanceWidget.RefreshData(expenditureLevel);
            int num1 = this.simState.ExpenditureMoraleValue[expenditureLevel];
            this.SetField(this.MoraleValueField, string.Format("{0}{1}", num1 > 0 ? (object) "+" : (object) "", (object) num1));
            if (showMoraleChange)
            {
              int morale = this.simState.Morale;
              this.MoralBar.ShowMoraleChange(morale, morale + num1);
            }
            else
              this.MoralBar.ShowCurrentMorale();
            this.ClearListLineItems(this.SectionOneExpensesList);
            List<KeyValuePair<string, int>> keyValuePairList = new List<KeyValuePair<string, int>>();
            int ongoingUpgradeCosts = 0;
            string key = this.simState.CurDropship == DropshipType.Leopard ? Strings.T("Bank Loan Interest Payment") : Strings.T("Argo Operating Costs");
            int num2 = Mathf.RoundToInt(expenditureCostModifier * (float) this.simState.GetShipBaseMaintenanceCost());
            keyValuePairList.Add(new KeyValuePair<string, int>(key, num2));
            foreach (ShipModuleUpgrade shipUpgrade in this.simState.ShipUpgrades)
            {
              if (this.simState.CurDropship == DropshipType.Argo && Mathf.CeilToInt((float) shipUpgrade.AdditionalCost * this.simState.Constants.CareerMode.ArgoMaintenanceMultiplier) > 0)
              {
                string name = shipUpgrade.Description.Name;
                int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) Mathf.CeilToInt((float) shipUpgrade.AdditionalCost * this.simState.Constants.CareerMode.ArgoMaintenanceMultiplier));
                keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
              }
            }
            foreach (MechDef mechDef in this.simState.ActiveMechs.Values)
            {
              string name = mechDef.Name;
              int num3 = Mathf.RoundToInt(expenditureCostModifier * (float) this.simState.Constants.Finances.MechCostPerQuarter);
              keyValuePairList.Add(new KeyValuePair<string, int>(name, num3));
            }
            keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>) ((a, b) => b.Value.CompareTo(a.Value)));
            keyValuePairList.ForEach((Action<KeyValuePair<string, int>>) (entry =>
            {
              ongoingUpgradeCosts += entry.Value;
              this.AddListLineItem(this.SectionOneExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value));
            }));
            this.SetField(this.SectionOneExpensesField, SimGameState.GetCBillString(ongoingUpgradeCosts));
            keyValuePairList.Clear();
            this.ClearListLineItems(this.SectionTwoExpensesList);
            int ongoingMechWariorCosts = 0;
            foreach (Pilot pilot in this.simState.PilotRoster)
            {
              string displayName = pilot.pilotDef.Description.DisplayName;
              int num3 = Mathf.CeilToInt(expenditureCostModifier * (float) this.simState.GetMechWarriorValue(pilot.pilotDef));
              keyValuePairList.Add(new KeyValuePair<string, int>(displayName, num3));
            }
            keyValuePairList.Sort((Comparison<KeyValuePair<string, int>>) ((a, b) => b.Value.CompareTo(a.Value)));
            keyValuePairList.ForEach((Action<KeyValuePair<string, int>>) (entry =>
            {
              ongoingMechWariorCosts += entry.Value;
              this.AddListLineItem(this.SectionTwoExpensesList, entry.Key, SimGameState.GetCBillString(entry.Value));
            }));
            this.SetField(this.SectionTwoExpensesField, SimGameState.GetCBillString(ongoingMechWariorCosts));
            this.SetField(this.EndOfQuarterFunds, SimGameState.GetCBillString(this.simState.Funds + this.simState.GetExpenditures(false)));
            this.SetField(this.QuarterOperatingExpenses, SimGameState.GetCBillString(this.simState.GetExpenditures(false)));
            this.SetField(this.CurrentFunds, SimGameState.GetCBillString(this.simState.Funds));
            int index = 0;
            foreach (KeyValuePair<EconomyScale, int> keyValuePair in this.simState.ExpenditureMoraleValue)
            {
              this.ExpenditureLvlBtnMoraleFields[index].SetText(string.Format("{0}", (object) keyValuePair.Value), (object[]) Array.Empty<object>());
              this.ExpenditureLvlBtnCostFields[index].SetText(SimGameState.GetCBillString(this.simState.GetExpenditures(keyValuePair.Key, false)), (object[]) Array.Empty<object>());
              ++index;
            }
        }
    }
}