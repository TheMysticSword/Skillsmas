using RoR2;
using R2API;
using MysticsRisky2Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using System.Collections.Generic;
using RoR2.Projectile;

namespace Skillsmas.SoftDependencies
{
    internal static class ArtificerExtendedSupport
    {
        public static System.Reflection.BindingFlags bindingFlagAll = (System.Reflection.BindingFlags)(-1);

        internal static void Init()
        {
            new ILHook(
                typeof(ArtificerExtended.Components.ElementCounter).GetMethod("GetPowers", bindingFlagAll),
                il =>
                {
                    ILCursor c = new ILCursor(il);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<System.Action<ArtificerExtended.Components.ElementCounter, SkillLocator>>((elementCounter, skillLocator) =>
                    {
                        SkillsmasArtificerExtendedElementCounter customElementCounter;
                        var gameObject = elementCounter.gameObject;
                        if (!SkillsmasArtificerExtendedElementCounter.lookupDictionary.TryGetValue(gameObject, out customElementCounter))
                        {
                            customElementCounter = gameObject.AddComponent<SkillsmasArtificerExtendedElementCounter>();
                        }
                        customElementCounter.rockPower = 0;
                        customElementCounter.waterPower = 0;
                    });
                }
            );

            new ILHook(
                typeof(ArtificerExtended.Components.ElementCounter).GetMethod("GetSkillPower", bindingFlagAll),
                il =>
                {
                    ILCursor c = new ILCursor(il);
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<System.Action<ArtificerExtended.Components.ElementCounter, GenericSkill>>((elementCounter, skill) =>
                    {
                        SkillsmasArtificerExtendedElementCounter customElementCounter;
                        SkillsmasArtificerExtendedElementCounter.lookupDictionary.TryGetValue(elementCounter.gameObject, out customElementCounter);

                        var skillNameToken = skill.baseSkill.skillNameToken;
                        if (skillNameToken.StartsWith("MAGE_SKILLSMAS_"))
                        {
                            var split = skillNameToken.Split('_');
                            if (split.Length >= 4)
                            {
                                var element = split[3];
                                switch (element)
                                {
                                    case "FIRE":
                                        elementCounter.firePower++;
                                        break;
                                    case "LIGHTNING":
                                        elementCounter.lightningPower++;
                                        break;
                                    case "ICE":
                                        elementCounter.icePower++;
                                        break;
                                    case "ROCK":
                                        if (customElementCounter) customElementCounter.rockPower++;
                                        break;
                                    case "WATER":
                                        if (customElementCounter) customElementCounter.waterPower++;
                                        break;
                                }
                            }
                        }
                    });
                }
            );
        }

        internal static void TriggerAltPassiveSkillCast(GameObject entityStateMachineObject)
        {
            if (ArtificerExtended.Passive.AltArtiPassive.instanceLookup.TryGetValue(entityStateMachineObject, out var passive))
            {
                passive.SkillCast();
            }
        }

        internal static bool BodyHasAltPassive(GameObject bodyObject)
        {
            return ArtificerExtended.Passive.AltArtiPassive.instanceLookup.TryGetValue(bodyObject, out _);
        }

        internal static void OnBarrierCrystalSpawned(GameObject childGameObject, ProjectileExplosion projectileExplosion)
        {
            var barrierPickup = childGameObject.GetComponentInChildren<DamageTypes.Crystallize.SkillsmasBarrierPickup>();
            if (barrierPickup)
            {
                var owner = projectileExplosion.projectileController.owner;
                if (owner && BodyHasAltPassive(owner) && SkillsmasArtificerExtendedElementCounter.lookupDictionary.TryGetValue(owner, out var customElementCounter))
                {
                    barrierPickup.hardenBuffDuration = DamageTypes.Crystallize.energeticResonanceDuration * (int)customElementCounter.rockPower;
                }
            }
        }

        internal static int GetWaterPower(GameObject bodyGameObject)
        {
            if (bodyGameObject && SkillsmasArtificerExtendedElementCounter.lookupDictionary.TryGetValue(bodyGameObject, out var customElementCounter))
            {
                return (int)customElementCounter.waterPower;
            }
            return 0;
        }

        internal class SkillsmasArtificerExtendedElementCounter : MonoBehaviour
        {
            public static Dictionary<GameObject, SkillsmasArtificerExtendedElementCounter> lookupDictionary = new Dictionary<GameObject, SkillsmasArtificerExtendedElementCounter>();

            public ArtificerExtended.Components.ElementCounter.Power rockPower;
            public ArtificerExtended.Components.ElementCounter.Power waterPower;

            public void OnEnable()
            {
                lookupDictionary[gameObject] = this;
            }

            public void OnDisable()
            {
                lookupDictionary.Remove(gameObject);
            }
        }
    }
}