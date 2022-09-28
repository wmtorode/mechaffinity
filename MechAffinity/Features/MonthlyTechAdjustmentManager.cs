using BattleTech;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using MechAffinity.Data;
using MechAffinity.Data.MonthlyTech;
using Newtonsoft.Json;
using System;

namespace MechAffinity
{
    public class MonthlyTechAdjustmentManager: BaseEffectManager
    {
        private const string MechTechModifier = "MA_MT_MechTechMod";
        private const string MedTechModifier = "MA_MT_MedTechSkill";
        private const string MTMASaveTagPrefix = "MTMA{";
        private const string ExpenseLevel = "ExpenseLevel";
        private const string MechTechSkill = "MechTechSkill";
        private const string MedTechSkill = "MedTechSkill";
        private static MonthlyTechAdjustmentManager _instance;
        private StatCollection companyStats;
        private MonthlyTechSettings settings;

        public static MonthlyTechAdjustmentManager Instance
        {
            get
            {
                if (_instance == null) _instance = new MonthlyTechAdjustmentManager();
                if (!_instance.hasInitialized) _instance.initialize(Main.settings.monthlyTechSettings);
                return _instance;
            }
        }

        public void initialize(MonthlyTechSettings monthlyTechSettings)
        {
            if (hasInitialized) return;
            settings = monthlyTechSettings;
            UidManager.reset();
            hasInitialized = true;
        }

        public void setCompanyStats(StatCollection stats, SimGameState sim)
        {
            companyStats = stats;

            if (!companyStats.ContainsStatistic(MechTechModifier))
            {
                companyStats.AddStatistic<int>(MechTechModifier, 0);
            }

            if (!companyStats.ContainsStatistic(MedTechModifier))
            {
                companyStats.AddStatistic<int>(MedTechModifier, 0);
            }
            
            if (sim.CompanyTags.Any(x => x.StartsWith(MTMASaveTagPrefix)))
            {
                LegacyMtmaSave legacyMtmaSave = JsonConvert.DeserializeObject<LegacyMtmaSave>(sim.CompanyTags.First(x => x.StartsWith(MTMASaveTagPrefix)).Substring(4));
                Main.modLog.LogMessage($"loaded MTMA legacy data, importing....");
                companyStats.Set<int>(MechTechModifier, legacyMtmaSave.DeltaMechTech);
                companyStats.Set<int>(MedTechModifier, legacyMtmaSave.DeltaMedTech);
                Main.modLog.LogMessage($"Imported: {legacyMtmaSave.ExpenseLevel} - {legacyMtmaSave.DeltaMechTech}/{legacyMtmaSave.DeltaMedTech}");
                sim.CompanyTags.Where(tag => tag.StartsWith(MTMASaveTagPrefix)).Do(x => sim.CompanyTags.Remove(x));
            }

            Main.modLog.LogMessage(
                $"MT Stat: {MechTechModifier}, value: {companyStats.GetValue<int>(MechTechModifier)}");
            Main.modLog.LogMessage(
                $"MT Stat: {MedTechModifier}, value: {companyStats.GetValue<int>(MedTechModifier)}");
        }

        public void resetTechLevels()
        {
            if (companyStats is null) return;
            int mechTech = companyStats.GetValue<int>(MechTechSkill) + companyStats.GetValue<int>(MechTechModifier);
            int medTech = companyStats.GetValue<int>(MedTechSkill) + companyStats.GetValue<int>(MedTechModifier);

            companyStats.Set<int>(MechTechSkill, mechTech);
            companyStats.Set<int>(MedTechSkill, medTech);
            Main.modLog.LogMessage($"Reset Mech/MedTech: {mechTech}/{medTech}");
        }

        public void getTechAdjustments(EconomyScale economyScale, out int mechTechAdjust, out int medTechAdjust)
        {
            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    mechTechAdjust = settings.SpartanMechModifier;
                    medTechAdjust = settings.SpartanMedModifier;
                    break;
                case EconomyScale.Restrictive:
                    mechTechAdjust = settings.RestrictedMechModifier;
                    medTechAdjust = settings.RestrictedMedModifier;
                    break;
                case EconomyScale.Normal:
                    mechTechAdjust = settings.NormalMechModifier;
                    medTechAdjust = settings.NormalMedModifier;
                    break;
                case EconomyScale.Generous:
                    mechTechAdjust = settings.GenerousMechModifier;
                    medTechAdjust = settings.GenerousMedModifier;
                    break;
                case EconomyScale.Extravagant:
                    mechTechAdjust = settings.ExtravagantMechModifier;
                    medTechAdjust = settings.ExtravagantMedModifier;
                    break;
                default:
                    mechTechAdjust = 0;
                    medTechAdjust = 0;
                    break;
            }
        }

        public void adjustTechLevels(EconomyScale economyScale)
        {
            if (companyStats is null) return;
            int medTechAdjust = 0;
            int mechTechAdjust = 0;

            getTechAdjustments(economyScale, out mechTechAdjust, out medTechAdjust);

            int currentMechTech = companyStats.GetValue<int>(MechTechSkill);
            int currentMedTech = companyStats.GetValue<int>(MedTechSkill);

            int newMechTech = Math.Max(currentMechTech + mechTechAdjust, 1);
            int newMedTech = Math.Max(currentMedTech + medTechAdjust, 1);

            companyStats.Set<int>(MechTechSkill, newMechTech);
            companyStats.Set<int>(MechTechModifier, currentMechTech - newMechTech);
            companyStats.Set<int>(MedTechSkill, newMedTech);
            companyStats.Set<int>(MedTechModifier, currentMedTech - newMedTech);
            Main.modLog.LogMessage($"Adjusting tech on expend: Mech: M:{mechTechAdjust}, C:{currentMechTech}, N: {newMechTech} S: {currentMechTech - newMechTech}");
            Main.modLog.LogMessage($"Adjusting tech on expend: Med: M:{medTechAdjust}, C:{currentMedTech}, N: {newMedTech} S: {currentMedTech - newMedTech}");
        }
    }
}