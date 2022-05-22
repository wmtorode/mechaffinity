using System.Collections.Generic;
using BattleTech;
using MechAffinity.Data;
using System;

namespace MechAffinity
{
    public class StablePilotingManager: BaseEffectManager
    {
        
        private static StablePilotingManager _instance;
        private Dictionary<string, PilotTagStabilityEffect> tagEffects;
        private StablePilotingSettings settings;

        public static StablePilotingManager Instance
        {
            get
            {
                if (_instance == null) _instance = new StablePilotingManager();
                if (!_instance.hasInitialized) _instance.initialize();
                return _instance;
            }
        }

        public void initialize()
        {
            if(hasInitialized) return;
            tagEffects = new Dictionary<string, PilotTagStabilityEffect>();
            settings = Main.settings.stablePilotingSettings;
            foreach (PilotTagStabilityEffect tagEffect in settings.tagEffects)
            {
                tagEffects.Add(tagEffect.tag, tagEffect);
            }

            hasInitialized = true;
        }

        private float getInjuryPenalty(Pilot pilot)
        {
            return (float)pilot.Injuries * settings.increasePerInjury;
        }

        private float getReductionPerPilotingSkill(Pilot pilot)
        {
            return (float)pilot.Piloting * settings.reductionPerPiloting;
        }

        private float getTagEffect(Pilot pilot, PilotTagStabilityEffect tagEffect)
        {
            float effect = 0f;
            switch (tagEffect.type)
            {
                case EStabilityEffectType.Flat:
                    effect = tagEffect.effect;
                    break;
                case EStabilityEffectType.PilotingInverse:
                    effect = (float)Math.Max(settings.InverseMax - pilot.Piloting, 0) * tagEffect.effect;
                    break;
                default:
                    effect = (float)pilot.Piloting * tagEffect.effect;
                    break;
            }
            return effect;
        }

        private float getTagEffects(Pilot pilot)
        {
            float overallEffect = 0f;
            foreach (string tag in pilot.pilotDef.PilotTags)
            {
                if (tagEffects.ContainsKey(tag))
                {
                    overallEffect += getTagEffect(pilot, tagEffects[tag]);
                }
            }

            return overallEffect;
        }

        public float getStabilityModifier(Pilot pilot)
        {
            float modifier = 1.0f;
            modifier -= getReductionPerPilotingSkill(pilot);
            modifier += getInjuryPenalty(pilot);
            modifier += getTagEffects(pilot);
            return modifier;
        }
    }
}