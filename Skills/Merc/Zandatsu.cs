using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Merc
{
    public class Zandatsu : BaseSkill
    {
        public static DamageAPI.ModdedDamageType zandatsuDamageType;
        public static GameObject killEffectPrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Zandatsu",
            "Damage",
            600f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_ZANDATSU_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Zandatsu",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> executeThreshold = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Zandatsu",
            "Execute Threshold",
            10f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_ZANDATSU_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> healing = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Zandatsu",
            "Healing",
            30f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_ZANDATSU_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            zandatsuDamageType = DamageAPI.ReserveDamageType();
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_Zandatsu";
            skillDef.skillNameToken = "MERC_SKILLSMAS_ZANDATSU_NAME";
            skillDef.skillDescriptionToken = "MERC_SKILLSMAS_ZANDATSU_DESCRIPTION";
            skillDef.icon = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/AttackSpeedOnCrit/texBuffAttackSpeedOnCritIcon.tif").WaitForCompletion();
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ZandatsuDash));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Mercenary: Zandatsu",
                baseRechargeInterval: 6f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Merc/MercBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            killEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/Bandit2ResetEffect.prefab").WaitForCompletion();

            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            GenericGameEvents.OnApplyDamageExecutions += GenericGameEvents_OnApplyDamageExecutions;
        }

        private void GenericGameEvents_OnApplyDamageExecutions(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo, float damage, ref float executionThreshold, ref GameObject executionEffectPrefab, ref bool forceExecution)
        {
            var num = executeThreshold / 100f;
            if (damageInfo.HasModdedDamageType(zandatsuDamageType) && executionThreshold < num)
            {
                executionThreshold = num;
                executionEffectPrefab = null;
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
        {
            if (damageReport.damageInfo != null && damageReport.damageInfo.HasModdedDamageType(zandatsuDamageType))
            {
                EffectManager.SpawnEffect(killEffectPrefab, new EffectData
                {
                    origin = damageReport.damageInfo.position
                }, true);

                if (damageReport.attackerBody)
                {
                    damageReport.attackerBody.healthComponent.Heal(healing / 100f * damageReport.attackerBody.healthComponent.fullHealth, default);
                }
            }
        }

        public class ZandatsuDash : EntityStates.Merc.EvisDash
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Merc.EvisDash));
                base.OnEnter();
                if (NetworkServer.active) characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
                outer.nextStateModifier += ModifyBodyNextState;
            }

            public void ModifyBodyNextState(EntityStateMachine entityStateMachine, ref EntityStates.EntityState newNextState)
            {
                outer.nextStateModifier -= ModifyBodyNextState;
                newNextState = new ZandatsuHit();
            }

            public override void OnExit()
            {
                if (NetworkServer.active) characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                base.OnExit();
            }
        }

        public class ZandatsuHit : EntityStates.Merc.Evis
        {
            public static GameObject explosionEffectPrefab;
            public bool hitDone = false;
            public float attackStopwatch2 = 0f;
            public float stopwatch2 = 0f;

            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Merc.Evis));
                base.OnEnter();
                if (NetworkServer.active) characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
            }

            public override void FixedUpdate()
            {
                stopwatch = -Time.fixedDeltaTime;
                attackStopwatch = -Time.fixedDeltaTime;
                base.FixedUpdate();

                stopwatch2 += Time.fixedDeltaTime;
                attackStopwatch2 += Time.fixedDeltaTime;

                var attackInterval = 1f / damageFrequency / attackSpeedStat;
                if (attackStopwatch2 >= attackInterval)
                {
                    attackStopwatch2 -= attackInterval;

                    var target = SearchForTarget();
                    if (target)
                    {
                        Util.PlayAttackSpeedSound(slashSoundString, gameObject, slashPitch);
                        Util.PlaySound(dashSoundString, gameObject);
                        Util.PlaySound(impactSoundString, gameObject);
                        var hurtBoxGroup = target.hurtBoxGroup;
                        target = hurtBoxGroup.hurtBoxes[Random.Range(0, hurtBoxGroup.hurtBoxes.Length - 1)];
                        if (target)
                        {
                            var randomCircle = Random.insideUnitCircle.normalized;
                            EffectManager.SimpleImpactEffect(
                                hitEffectPrefab,
                                target.transform.position,
                                new Vector3(randomCircle.x, 0f, randomCircle.y),
                                false
                            );

                            var targetTransform = hurtBoxGroup.transform;
                            var temporaryOverlay = targetTransform.gameObject.AddComponent<TemporaryOverlay>();
                            temporaryOverlay.duration = attackInterval;
                            temporaryOverlay.animateShaderAlpha = true;
                            temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                            temporaryOverlay.destroyComponentOnEnd = true;
                            temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
                            temporaryOverlay.AddToCharacerModel(targetTransform.GetComponent<CharacterModel>());

                            if (NetworkServer.active)
                            {
                                var damageInfo = new DamageInfo
                                {
                                    damage = damage / 100f * damageStat,
                                    attacker = gameObject,
                                    procCoefficient = procCoefficient,
                                    position = target.transform.position,
                                    crit = crit
                                };
                                damageInfo.AddModdedDamageType(zandatsuDamageType);
                                target.healthComponent.TakeDamage(damageInfo);
                                GlobalEventManager.instance.OnHitEnemy(damageInfo, target.healthComponent.gameObject);
                                GlobalEventManager.instance.OnHitAll(damageInfo, target.healthComponent.gameObject);
                            }

                            hitDone = true;
                        }
                    }
                    else if (isAuthority && stopwatch2 > minimumDuration)
                    {
                        outer.SetNextStateToMain();
                    }
                }

                if (isAuthority && hitDone) outer.SetNextStateToMain();
            }

            public override void OnExit()
            {
                var _lingeringInvincibilityDuration = lingeringInvincibilityDuration;
                lingeringInvincibilityDuration = 0;
                if (NetworkServer.active) characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
                base.OnExit();
                lingeringInvincibilityDuration = _lingeringInvincibilityDuration;
            }
        }
    }
}