using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using MechAffinity.Data;
using Newtonsoft.Json.Linq;

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
        private static PilotQuirkManager _instance;
        private StatCollection companyStats;
        private Dictionary<string, PilotQuirk> quirks;

        public static PilotQuirkManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotQuirkManager();
                return _instance;
            }
        }

        public void initialize()
        {
            UidManager.reset();
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
            Main.modLog.LogMessage($"Tracker Stat: {PqMechSkillTracker}, value: {companyStats.GetValue<float>(PqMechSkillTracker)}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMedSkillTracker}, value: {companyStats.GetValue<float>(PqMedSkillTracker)}");
            Main.modLog.LogMessage($"Tracker Stat: {PqMoraleTracker}, value: {companyStats.GetValue<float>(PqMoraleTracker)}");
        }

        private List<PilotQuirk> getQuirks(PilotDef pilotDef)
        {
            List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
            if (pilotDef != null)
            {
                List<string> tags = pilotDef.PilotTags.ToList();
                foreach (string tag in tags)
                {
                    //Main.modLog.LogMessage($"Processing tag: {tag}");
                    PilotQuirk quirk;
                    if (quirks.TryGetValue(tag, out quirk))
                    {
                        pilotQuirks.Add(quirk);
                    }
                }
            }
            return pilotQuirks;
        }

        private List<PilotQuirk> getQuirks(Pilot pilot)
        {
            if (pilot == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(pilot.pilotDef);
        }

        private List<PilotQuirk> getQuirks(AbstractActor actor)
        {
            if (actor == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(actor.GetPilot());
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

        private void getEffectBonuses(AbstractActor actor, out List<EffectData> effects)
        {
            effects = new List<EffectData>();
            List<PilotQuirk> pilotQuirks = getQuirks(actor);
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
                getEffectBonuses(actor, out effects);
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
            Main.modLog.LogMessage($"possible update to {cStat}, current {cValue}, tracker: {trackerValue}");
            int trackerInt = (int) trackerValue;
            trackerValue -= trackerInt;
            if (trackerInt != 0)
            {
                cValue += trackerInt;
            }
            if (trackerValue < 0)
            {
                cValue -= 1;
                trackerValue = 1.0f + trackerValue;
            }
            Main.modLog.LogMessage($"Updating: {cStat} => {cValue}, tracker => {trackerValue}");
            companyStats.Set<int>(cStat, cValue);
            companyStats.Set<float>(trackerStat, trackerValue);
        }

        public void proccessPilot(PilotDef def, bool isNew)
        {
            if (def.PilotTags.Contains(PqMarkedTag) && isNew)
            {
                Main.modLog.LogMessage($"pilot {def.Description.Callsign} already marked, skipping");
                return;
            }
            float currentMechTek = companyStats.GetValue<float>(PqMechSkillTracker);
            float currentMedTek = companyStats.GetValue<float>(PqMedSkillTracker);
            float currentMoraleTek = companyStats.GetValue<float>(PqMoraleTracker);
            bool updateMech = false;
            bool updateMed = false;
            bool updateMorale = false;
            
            Main.modLog.LogMessage($"processing pilot: {def.Description.Callsign}");
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
                                ret += effect.modifier;
                            }
                        }
                    }
                }
            }

            if (ret < 0.0f)
            {
                ret = 0.0f;
            }
            if (Main.settings.debug) Main.modLog.DebugMessage($"Found cost factor multiplier: {ret}");
            return ret;
        }
    }
}
