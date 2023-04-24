using System.Collections.Generic;
using System.Linq;
using BattleTech;
using MechAffinity.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

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
        private Dictionary<string, QuirkRestriction> quirkRestrictions;
        private Dictionary<string, bool> immortalityCache;
        private PilotQuirkSettings settings;
        private bool moraleModInstanced;
        private Dictionary<string, float> argoUpgradeBaseCostCache;
        private Dictionary<string, float> argoUpgradeUpkeepCostCache;
        private Dictionary<string, PilotStealChanceCacheEntry> pilotStealCache;

        public bool BlockFinanceScreenUpdate { get; set; }

        public static PilotQuirkManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PilotQuirkManager();
                if (!_instance.hasInitialized) _instance.initialize(Main.settings.quirkSettings, Main.pilotQuirks);
                return _instance;
            }
        }

        public void initialize(PilotQuirkSettings pilotQuirkSettings, List<PilotQuirk> pilotQuirks)
        {
            if(hasInitialized) return;
            settings = pilotQuirkSettings;
            UidManager.reset();
            moraleModInstanced = true;
            quirks = new Dictionary<string, PilotQuirk>();
            foreach (PilotQuirk pilotQuirk in pilotQuirks)
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
            foreach (QuirkPool quirkPool in settings.quirkPools)
            {
                quirkPools.Add(quirkPool.tag, quirkPool);
            }
            quirkRestrictions = new Dictionary<string, QuirkRestriction>();
            foreach (var restriction in settings.restrictions)
            {
                quirkRestrictions.Add(restriction.restrictionCategory, restriction);
            }

            immortalityCache = new Dictionary<string, bool>();

            hasInitialized = true;

            argoUpgradeBaseCostCache = new Dictionary<string, float>();
            argoUpgradeUpkeepCostCache = new Dictionary<string, float>();
            pilotStealCache = new Dictionary<string, PilotStealChanceCacheEntry>();
            BlockFinanceScreenUpdate = false;
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
            Main.modLog.Info?.Write($"Tracker Stat: {PqMechSkillTracker}, value: {companyStats.GetValue<float>(PqMechSkillTracker)}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMedSkillTracker}, value: {companyStats.GetValue<float>(PqMedSkillTracker)}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMoraleTracker}, value: {companyStats.GetValue<float>(PqMoraleTracker)}");
            immortalityCache.Clear();
        }

        public void forceMoraleInstanced()
        {
            moraleModInstanced = true;
        }

        public void ResetArgoCostCache()
        {
            Main.modLog.Info?.Write("Clearing argo cost caches");
            argoUpgradeBaseCostCache.Clear();
            argoUpgradeUpkeepCostCache.Clear();
            pilotStealCache.Clear();
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
        
        private List<PilotQuirk> getQuirks(PilotDef pilotDef, bool usePools = false, bool restrictedOnly = false)
        {
            List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();
            if (pilotDef != null)
            {
                IEnumerable<string> tags = pilotDef.PilotTags;
                List<string> usedQuirks = new List<string>();
                foreach (string tag in tags)
                {
                    //Main.modLog.Info?.Write($"Processing tag: {tag}");
                    PilotQuirk quirk;
                    if (quirks.TryGetValue(tag, out quirk))
                    {
                        if (restrictedOnly)
                        {
                            if (!string.IsNullOrEmpty(quirk.restrictionCategory))
                            {
                                pilotQuirks.Add(quirk);
                                usedQuirks.Add(tag);
                            }
                        }
                        else
                        {
                            pilotQuirks.Add(quirk);
                            usedQuirks.Add(tag);
                        }
                        
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
                                        Main.modLog.Info?.Write($"Adding Randomized Quirk: {possibleQuirk}");
                                    }
                                    else
                                    {
                                        Main.modLog.Info?.Write($"Skipped adding randomized quirk {possibleQuirk}, because it was already used");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return pilotQuirks;
        }

        private List<PilotQuirk> getQuirks(Pilot pilot, bool usePools = false, bool restrictedOnly = false)
        {
            if (pilot == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(pilot.pilotDef, usePools, restrictedOnly);
        }

        private List<PilotQuirk> getQuirks(AbstractActor actor, bool usePools, bool restrictedOnly = false)
        {
            if (actor == null)
            {
                return new List<PilotQuirk>();
            }

            return getQuirks(actor.GetPilot(), usePools, restrictedOnly);
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
        
        private bool GetQuirk(string tag, out PilotQuirk quirk)
        {
            quirk = null;
            if (quirks.TryGetValue(tag, out quirk))
            {
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
                    Main.modLog.Info?.Write("Processing AI actor, allowing pooled quirk use");
                }
                else
                {
                    if (settings.playerQuirkPools)
                    {
                        canUsePools = true;
                        Main.modLog.Info?.Write("pq player pools enabled, allowing pooled quirk use");
                    }
                }
                getEffectBonuses(actor, canUsePools, out effects);
                applyStatusEffects(actor, effects);
            }

        }

        public float getPilotCostModifier(PilotDef pilotDef, out int flatCost)
        {
            float ret = 1.0f;
            flatCost = 0;

            List<PilotQuirk> pilotQuirks = getQuirks(pilotDef);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach(QuirkEffect effect in quirk.quirkEffects)
                {
                    if(effect.type == EQuirkEffectType.PilotCostFactor)
                    {
                        ret += effect.modifier;
                        flatCost += Mathf.FloorToInt(effect.secondaryModifier);
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
            Main.modLog.Info?.Write($"possible update to {cStat}, current {cValue}, tracker: {trackerValue}");
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
            Main.modLog.Info?.Write($"Updating: {cStat} => {cValue}, tracker => {trackerValue}");
            companyStats.Set<int>(cStat, cValue);
            companyStats.Set<float>(trackerStat, trackerValue);
            if (updateMoraleMod)
            {
                Main.modLog.Info?.Write($"Updating: {PqMoraleModifierTracker} => {MoraleMod}");
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
                                def.Health += (int)effect.modifier;
                                Main.modLog.Info?.Write($"adding health to pilot: {def.Description.Callsign}");
                                if (!proccessTags.Contains(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString()))
                                {
                                    proccessTags.Add(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString());
                                }
                            }
                        }
                        else
                        {
                            Main.modLog.Info?.Write($"removing health to pilot: {def.Description.Callsign}");
                            def.Health -= (int)effect.modifier;
                        }
                        
                    }
                }
            }

            foreach (string tag in proccessTags)
            {
                def.PilotTags.Add(tag);
            }
        }

        private void processAdditionalTags(PilotDef def, bool isNew)
        {
            if (!isNew) return;
            IEnumerable<string> tags = def.PilotTags;
            foreach (string tag in settings.addTags)
            {
                if (!tags.Contains(tag))
                {
                    def.PilotTags.Add(tag);
                    Main.modLog.Info?.Write($"Adding Tag: {tag} to {def.Description.Callsign}");
                }
            }

            foreach (var update in settings.tagUpdates)
            {
                if (tags.Contains(update.selector))
                {
                    foreach (var newTag in update.addTags)
                    {
                        if (!tags.Contains(newTag))
                        {
                            def.PilotTags.Add(newTag);
                            Main.modLog.Info?.Write($"Adding Tag: {newTag} to {def.Description.Callsign}");
                        }
                    }
                    foreach (var depTag in update.removeTags)
                    {
                        if (tags.Contains(depTag))
                        {
                            def.PilotTags.Remove(depTag);
                            Main.modLog.Info?.Write($"Removing Tag: {depTag} to {def.Description.Callsign}");
                        }
                    }
                }
            }
            
        }

        public void proccessPilot(PilotDef def, bool isNew)
        {
            Main.modLog.Info?.Write($"processing pilot: {def.Description.Callsign}");
            proccessPilotStats(def, isNew);
            processAdditionalTags(def, isNew);
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
                        Main.modLog.Info?.Write($"Updating: {PqMoraleModifierTracker} => {currentMorale}");
                        companyStats.Set<int>(PqMoraleModifierTracker, currentMorale);
                    }
                }
                Main.modLog.Info?.Write($"pilot {def.Description.Callsign} already marked, skipping");
                return;
            }
            float currentMechTek = companyStats.GetValue<float>(PqMechSkillTracker);
            float currentMedTek = companyStats.GetValue<float>(PqMedSkillTracker);
            float currentMoraleTek = companyStats.GetValue<float>(PqMoraleTracker);
            bool updateMech = false;
            bool updateMed = false;
            bool updateMorale = false;
            
            
            Main.modLog.Info?.Write($"Tracker Stat: {PqMechSkillTracker}, value: {currentMechTek}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMedSkillTracker}, value: {currentMedTek}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMoraleTracker}, value: {currentMoraleTek}");

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

        public void processTagChange(Pilot pilot, string tag, bool isNew)
        {
            Main.modLog.Debug?.Write($"Tag change: {tag}");
            PilotQuirk quirk;
            if (!GetQuirk(tag, out quirk)) return;
            float currentMechTek = companyStats.GetValue<float>(PqMechSkillTracker);
            float currentMedTek = companyStats.GetValue<float>(PqMedSkillTracker);
            float currentMoraleTek = companyStats.GetValue<float>(PqMoraleTracker);
            bool updateMech = false;
            bool updateMed = false;
            bool updateMorale = false;
            Main.modLog.Info?.Write($"Processing Quirk Tag change on {pilot.Callsign} - {tag}: {isNew}");
            
            Main.modLog.Info?.Write($"Tracker Stat: {PqMechSkillTracker}, value: {currentMechTek}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMedSkillTracker}, value: {currentMedTek}");
            Main.modLog.Info?.Write($"Tracker Stat: {PqMoraleTracker}, value: {currentMoraleTek}");

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
                else if (effect.type == EQuirkEffectType.PilotHealth)
                {
                    if (isNew)
                    {
                        pilot.pilotDef.Health += (int)effect.modifier;
                        Main.modLog.Info?.Write($"adding health to pilot: {pilot.pilotDef.Description.Callsign}");
                        if (!pilot.pilotDef.PilotTags.Contains(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString()))
                        {
                            pilot.pilotDef.PilotTags.Add(PqMarkedPrefix + EQuirkEffectType.PilotHealth.ToString());
                        }
                    }
                    else
                    {
                        Main.modLog.Info?.Write($"removing health to pilot: {pilot.pilotDef.Description.Callsign}");
                        pilot.pilotDef.Health -= (int)effect.modifier;
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
        }

        public int stealAmount(Pilot pilot, SimGameState sim)
        {
            int totalStolen = 0;

            PilotStealChanceCacheEntry cacheEntry;

            if (!pilotStealCache.TryGetValue(pilot.pilotDef.Description.Id, out cacheEntry))
            {
                Main.modLog.Info?.Write($"Pilot {pilot.Callsign} steal cache miss!");
                cacheEntry = new PilotStealChanceCacheEntry();
                List<PilotQuirk> pilotQuirks = getQuirks(pilot);
                foreach (PilotQuirk quirk in pilotQuirks)
                {
                    foreach (QuirkEffect effect in quirk.quirkEffects)
                    {
                        if (effect.type == EQuirkEffectType.CriminalEffect)
                        {
                            cacheEntry.StealChance += (int)effect.modifier;
                            cacheEntry.StealAmount += (int)effect.secondaryModifier;
                        }
                        else if (effect.type == EQuirkEffectType.CriminalEffect2)
                        {
                            cacheEntry.StealChance2 += (int)effect.modifier;
                            cacheEntry.StealAmount2 += (int)effect.secondaryModifier;
                        }
                    }
                }
                
                pilotStealCache.Add(pilot.pilotDef.Description.Id, cacheEntry);
            }
            
            
            Random random = new Random();
            int roll = random.Next(0, 100);
            if (roll < cacheEntry.StealChance)
            {
                Main.modLog.Info?.Write($"Pilot {pilot.Callsign}, steals: {cacheEntry.StealAmount}");
                totalStolen -= cacheEntry.StealAmount;
            }
            roll = random.Next(0, 100);
            if (roll < cacheEntry.StealChance2)
            {
                Main.modLog.Info?.Write($"Pilot {pilot.Callsign}, steals: {cacheEntry.StealAmount2}");
                totalStolen -= cacheEntry.StealAmount2;
            }

            return totalStolen;
        }

        public bool hasImmortality(PilotDef pilotDef)
        {
            if (immortalityCache.ContainsKey(pilotDef.Description.Id))
            {
                return immortalityCache[pilotDef.Description.Id];
            }
            
            List<PilotQuirk> pilotQuirks = getQuirks(pilotDef);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.Immortality)
                    {
                        immortalityCache.Add(pilotDef.Description.Id, true);
                        return true;
                    }
                }
            }

            immortalityCache.Add(pilotDef.Description.Id, false);
            return false;
            
        }

        public void additionalSalvage(PilotDef pilotDef, ref int additionalSalvage, ref int additionalSalvagePicks)
        {
            List<PilotQuirk> pilotQuirks = getQuirks(pilotDef);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.AdditionalSalvage)
                    {
                        additionalSalvage += Mathf.FloorToInt(effect.modifier);
                        additionalSalvagePicks += Mathf.FloorToInt(effect.secondaryModifier);
                        Main.modLog.Info?.Write($"Pilot: {pilotDef.Description.Callsign}, adds: {effect.modifier} salvage, {effect.secondaryModifier} picks");
                    }
                }
            }
            
        }
        
        public void additionalCbills(PilotDef pilotDef, ref int flatPayout, ref float percentagePayout)
        {
            List<PilotQuirk> pilotQuirks = getQuirks(pilotDef);
            foreach (PilotQuirk quirk in pilotQuirks)
            {
                foreach (QuirkEffect effect in quirk.quirkEffects)
                {
                    if (effect.type == EQuirkEffectType.AdditionalCbills)
                    {
                        flatPayout += Mathf.FloorToInt(effect.modifier);
                        percentagePayout += effect.secondaryModifier;
                        Main.modLog.Info?.Write($"Pilot: {pilotDef.Description.Callsign}, adds: {effect.modifier} flat payout, {effect.secondaryModifier} payout percentage");
                    }
                }
            }
            
        }

        public bool isPilotImmortal(PilotDef pilotDef)
        {
            if (pilotDef.IsImmortal) return true;
            return hasImmortality(pilotDef);

        }

        public bool isPilotImmortal(Pilot pilot)
        {
            return isPilotImmortal(pilot.pilotDef);
        }

        public float getArgoUpgradeCostModifier(List<Pilot> pilots, string upgradeId, bool upkeep)
        {
            if (upkeep)
            {
                if (argoUpgradeUpkeepCostCache.ContainsKey(upgradeId)) return argoUpgradeUpkeepCostCache[upgradeId];
            }

            if (!upkeep && argoUpgradeBaseCostCache.ContainsKey(upgradeId)) return argoUpgradeBaseCostCache[upgradeId];


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
                                Main.modLog.Debug?.Write($"Found Argo factor: {quirk.quirkName}, value: {effect.modifier}");
                                if (settings.argoAdditive)
                                {
                                    ret += effect.modifier;
                                }
                                else
                                {
                                    if (settings.argoMultiAutoAdjust)
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

            if (ret < settings.argoMin)
            {
                ret = settings.argoMin;
            }
            Main.modLog.Info?.Write($"Found cost factor multiplier: {ret} for {upgradeId}, caching");
            if (upkeep)
            {
                argoUpgradeUpkeepCostCache.Add(upgradeId, ret);
            }
            else
            {
                argoUpgradeBaseCostCache.Add(upgradeId, ret);
            }
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
            Main.modLog.Info?.Write($"Reseting Morale, baseline {MoraleModifier}");
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
            Main.modLog.Info?.Write($"Morale, baseline + Argo {MoraleModifier}");
            MoraleModifier += sim.Constants.Story.StartingMorale;
            Main.modLog.Info?.Write($"New Morale: {MoraleModifier}");
            companyStats.ModifyStat<int>("Mission", 0, "COMPANY_MonthlyStartingMorale", StatCollection.StatOperation.Set, MoraleModifier, -1, true);
            companyStats.ModifyStat<int>("Mission", 0, "Morale", StatCollection.StatOperation.Set, MoraleModifier, -1, true);
        }

        public QuirkRestriction pilotRestrictionInEffect(List<Pilot> pilotsInUse)
        {
            Dictionary<string, int> restrictionsToWatch = new Dictionary<string, int>();
            // check each pilot and tally up any restricted quirks in use
            foreach (var pilot in pilotsInUse)
            {
                List<PilotQuirk> restrictedQuirks = getQuirks(pilot, false, true);
                foreach (var quirk in restrictedQuirks)
                {
                    if (!restrictionsToWatch.ContainsKey(quirk.restrictionCategory))
                    {
                        restrictionsToWatch.Add(quirk.restrictionCategory, 0);
                    }

                    restrictionsToWatch[quirk.restrictionCategory] += 1;
                }
            }
            
            // if any restrictions are breached send the first that is
            foreach (var restrictedCategory in restrictionsToWatch.Keys)
            {
                QuirkRestriction quirk;
                if (quirkRestrictions.TryGetValue(restrictedCategory, out quirk))
                {
                    if (quirk.deploymentCap < restrictionsToWatch[restrictedCategory])
                    {
                        return quirk;
                    }
                }
            }
            return (QuirkRestriction) null;
        }
    }
}
