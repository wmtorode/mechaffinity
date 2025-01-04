using BattleTech;
using System.Collections.Generic;
using System.Linq;
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
        
        private const string MechTechMultiplierPrefix = "MT_MechTechMultiplier_";
        private const string MedTechMultiplierPrefix = "MT_MedTechlMultiplier_";
        private const string MechTechFlatModifierPrefix = "MT_MechTechFlatModifier_";
        private const string MedTechFlatModifierPrefix = "MT_MedTechFlatModifier_";
        
        
        private static MonthlyTechAdjustmentManager _instance;
        private StatCollection companyStats;
        private MonthlyTechSettings settings;

        public static MonthlyTechAdjustmentManager Instance
        {
            get
            {
                if (_instance == null) _instance = new MonthlyTechAdjustmentManager();
                if (!_instance.hasInitialized) _instance.Initialize(Main.settings.monthlyTechSettings);
                return _instance;
            }
        }

        private void Initialize(MonthlyTechSettings monthlyTechSettings)
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

            List<EconomyScale?> economyScales = new List<EconomyScale?>()
            {
                EconomyScale.Spartan,
                EconomyScale.Restrictive,
                EconomyScale.Normal,
                EconomyScale.Generous,
                EconomyScale.Extravagant
            };

            foreach (EconomyScale? economyScale in economyScales)
            {
                if (!companyStats.ContainsStatistic(GetMechTechMultiplierStatId(economyScale)))
                {
                    companyStats.AddStatistic(GetMechTechMultiplierStatId(economyScale), GetMechTechDefaultMultiplier(economyScale));
                }
                
                if (!companyStats.ContainsStatistic(GetMechTechFlatStatId(economyScale)))
                {
                    companyStats.AddStatistic(GetMechTechFlatStatId(economyScale), GetMechTechDefaultModifier(economyScale));
                }
                
                if (!companyStats.ContainsStatistic(GetMedTechMultiplierStatId(economyScale)))
                {
                    companyStats.AddStatistic(GetMedTechMultiplierStatId(economyScale), GetMedTechDefaultMultiplier(economyScale));
                }
                
                if (!companyStats.ContainsStatistic(GetMedTechFlatStatId(economyScale)))
                {
                    companyStats.AddStatistic(GetMedTechFlatStatId(economyScale), GetMedTechDefaultModifier(economyScale));
                }
                
            }
            
            if (sim.CompanyTags.Any(x => x.StartsWith(MTMASaveTagPrefix)))
            {
                LegacyMtmaSave legacyMtmaSave = JsonConvert.DeserializeObject<LegacyMtmaSave>(sim.CompanyTags.First(x => x.StartsWith(MTMASaveTagPrefix)).Substring(4));
                Main.modLog.Info?.Write($"loaded MTMA legacy data, importing....");
                companyStats.Set<int>(MechTechModifier, legacyMtmaSave.DeltaMechTech);
                companyStats.Set<int>(MedTechModifier, legacyMtmaSave.DeltaMedTech);
                Main.modLog.Info?.Write($"Imported: {legacyMtmaSave.ExpenseLevel} - {legacyMtmaSave.DeltaMechTech}/{legacyMtmaSave.DeltaMedTech}");
                sim.CompanyTags.Where(tag => tag.StartsWith(MTMASaveTagPrefix)).Do(x => sim.CompanyTags.Remove(x));
            }

            Main.modLog.Info?.Write(
                $"MT Stat: {MechTechModifier}, value: {companyStats.GetValue<int>(MechTechModifier)}");
            Main.modLog.Info?.Write(
                $"MT Stat: {MedTechModifier}, value: {companyStats.GetValue<int>(MedTechModifier)}");
        }

        public void resetTechLevels()
        {
            if (companyStats is null) return;
            int mechTech = companyStats.GetValue<int>(MechTechSkill) + companyStats.GetValue<int>(MechTechModifier);
            int medTech = companyStats.GetValue<int>(MedTechSkill) + companyStats.GetValue<int>(MedTechModifier);

            companyStats.Set<int>(MechTechSkill, mechTech);
            companyStats.Set<int>(MedTechSkill, medTech);
            companyStats.Set<int>(MechTechModifier, 0);
            companyStats.Set<int>(MedTechModifier, 0);
            Main.modLog.Info?.Write($"Reset Mech/MedTech: {mechTech}/{medTech}");
        }

        public void getTechAdjustments(EconomyScale economyScale, out int mechTechAdjust, out int medTechAdjust)
        {

            var mechTechbase = 0;
            var medTechbase = 0;
            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    mechTechbase = settings.SpartanMechModifier;
                    medTechbase = settings.SpartanMedModifier;
                    break;
                case EconomyScale.Restrictive:
                    mechTechbase = settings.RestrictedMechModifier;
                    medTechbase = settings.RestrictedMedModifier;
                    break;
                case EconomyScale.Normal:
                    mechTechbase = settings.NormalMechModifier;
                    medTechbase = settings.NormalMedModifier;
                    break;
                case EconomyScale.Generous:
                    mechTechbase = settings.GenerousMechModifier;
                    medTechbase = settings.GenerousMedModifier;
                    break;
                case EconomyScale.Extravagant:
                    mechTechbase = settings.ExtravagantMechModifier;
                    medTechbase = settings.ExtravagantMedModifier;
                    break;
                
            }

            if (settings.UseEnhancedFormulas)
            {
                mechTechAdjust = GetMechTechAdjustment(economyScale);
                medTechAdjust = GetMedTechAdjustment(economyScale);
            }
            else
            {
                mechTechAdjust = mechTechbase;
                medTechAdjust = medTechbase;
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
            Main.modLog.Info?.Write($"Adjusting tech on expend: Mech: M:{mechTechAdjust}, C:{currentMechTech}, N: {newMechTech} S: {currentMechTech - newMechTech}");
            Main.modLog.Info?.Write($"Adjusting tech on expend: Med: M:{medTechAdjust}, C:{currentMedTech}, N: {newMedTech} S: {currentMedTech - newMedTech}");
        }
        
        private int GetMechTechAdjustment(EconomyScale economyScale)
        {
            
            var multiplier = companyStats.GetValue<float>(GetMechTechMultiplierStatId(economyScale));
            var modifier = companyStats.GetValue<int>(GetMechTechFlatStatId(economyScale));
            
            var currentMechTech = companyStats.GetValue<int>(MechTechSkill) + companyStats.GetValue<int>(MechTechModifier);
            
            if (settings.ModifiersFirst)
                return (int)Math.Ceiling((currentMechTech + modifier) * multiplier);

            return (int)Math.Ceiling((currentMechTech * multiplier) + modifier);
            
            
        }
        
        private int GetMedTechAdjustment(EconomyScale economyScale)
        {
            
            var multiplier = companyStats.GetValue<float>(GetMedTechMultiplierStatId(economyScale));
            var modifier = companyStats.GetValue<int>(GetMedTechFlatStatId(economyScale));
            
            var currentMedTech = companyStats.GetValue<int>(MedTechSkill) + companyStats.GetValue<int>(MedTechModifier);
            
            if (settings.ModifiersFirst)
                return (int)Math.Ceiling((currentMedTech + modifier) * multiplier);

            return (int)Math.Ceiling((currentMedTech * multiplier) + modifier);
            
        }

        private string GetMedTechMultiplierStatId(EconomyScale? economyScale)
        {
            if (economyScale == null) return $"{MedTechMultiplierPrefix}Global";
            
            return $"{MedTechMultiplierPrefix}{economyScale.Value.ToString()}";
        }
        
        private string GetMedTechFlatStatId(EconomyScale? economyScale)
        {
            if (economyScale == null) return $"{MedTechFlatModifierPrefix}Global";
            
            return $"{MedTechFlatModifierPrefix}{economyScale.Value.ToString()}";
        }
        
        private string GetMechTechMultiplierStatId(EconomyScale? economyScale)
        {
            if (economyScale == null) return $"{MechTechMultiplierPrefix}Global";
            
            return $"{MechTechMultiplierPrefix}{economyScale.Value.ToString()}";
        }
        
        private string GetMechTechFlatStatId(EconomyScale? economyScale)
        {
            if (economyScale == null) return $"{MechTechFlatModifierPrefix}Global";
            
            return $"{MechTechFlatModifierPrefix}{economyScale.Value.ToString()}";
        }

        private int GetMechTechDefaultModifier(EconomyScale? economyScale)
        {
            if (economyScale == null) return settings.DefaultMechTechModifier;
            
            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    return settings.SpartanMechModifier;
                case EconomyScale.Restrictive:
                    return settings.RestrictedMechModifier;
                case EconomyScale.Normal:
                    return settings.NormalMechModifier;
                case EconomyScale.Generous:
                    return settings.GenerousMechModifier;
                case EconomyScale.Extravagant:
                    return settings.ExtravagantMechModifier;
                default:
                    return settings.DefaultMechTechModifier;
            }
        }
        
        private int GetMedTechDefaultModifier(EconomyScale? economyScale)
        {
            if (economyScale == null) return settings.DefaultMedTechModifier;

            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    return settings.SpartanMedModifier;
                case EconomyScale.Restrictive:
                    return settings.RestrictedMedModifier;
                case EconomyScale.Normal:
                    return settings.NormalMedModifier;
                case EconomyScale.Generous:
                    return settings.GenerousMedModifier;
                case EconomyScale.Extravagant:
                    return settings.ExtravagantMedModifier;
                default:
                    return settings.DefaultMedTechModifier;
            }
        }

        private float GetMechTechDefaultMultiplier(EconomyScale? economyScale)
        {
            if (economyScale == null) return settings.DefaultMechTechMultiplier;

            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    return settings.SpartanMechMultiplier;
                case EconomyScale.Restrictive:
                    return settings.RestrictedMechMultiplier;
                case EconomyScale.Normal:
                    return settings.NormalMechMultiplier;
                case EconomyScale.Generous:
                    return settings.GenerousMechMultiplier;
                case EconomyScale.Extravagant:
                    return settings.ExtravagantMechMultiplier;
                default:
                    return settings.DefaultMechTechMultiplier;
            }
        }

        private float GetMedTechDefaultMultiplier(EconomyScale? economyScale)
        {
            if (economyScale == null) return settings.DefaultMedTechMultiplier;

            switch (economyScale)
            {
                case EconomyScale.Spartan:
                    return settings.SpartanMedMultiplier;
                case EconomyScale.Restrictive:
                    return settings.RestrictedMedMultiplier;
                case EconomyScale.Normal:
                    return settings.NormalMedMultiplier;
                case EconomyScale.Generous:
                    return settings.GenerousMedMultiplier;
                case EconomyScale.Extravagant:
                    return settings.ExtravagantMedMultiplier;
                default:
                    return settings.DefaultMedTechMultiplier;
            }
        }
    }
}