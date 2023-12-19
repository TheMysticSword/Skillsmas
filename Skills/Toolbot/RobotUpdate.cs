using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Toolbot
{
    public class RobotUpdate : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> updateDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Update Mode",
            "Update Duration",
            5f,
            stringsToAffect: new List<string>
            {
                "TOOLBOT_SKILLSMAS_ROBOTUPDATE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> updateSpeedMultiplier = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Update Mode",
            "Update Speed Multiplier",
            100f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> healing = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Update Mode",
            "Healing",
            10f,
            stringsToAffect: new List<string>
            {
                "TOOLBOT_SKILLSMAS_ROBOTUPDATE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> attackSpeed = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Update Mode",
            "Attack Speed",
            50f,
            stringsToAffect: new List<string>
            {
                "TOOLBOT_SKILLSMAS_ROBOTUPDATE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> buffDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Update Mode",
            "Buff Duration",
            10f,
            stringsToAffect: new List<string>
            {
                "TOOLBOT_SKILLSMAS_ROBOTUPDATE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RobotUpdate";
            skillDef.skillNameToken = "TOOLBOT_SKILLSMAS_ROBOTUPDATE_NAME";
            skillDef.skillDescriptionToken = "TOOLBOT_SKILLSMAS_ROBOTUPDATE_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/Skills/MUL-T/RobotUpdate/texRobotUpdateBuff.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ToolbotUpdating));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "MUL-T: Update Mode",
                baseRechargeInterval: 10f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: false,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Toolbot/ToolbotBodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ToolbotUpdating));

            ToolbotUpdating.endEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/MUL-T/RobotUpdate/RobotUpdateFinish.prefab");
            ToolbotUpdating.endEffectPrefab.AddComponent<EffectComponent>();
            var vfxAttributes = ToolbotUpdating.endEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            SkillsmasContent.Resources.effectPrefabs.Add(ToolbotUpdating.endEffectPrefab);
        }

        public class ToolbotUpdating : EntityStates.BaseCharacterMain
        {
            public static GameObject startEffectPrefab;
            public static GameObject endEffectPrefab;

            public float duration;
            public Vector3 idealDirection;
            public uint soundID;
            public bool buffGranted = false;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = updateDuration / attackSpeedStat;

                if (isAuthority)
                {
                    void CancelSkill(GenericSkill skillSlot)
                    {
                        if (skillSlot && skillSlot.stateMachine.state.GetType() == skillSlot.activationState.stateType)
                        {
                            skillSlot.stateMachine.SetNextStateToMain();
                        }
                    }
                    CancelSkill(skillLocator.primary);
                    CancelSkill(skillLocator.secondary);

                    if (inputBank)
                    {
                        idealDirection = inputBank.aimDirection;
                        idealDirection.y = 0f;
                    }
                    UpdateDirection();
                }
                if (modelLocator) modelLocator.normalizeToFloor = true;
                if (characterDirection) characterDirection.forward = idealDirection;

                if (startEffectPrefab && characterBody)
                {
                    EffectManager.SpawnEffect(startEffectPrefab, new EffectData
                    {
                        origin = characterBody.corePosition
                    }, false);
                }
                soundID = Util.PlaySound("Play_MULT_shift_start", gameObject);
                PlayCrossfade("Body", "BoxModeEnter", 0.1f);
                PlayCrossfade("Stance, Override", "PutAwayGun", 0.1f);
                modelAnimator.SetFloat("aimWeight", 0f);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (fixedAge >= duration)
                {
                    if (!buffGranted)
                    {
                        buffGranted = true;

                        if (NetworkServer.active)
                        {
                            if (characterBody.healthComponent)
                                characterBody.healthComponent.Heal(healing / 100f * characterBody.healthComponent.fullHealth, default);
                            characterBody.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_ToolbotUpdated, buffDuration);
                        }
                    }
                    outer.SetNextStateToMain();
                    return;
                }

                if (isAuthority)
                {
                    if (characterBody) characterBody.isSprinting = false;
                    if (skillLocator.special && inputBank.skill4.down) skillLocator.special.ExecuteIfReady();
                    UpdateDirection();
                    if (characterDirection)
                    {
                        characterDirection.moveVector = idealDirection;
                        if (characterMotor && !characterMotor.disableAirControlUntilCollision)
                        {
                            characterMotor.rootMotion += GetIdealVelocity() * Time.fixedDeltaTime;
                        }
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
                return characterDirection.forward * characterBody.moveSpeed * updateSpeedMultiplier / 100f;
            }

            public override void OnExit()
            {
                AkSoundEngine.StopPlayingID(soundID);
                Util.PlaySound("Play_MULT_shift_end", gameObject);
                if (!outer.destroying && characterBody)
                {
                    if (endEffectPrefab)
                    {
                        EffectManager.SpawnEffect(endEffectPrefab, new EffectData
                        {
                            origin = characterBody.corePosition
                        }, false);
                    }
                    PlayAnimation("Body", "BoxModeExit");
                    PlayCrossfade("Stance, Override", "Empty", 0.1f);
                }
                if (modelLocator) modelLocator.normalizeToFloor = false;
                modelAnimator.SetFloat("aimWeight", 1f);

                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Frozen;
            }
        }
    }
}