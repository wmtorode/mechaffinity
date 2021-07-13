using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace MechAffinity
{
    public class BaseEffectManager
    {
        protected bool hasInitialized = false; 
        protected void applyStatusEffects(AbstractActor actor, List<EffectData> effects)
        {
            foreach (EffectData statusEffect in effects)
            {
                if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive)
                {
                    if (statusEffect.targetingData.effectTargetType == EffectTargetType.Creator)
                    {
                        string effectId = $"PassiveEffect_{actor.GUID}_{UidManager.Uid}";
                        Main.modLog.LogMessage($"Applying affect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name}");
                        actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, (ICombatant)actor, (ICombatant)actor, new WeaponHitInfo(), 0, false);
                    }
                }
            }
        }

    }
}
