using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using MechAffinity.Data;
using Newtonsoft.Json.Linq;
using Harmony;

namespace MechAffinity
{
    public class PilotQuirkManager : BaseEffectManager
    {
        private const string MechTechSkill = "MechTechSkill";
        private const string MedTechSkill = "MedTechSkill";
        private const string Morale = "Morale";
        private const string PqMechSkillTracker = "PqMechSkillTracker";
        private const string PqMedSkillTracker = "PqMedSkillTracker";
        private const string PqMoraleTracker = "PqMoraleTracker";
        private const string PqAllArgoUpgrades = "PqAllArgoUpgrades";
        private const string PqMarkedTag = "PqMarked";
        private const string PqMarkedPrefix = "PqTagged_";
        private const string PqMoraleModifierTracker = "PqMoraleModifierTracker";
        private static PilotQuirkManager _instance;
        private StatCollection companyStats;
        private Dictionary<string, PilotQuirk> quirks;
        private Dictionary<string, QuirkPool> quirkPools;
        private bool moraleModInstanced;

        public static PilotQuirkManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotQuirkManager();
                if (!_instance.hasInitialized) _instance.initialize();
                return _instance;
            }
        }

        public void initialize()
        {
            if(hasInitialized) return;
            UidManager.reset();
            moraleModInstanced = true;
            quirks = new Dictionary<string, PilotQuirk>();
            foreach (PilotQuirk pilotQuirk in Main.settings.pilotQuirks)
            {
                foreach (JObject jObject in pilotQuirk.effectData)
                {
                    EffectData effectData = new EffectData();
                    effectData.FromJSON(jObject.ToString());
                    pilotQuirk.effects.Add(effectData);
                }

                quirks.Add(pilotQuirk.tag, pilotQuirk);
            }
            quirkPools = new Dictionary<string, QuirkPool>();
            foreach (QuirkPool quirkPool in Main.settings.quirkPools)
            {
                quirkPools.Add(quirkPool.tag, quirkPool);
            }

            hasInitialized = true;
        }
        
        public void setCompanyStats(StatCollection stats)
        {
            companyStats = stats;
            
            if (!companyStats.ContainsStatistic(PqMechSkillTracker))
            {
                companyStats.AddStatistic<float>(PqMechSkillTracker, 0.0f);
            }
            if (!companyStats.ContainsStatistic(PqMedSkillTracker))
            {
                companyStats.AddStatistic<float>(PqMedSkillTracker, 0.0f);
            }
            if (!companyStats.ContainsStatistic(PqMoraleTracker))
            {
                companyStats.AddStatistic<float>(PqMoraleTracker, 0.0f);
            }
            if (!companyStats.ContainsStatistic(PqMoraleModifierTracker))
            {
                companyStats.AddStatistic<int>(PqMoraleModifierTracker, 0);
                moraleModInstanced = false;
            }
            Main.modLog.LogMessage($"Tracker Stat: {PqMechSkillTracker}, value: {companyStats.GetValue<float>(PqMechSkillTracker)}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMedSkillTracker}, value: {companyStats.GetValue<float>(PqMedSkillTracker)}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMoraleTracker}, value: {companyStats.GetValue<float>(PqMoraleTracker)}");
        }

        public void forceMoraleInstanced()
        {
            moraleModInstanced = true;
        }

        private List<string> getPooledQuirks(QuirkPool pool)
        {
            List<string> choosenQuirks = new List<string>();
            Random random = new Random();
            while (choosenQuirks.Count < pool.quirksToPick)
            {
                string quirk = pool.quirksAvailable[random.Next(pool.quirksAvailable.Count)];
                if (!choosenQuirks.Contains(quirk))
                {
                    choosenQuirks.Add(quirk);
                }
            }

            return choosenQuirks;
        }
        
        private List<PilotQuirk> getQuirks(PilotDef pilotDef, bool usePools = false)
        {
            List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
            if (pilotDef != null)
            {
                List<string> tags = pilotDef.PilotTags.ToList();
                List<string> usedQuirks = new List<string>();
                foreach (string tag in tags)
                {
                    //Main.modLog.LogMessage($"Processing tag: {tag}");
                    PilotQuirk quirk;
                    if (quirks.TryGetValue(tag, out quirk))
                    {
                        pilotQuirks.Add(quirk);
                        usedQuirks.Add(tag);
                    }
                }

                if (usePools)
                {
                    foreach (string tag in tags)
                    {
                        QuirkPool quirkpool;
                        if (quirkPools.TryGetValue(tag, out quirkpool))
                        {
                            List<string> choosenQuirks = getPooledQuirks(quirkpool);
                            foreach (string possibleQuirk in choosenQuirks)
                            {
                                PilotQuirk quirk;
                                if (quirks.TryGetValue(possibleQuirk, out quirk))
                                {
                                    if (!usedQuirks.Contains(possibleQuirk))
                                    {
                                        pilotQuirks.Add(quirk);
                                        usedQuirks.Add(possibleQuirk);
                                        Main.modLog.LogMessage($"Adding Randomized Quirk: {possibleQuirk}");
                                    }
                                    else
                                    {
                                        Main.modLog.LogMessage($"Skipped adding randomized quirk {possibleQuirk}, because it was already used");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return pilotQuirks;
        }

        private List<PilotQuirk> getQuirks(Pilot pilot, bool usePools = false)
        {
            if (pilot == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(pilot.pilotDef, usePools);
        }

        private List<PilotQuirk> getQuirks(AbstractActor actor, bool usePools)
        {
            if (actor == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(actor.GetPilot(), usePools);
        }

        public string getPilotToolTip(Pilot pilot)
        {
            string ret = "";

            if (pilot != null && Main.settings.enablePilotQuirks)
            {
                List<PilotQuirk> pilotQuirks = getQuirks(pilot);
                ret = "\n";
                foreach (PilotQuirk quirk in pilotQuirks)
                {
                    ret += $"<b>{quirk.quirkName}</b>:\n{quirk.description}\n\n";
                }
            }

            return ret;
        }

        public string getRoninHiringHallDescription(Pilot pilot)
        {
            string ret = "\n\n";

            if (pilot != null && Main.settings.enablePilotQuirks)
            {
                List<PilotQuirk> pilotQuirks = getQuirks(pilot);
                foreach (PilotQuirk quirk in pilotQuirks)
                {
                    ret += $"<b>{quirk.quirkName}:</b>{quirk.description}\n\n";
                }
            }

            return ret;
        }

        public string getRegularHiringHallDescription(Pilot pilot)
        {
            string ret = "";

            if (pilot != null && Main.settings.enablePilotQuirks)
            {
                List<PilotQuirk> pilotQuirks = getQuirks(pilot);
                foreach (PilotQuirk quirk in pilotQuirks)
                {
                    ret += $"{quirk.quirkName}\n\n{quirk.description}\n\n";
                }
            }

            return ret;
        }

        public bool lookUpQuirkDescription(string tag, out string desc)
        {
            PilotQuirk quirk;
            desc = "";
            if (quirks.TryGetValue(tag, out quirk))
            {
                desc = quirk.description;
                return true;
            }

            return false;
        }

        private void getEffectBonuses(AbstractActor actor, bool usePools, out List<EffectData> effects)
        {
            effects = new List<EffectData>();
            List<PilotQuirk> pilotQuirks = getQuirks(actor, usePools);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (EffectData effect in quirk.effects)
                {
                    effects.Add(effect);
                }
            }

        }

        public void applyBonuses(AbstractActor actor)
        {
            if (Main.settings.enablePilotQuirks)
            {
                List<EffectData> effects;
                bool canUsePools = false;
                if (actor.team == null || !actor.team.IsLocalPlayer)
                {
                    canUsePools = true;
                    Main.modLog.LogMessage("Processing AI actor, allowing pooled quirk use");
                }
                else
                {
                    if (Main.settings.playerQuirkPools)
                    {
                        canUsePools = true;
                        Main.modLog.LogMessage("pq player pools enabled, allowing pooled quirk use");
                    }
                }
                getEffectBonuses(actor, canUsePools, out effects);
                applyStatusEffects(actor, effects);
            }

        }

        public float getPilotCostMulitplier(PilotDef pilotDef)
        {
            float ret = 1.0f;

            List<PilotQuirk> pilotQuirks = getQuirks(pilotDef);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach(QuirkEffect effect in quirk.quirkEffects)
                {
                    if(effect.type == EQuirkEffectType.PilotCostFactor)
                    {
                        ret += effect.modifier;
                    }
                }
            }

            return ret;
        }

        private void updateStat(string trackerStat, string cStat, float trackerValue)
        {
            int cValue = companyStats.GetValue<int>(cStat);
            int MoraleMod = companyStats.GetValue<int>(PqMoraleModifierTracker);
            bool updateMoraleMod = cStat == Morale;
            Main.modLog.LogMessage($"possible update to {cStat}, current {cValue}, tracker: {trackerValue}");
            int trackerInt = (int) trackerValue;
            trackerValue -= trackerInt;
            if (trackerInt != 0)
            {
                cValue += trackerInt;
                MoraleMod += trackerInt;
            }
            if (trackerValue < 0)
            {
                cValue -= 1;
                MoraleMod -= 1;
                trackerValue = 1.0f + trackerValue;
            }
            Main.modLog.LogMessage($"Updating: {cStat} => {cValue}, tracker => {trackerValue}");
            companyStats.Set<int>(cStat, cValue);
            companyStats.Set<float>(trackerStat, trackerValue);
            if (updateMoraleMod)
            {
                Main.modLog.LogMessage($"Updating: {PqMoraleModifierTracker} => {MoraleMod}");
                companyStats.Set<int>(PqMoraleModifierTracker, MoraleMod);
            }
        }

        private void proccessPilotStats(PilotDef def, bool isNew)
        {
            List<string> proccessTags = new List<string>();
            List<PilotQuirk> pilotQuirks = getQuirks(def);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.PilotHealth)
                    {
                        if (isNew)
                        {
                            if (!def.PilotTags.Contains(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString()))
                            {
                                Traverse.Create(def).Property("Health")
                                    .SetValue((int) (def.Health + (int) effect.modifier));
                                Main.modLog.LogMessage($"adding health to pilot: {def.Description.Callsign}");
                                if (!proccessTags.Contains(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString()))
                                {
                                    proccessTags.Add(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString());
                                }
                            }
                        }
                        else
                        {
                            Main.modLog.LogMessage($"removing health to pilot: {def.Description.Callsign}");
                            Traverse.Create(def).Property("Health").SetValue((int) (def.Health - (int) effect.modifier));
                        }
                        
                    }
                }
            }

            foreach (string tag in proccessTags)
            {
                def.PilotTags.Add(tag);
            }
        }

        public void proccessPilot(PilotDef def, bool isNew)
        {
            Main.modLog.LogMessage($"processing pilot: {def.Description.Callsign}");
            proccessPilotStats(def, isNew);
            if (def.PilotTags.Contains(PqMarkedTag) && isNew)
            {
                if (!moraleModInstanced)
                {
                    int currentMorale = companyStats.GetValue<int>(PqMoraleModifierTracker);
                    bool updateMoraleMod = false;
                    float modChange = 0.0f;
                    
                    List<PilotQuirk> pQuirks = getQuirks(def);
                    foreach (PilotQuirk quirk in pQuirks)
                    {
                        foreach (QuirkEffect effect in quirk.quirkEffects)
                        {
                            if (effect.type == EQuirkEffectType.Morale)
                            {

                                modChange += effect.modifier;
                                updateMoraleMod = true;
                            }
                        }
                    }

                    if (updateMoraleMod)
                    {
                        currentMorale += (int)modChange;
                        Main.modLog.LogMessage($"Updating: {PqMoraleModifierTracker} => {currentMorale}");
                        companyStats.Set<int>(PqMoraleModifierTracker, currentMorale);
                    }
                }
                Main.modLog.LogMessage($"pilot {def.Description.Callsign} already marked, skipping");
                return;
            }
            float currentMechTek = companyStats.GetValue<float>(PqMechSkillTracker);
            float currentMedTek = companyStats.GetValue<float>(PqMedSkillTracker);
            float currentMoraleTek = companyStats.GetValue<float>(PqMoraleTracker);
            bool updateMech = false;
            bool updateMed = false;
            bool updateMorale = false;
            
            
            Main.modLog.LogMessage($"Tracker Stat: {PqMechSkillTracker}, value: {currentMechTek}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMedSkillTracker}, value: {currentMedTek}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMoraleTracker}, value: {currentMoraleTek}");

            List<PilotQuirk> pilotQuirks = getQuirks(def);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.MechTech)
                    {
                        if (isNew)
                        {
                            currentMechTek += effect.modifier;
                        }
                        else
                        {
                            currentMechTek -= effect.modifier;
                        }

                        updateMech = true;
                    }
                    else if (effect.type == EQuirkEffectType.MedTech)
                    {
                        if (isNew)
                        {
                            currentMedTek += effect.modifier;
                        }
                        else
                        {
                            currentMedTek -= effect.modifier;
                        }

                        updateMed = true;
                    }
                    else if (effect.type == EQuirkEffectType.Morale)
                    {
                        if (isNew)
                        {
                            currentMoraleTek += effect.modifier;
                        }
                        else
                        {
                            currentMoraleTek -= effect.modifier;
                        }

                        updateMorale = true;
                    }
                }
            }

            if (updateMech)
            {
                updateStat(PqMechSkillTracker, MechTechSkill, currentMechTek);
            }
            if (updateMed)
            {
                updateStat(PqMedSkillTracker, MedTechSkill, currentMedTek);
            }
            if (updateMorale)
            {
                updateStat(PqMoraleTracker, Morale, currentMoraleTek);
            }
            
            if (!def.PilotTags.Contains(PqMarkedTag) && isNew)
            {
                def.PilotTags.Add(PqMarkedTag);
            }
            
        }

        public void stealAmount(Pilot pilot, SimGameState sim)
        {
            int stealChance = 0;
            int stealAmount = 0;
            int stealChance2 = 0;
            int stealAmount2 = 0;
            List<PilotQuirk> pilotQuirks = getQuirks(pilot);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.CriminalEffect)
                    {
                        stealChance += (int)effect.modifier;
                        stealAmount += (int)effect.secondaryModifier;
                    }
                    else if (effect.type == EQuirkEffectType.CriminalEffect2)
                    {
                        stealChance2 += (int)effect.modifier;
                        stealAmount2 += (int)effect.secondaryModifier;
                    }
                }
            }
            Random random = new Random();
            int roll = random.Next(1, 101);
            if (roll < stealChance)
            {
                Main.modLog.LogMessage($"Pilot {pilot.Callsign}, steals: {stealAmount}");
                 sim.AddFunds(stealAmount * -1, null, true);
            }
            roll = random.Next(1, 101);
            if (roll < stealChance2)
            {
                Main.modLog.LogMessage($"Pilot {pilot.Callsign}, steals: {stealAmount2}");
                sim.AddFunds(stealAmount2 * -1, null, true);
            }
        }

        public float getArgoUpgradeCostModifier(List<Pilot> pilots, string upgradeId, bool upkeep)
        {
            float ret = 1.0f;
            EQuirkEffectType type = EQuirkEffectType.ArgoUpgradeFactor;
            if (upkeep)
            {
                type = EQuirkEffectType.ArgoUpkeepFactor;
            }
            foreach (Pilot pilot in pilots)
            {
                List<PilotQuirk> pilotQuirks = getQuirks(pilot);
                foreach (PilotQuirk quirk in pilotQuirks)
                {
                    foreach (QuirkEffect effect in quirk.quirkEffects)
                    {
                        if (effect.type == type)
                        {
                            if (effect.affectedIds.Contains(upgradeId) || effect.affectedIds.Contains(PqAllArgoUpgrades))
                            {
                                if (Main.settings.debug) Main.modLog.DebugMessage($"Found Argo factor: {quirk.quirkName}, value: {effect.modifier}");
                                if (Main.settings.pqArgoAdditive)
                                {
                                    ret += effect.modifier;
                                }
                                else
                                {
                                    if (Main.settings.pqArgoMultiAutoAdjust)
                                    {
                                        ret *= (1.0f + effect.modifier);
                                    }
                                    else
                                    {
                                        ret *= effect.modifier;
                                    }
                                }

                                
                            }
                        }
                    }
                }
            }

            if (ret < Main.settings.pqArgoMin)
            {
                ret = Main.settings.pqArgoMin;
            }
            if (Main.settings.debug) Main.modLog.DebugMessage($"Found cost factor multiplier: {ret}");
            return ret;
        }

        public void resetMorale(SimGameState sim)
        {
            // this can only happen on new career start
            if (companyStats == null)
            {
                setCompanyStats(sim.CompanyStats);
                forceMoraleInstanced();
            }
            
            int MoraleModifier = companyStats.GetValue<int>(PqMoraleModifierTracker);
            Main.modLog.LogMessage($"Reseting Morale, baseline {MoraleModifier}");
            if (sim.CurDropship == DropshipType.Argo)
            {
                foreach (ShipModuleUpgrade shipModuleUpgrade in sim.ShipUpgrades)
                {
                    foreach (SimGameStat stat in shipModuleUpgrade.Stats)
                    {
                        bool isNumeric = false;
                        int modifier = 0;
                        if (stat.name == Morale)
                        {
                            isNumeric = int.TryParse(stat.value, out modifier);
                            if (isNumeric)
                            {
                                MoraleModifier += modifier;
                            }
                        }
                    }
                }
            }
            Main.modLog.LogMessage($"Morale, baseline + Argo {MoraleModifier}");
            MoraleModifier += sim.Constants.Story.StartingMorale;
            Main.modLog.LogMessage($"New Morale: {MoraleModifier}");
            companyStats.ModifyStat<int>("Mission", 0, "COMPANY_MonthlyStartingMorale", StatCollection.StatOperation.Set, MoraleModifier, -1, true);
            companyStats.ModifyStat<int>("Mission", 0, "Morale", StatCollection.StatOperation.Set, MoraleModifier, -1, true);
        }
    }
}
