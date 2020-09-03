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

        private List<PilotQuirk> getQuirks(Pilot pilot)
        {
            List<PilotQuirk> pilotQuirks = new List<PilotQuirk>();

            if (pilot != null)
            {
                List<string> tags = pilot.pilotDef.PilotTags.ToList();
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
                    ret += $"<b>{quirk.tag}</b>\n{quirk.description}\n\n";
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
    }
}
