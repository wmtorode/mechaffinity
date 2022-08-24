using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using MechAffinity.Data;

namespace MechAffinity
{
    public class BaseEffectManager
    {
        protected bool hasInitialized = false;
        protected List<PilotDelayedEffects> delayedEffectsList = new List<PilotDelayedEffects>();
        private List<AbstractActor> spawnedActors = new List<AbstractActor>();


        public void ResetEffectCache()
        {
           delayedEffectsList.Clear();
           spawnedActors.Clear();
        }

        protected void applyStatusEffects(AbstractActor actor, List<EffectData> effects)
        {
            List<PilotDelayedEffects> delayedEffectsFromActor = new List<PilotDelayedEffects>();
            foreach (EffectData statusEffect in effects)
            {
                string effectId = $"PassiveEffect_{actor.GUID}_{UidManager.Uid}";
                switch (statusEffect.targetingData.effectTriggerType)
                {
                    case EffectTriggerType.Passive:
                        switch (statusEffect.targetingData.effectTargetType)
                        {
                            case EffectTargetType.Creator:
                                Main.modLog.LogMessage($"Applying affect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name} to creator");
                                actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, actor,actor, new WeaponHitInfo(), 0, false);
                                break;
                            case EffectTargetType.AllLanceMates:
                                Main.modLog.LogMessage($"Found lancemate effect {effectId}, effect ID: {statusEffect.Description.Id}");
                                actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, actor,actor, new WeaponHitInfo(), 0, false);
                                List<AbstractActor> lancemates =
                                    spawnedActors.FindAll((x => x.team == actor.team));
                                foreach (var lancemate in lancemates)
                                {
                                    Main.modLog.LogMessage($"Applying Lancemate effect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name} to {lancemate.DisplayName} ");
                                    actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, actor, lancemate,
                                        new WeaponHitInfo(), 0);
                                }
                                delayedEffectsFromActor.Add(new PilotDelayedEffects()
                                {
                                    actor = actor,
                                    effect = statusEffect,
                                    effectId = effectId,
                                    effectTargetType = statusEffect.targetingData.effectTargetType
                                });
                                break;
                            case EffectTargetType.AllEnemies:
                                Main.modLog.LogMessage($"Found enemy effect {effectId}, effect ID: {statusEffect.Description.Id}");
                                List<AbstractActor> allEnemies = spawnedActors.FindAll((x => x.IsEnemy(actor)));
                                foreach (var enemy in allEnemies)
                                {
                                    Main.modLog.LogMessage($"Applying enemy effect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name} to {enemy.DisplayName} ");
                                    actor.Combat.EffectManager.CreateEffect(statusEffect, effectId, -1, actor, enemy,
                                        new WeaponHitInfo(), 0);
                                }
                                delayedEffectsFromActor.Add(new PilotDelayedEffects()
                                {
                                    actor = actor,
                                    effect = statusEffect,
                                    effectId = effectId,
                                    effectTargetType = statusEffect.targetingData.effectTargetType
                                });
                                break;
                            default:
                                Main.modLog.LogError($"Unable to apply passive effect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name}, unsupported target type: {statusEffect.targetingData.effectTargetType.ToString()} ");
                                break;
                        }
                        break;
                    case EffectTriggerType.OnHit:
                        foreach (var weapon in actor.Weapons)
                        {
                            Main.modLog.LogMessage($"Add onHit effect: {statusEffect.Description.Name} to {weapon.UIName}");
                            Main.modLog.DebugMessage($"Before Add: {weapon.weaponDef.statusEffects.Length}");
                            List<EffectData> statEffects = weapon.weaponDef.statusEffects.ToList();
                            statEffects.Add(statusEffect);
                            weapon.weaponDef.SetEffectData(statEffects.ToArray());
                            Main.modLog.DebugMessage($"After Add: {weapon.weaponDef.statusEffects.Length}");
                            
                        }
                        break;
                    default:
                        Main.modLog.LogError($"Unable to apply effect {effectId}, effect ID: {statusEffect.Description.Id}, name: {statusEffect.Description.Name}, unsupported Trigger Type: {statusEffect.targetingData.effectTriggerType.ToString()} ");
                        break;

                }
            }

            foreach (var delayedEffect in delayedEffectsList)
            {
                switch (delayedEffect.effectTargetType)
                {
                    case EffectTargetType.AllLanceMates:
                        if (delayedEffect.actor.team == actor.team)
                        {
                            Main.modLog.LogMessage($"Applying delayed Lancemate effect {delayedEffect.effectId}, effect ID: {delayedEffect.effect.Description.Id}, name: {delayedEffect.effect.Description.Name} to {actor.DisplayName} ");
                            actor.Combat.EffectManager.CreateEffect(delayedEffect.effect, delayedEffect.effectId, -1, delayedEffect.actor, actor,
                                new WeaponHitInfo(), 0);
                        }
                        break;
                    case EffectTargetType.AllEnemies:
                        if (delayedEffect.actor.IsEnemy(actor))
                        {
                            Main.modLog.LogMessage($"Applying delayed enemy effect {delayedEffect.effectId}, effect ID: {delayedEffect.effect.Description.Id}, name: {delayedEffect.effect.Description.Name} to {actor.DisplayName} ");
                            actor.Combat.EffectManager.CreateEffect(delayedEffect.effect, delayedEffect.effectId, -1, delayedEffect.actor, actor,
                                new WeaponHitInfo(), 0);
                        }
                        break;
                }
            }
            delayedEffectsList.AddRange(delayedEffectsFromActor);
            spawnedActors.Add(actor);
        }

    }
}
