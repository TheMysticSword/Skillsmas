using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Commando
{
    public class JetUp : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Tactical Lift-Off",
            "Damage",
            500f,
            stringsToAffect: new List<string>
            {
                "COMMANDO_SKILLSMAS_JETUP_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Tactical Lift-Off",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> explosionRadius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Tactical Lift-Off",
            "Explosion Radius",
            7f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_JetUp";
            skillDef.skillNameToken = "COMMANDO_SKILLSMAS_JETUP_NAME";
            skillDef.skillDescriptionToken = "COMMANDO_SKILLSMAS_JETUP_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/LiftOff.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(JetUpState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Commando: Tactical Lift-Off",
                baseRechargeInterval: 4f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Commando/CommandoBodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(JetUpState));

            JetUpState.explosionEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LemurianBruiser/OmniExplosionVFXLemurianBruiserFireballImpact.prefab").WaitForCompletion();
            JetUpState.speedCoefficientCurve = AnimationCurve.EaseInOut(0f, 5.25f, 1f, 0f);
        }

        public class JetUpState : EntityStates.GenericCharacterMain
        {
            public static GameObject explosionEffectPrefab;
            public static AnimationCurve speedCoefficientCurve;

            public float baseDuration = 0.4f;
            public Vector3 flyVector = Vector3.up;

            public float duration;
            public Vector3 blastPosition;

            public override void OnEnter()
            {
                base.OnEnter();

                duration = baseDuration;

                characterMotor.Motor.ForceUnground();
                characterMotor.velocity.y = 0f;

                if (isAuthority)
                {
                    blastPosition = characterBody.corePosition;
                }
                if (NetworkServer.active)
                {
                    var blastAttack = new BlastAttack
                    {
                        radius = explosionRadius,
                        procCoefficient = procCoefficient,
                        position = blastPosition,
                        attacker = gameObject,
                        crit = RollCrit(),
                        baseDamage = damageStat * damage / 100f,
                        falloffModel = BlastAttack.FalloffModel.None,
                        baseForce = 700f,
                        teamIndex = teamComponent.teamIndex,
                        attackerFiltering = AttackerFiltering.NeverHitSelf,
                        damageType = DamageType.IgniteOnHit
                    };
                    blastAttack.Fire();
                }

                Util.PlaySound("Play_commando_M2_grenade_explo", gameObject);
                PlayAnimation("Body", "Jump");
                if (EntityStates.Commando.SlideState.jetEffectPrefab)
                {
                    var jetLeft = FindModelChild("LeftJet");
                    var jetRight = FindModelChild("RightJet");
                    if (jetLeft)
                        Object.Instantiate(EntityStates.Commando.SlideState.jetEffectPrefab, jetLeft);
                    if (jetRight)
                        Object.Instantiate(EntityStates.Commando.SlideState.jetEffectPrefab, jetRight);
                }

                if (explosionEffectPrefab)
                {
                    EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
                    {
                        origin = blastPosition,
                        scale = explosionRadius,
                        rotation = Util.QuaternionSafeLookRotation(flyVector)
                    }, false);
                }
            }

            public override void OnSerialize(NetworkWriter writer)
            {
                base.OnSerialize(writer);
                writer.Write(blastPosition);
            }

            public override void OnDeserialize(NetworkReader reader)
            {
                base.OnDeserialize(reader);
                blastPosition = reader.ReadVector3();
            }

            public override void HandleMovements()
            {
                base.HandleMovements();
                characterMotor.rootMotion += flyVector * (speedCoefficientCurve.Evaluate(fixedAge / duration) * moveSpeedStat * Time.fixedDeltaTime);
                characterMotor.velocity.y = 0f;
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (fixedAge >= duration && isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}