using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using RoR2.Audio;

namespace Skillsmas.Skills.Merc
{
    public class BladeRoll : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> maxDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Riptide",
            "Max Duration",
            7f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_BLADEROLL_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> maxCooldownIncrease = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Riptide",
            "Max Cooldown Increase",
            2f,
            description: "Example: if this is at 2x and the cooldown is 2.5s, the calculated cooldown will range between 2.5s and 7.5s.",
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Riptide",
            "Damage",
            220f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_BLADEROLL_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Riptide",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> speedMultiplier = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Riptide",
            "Speed Multiplier",
            300f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_BladeRoll";
            skillDef.skillNameToken = "MERC_SKILLSMAS_BLADEROLL_NAME";
            skillDef.skillDescriptionToken = "MERC_SKILLSMAS_BLADEROLL_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Riptide.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(BladeRollState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Mercenary: Riptide",
                baseRechargeInterval: 2.5f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 0,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: false
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Merc/MercBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(BladeRollState));

            BladeRollState.loopSoundDef = ScriptableObject.CreateInstance<LoopSoundDef>();
            BladeRollState.loopSoundDef.startSoundName = "Play_merc_R_slicingBlades_flight_loop";
            BladeRollState.loopSoundDef.stopSoundName = "Stop_merc_R_slicingBlades_flight_loop";

            BladeRollState.swirlEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/MercSwordSlashWhirlwind.prefab").WaitForCompletion();
            BladeRollState.hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/OmniImpactVFXSlashMerc.prefab").WaitForCompletion();
        }

        public class BladeRollState : EntityStates.BaseCharacterMain
        {
            public static GameObject startEffectPrefab;
            public static GameObject endEffectPrefab;
            public static GameObject swirlEffectPrefab;
            public static GameObject hitEffectPrefab;
            public static LoopSoundDef loopSoundDef;
            public static float minDuration = 0.5f;
            public static float baseAnimationDuration = 0.25f;
            public static float baseSwirlEffectInterval = 0.14f;
            public static float overlapAttackResetInterval = 1f;

            public float moveSpeedDifference;
            public Vector3 idealDirection;
            public LoopSoundManager.SoundLoopPtr soundLoopPtr;
            public float animationDuration;
            public float animationTimer;
            public float swirlEffectInterval;
            public float swirlEffectTimer;
            public OverlapAttack overlapAttack;
            public float overlapAttackResetTimer;

            public override void OnEnter()
            {
                base.OnEnter();

                moveSpeedDifference = moveSpeedStat / characterBody.baseMoveSpeed;
                animationDuration = baseAnimationDuration / moveSpeedDifference;
                swirlEffectInterval = baseSwirlEffectInterval / moveSpeedDifference;

                if (isAuthority)
                {
                    if (inputBank)
                    {
                        idealDirection = inputBank.aimDirection;
                        idealDirection.y = 0f;
                    }
                    UpdateDirection();
                }
                if (modelLocator) modelLocator.normalizeToFloor = true;
                if (characterDirection) characterDirection.forward = idealDirection;

                overlapAttack = InitMeleeOverlap(damage / 100f, hitEffectPrefab, GetModelTransform(), "WhirlwindAir");
                overlapAttack.procCoefficient = procCoefficient;

                if (startEffectPrefab && characterBody)
                {
                    EffectManager.SpawnEffect(startEffectPrefab, new EffectData
                    {
                        origin = characterBody.corePosition
                    }, false);
                }
                Util.PlaySound("Play_merc_R_dash", gameObject);
                soundLoopPtr = LoopSoundManager.PlaySoundLoopLocal(gameObject, loopSoundDef);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                animationTimer -= Time.fixedDeltaTime;
                if (animationTimer <= 0)
                {
                    animationTimer += animationDuration;

                    PlayCrossfade("FullBody, Override", "WhirlwindAir", "Whirlwind.playbackRate", animationDuration, 0.01f);
                }

                swirlEffectTimer -= Time.fixedDeltaTime;
                if (swirlEffectTimer <= 0)
                {
                    swirlEffectTimer += swirlEffectInterval;

                    EffectManager.SimpleMuzzleFlash(swirlEffectPrefab, gameObject, "WhirlwindAir", false);
                }

                if (fixedAge >= minDuration && (fixedAge >= maxDuration || !inputBank.skill2.down))
                {
                    outer.SetNextStateToMain();
                    return;
                }

                if (isAuthority)
                {
                    UpdateDirection();
                    if (characterDirection)
                    {
                        characterDirection.moveVector = idealDirection;
                        if (characterMotor && !characterMotor.disableAirControlUntilCollision)
                        {
                            characterMotor.rootMotion += GetIdealVelocity() * Time.fixedDeltaTime;
                        }
                    }

                    overlapAttackResetTimer += Time.fixedDeltaTime;
                    if (overlapAttackResetTimer >= overlapAttackResetInterval)
                    {
                        overlapAttackResetTimer -= overlapAttackResetInterval;

                        overlapAttack.ResetIgnoredHealthComponents();
                    }

                    if (overlapAttack.Fire())
                    {
                        Util.PlaySound("Play_merc_sword_impact", gameObject);
                    }
                }
            }

            public void UpdateDirection()
            {
                if (inputBank)
                {
                    var moveVector2D = Util.Vector3XZToVector2XY(inputBank.moveVector);
                    if (moveVector2D != Vector2.zero)
                    {
                        moveVector2D.Normalize();
                        idealDirection = new Vector3(moveVector2D.x, 0f, moveVector2D.y).normalized;
                    }
                }
            }

            public Vector3 GetIdealVelocity()
            {
                return characterDirection.forward * characterBody.moveSpeed * speedMultiplier / 100f;
            }

            public override void OnExit()
            {
                LoopSoundManager.StopSoundLoopLocal(soundLoopPtr);
                Util.PlaySound("Play_merc_R_end", gameObject);
                if (!outer.destroying && characterBody)
                {
                    if (endEffectPrefab)
                    {
                        EffectManager.SpawnEffect(endEffectPrefab, new EffectData
                        {
                            origin = characterBody.corePosition
                        }, false);
                    }
                    PlayAnimation("FullBody, Override", "WhirlwindAirExit");
                }
                if (modelLocator) modelLocator.normalizeToFloor = false;

                SmallHop(characterMotor, 7f);
                if (characterMotor && !characterMotor.disableAirControlUntilCollision)
                {
                    characterMotor.velocity += GetIdealVelocity();
                }

                if (isAuthority)
                {
                    var activatorSkillSlot = skillLocator.secondary;
                    if (activatorSkillSlot)
                    {
                        activatorSkillSlot.DeductStock(1);
                        activatorSkillSlot.rechargeStopwatch = Util.Remap(fixedAge, minDuration, maxDuration, 0f, 0f - activatorSkillSlot.finalRechargeInterval * maxCooldownIncrease);
                    }
                }

                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Pain;
            }
        }
    }
}