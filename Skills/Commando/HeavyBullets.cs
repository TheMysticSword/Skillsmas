using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using MysticsRisky2Utils.MonoBehaviours;

namespace Skillsmas.Skills.Commando
{
    public class HeavyBullets : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Heavy Bullets",
            "Damage",
            350f,
            stringsToAffect: new List<string>
            {
                "COMMANDO_SKILLSMAS_HEAVYBULLETS_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Heavy Bullets",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> shotsPerSecond = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Heavy Bullets",
            "Shots Per Second",
            600f / 350f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public static GameObject tracerEffectPrefab;
        public static GameObject hitEffectPrefab;

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_HeavyBullets";
            skillDef.skillNameToken = "COMMANDO_SKILLSMAS_HEAVYBULLETS_NAME";
            skillDef.skillDescriptionToken = "COMMANDO_SKILLSMAS_HEAVYBULLETS_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/HeavyBullets.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(SlowFirePistol));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Commando: Heavy Bullets",
                baseRechargeInterval: 0f,
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
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var customSkillDef = skillDef as SteppedSkillDef;
            customSkillDef.stepCount = 2;
            customSkillDef.stepGraceDuration = 0.1f;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Commando/CommandoBodyPrimaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(SlowFirePistol));

            tracerEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Commando/HeavyBullets/TracerCommandoHeavyBullet.prefab");
            tracerEffectPrefab.AddComponent<EffectComponent>();
            var tracer = tracerEffectPrefab.AddComponent<Tracer>();
            tracer.headTransform = tracerEffectPrefab.transform.Find("TracerHead");
            tracer.tailTransform = tracerEffectPrefab.transform.Find("TracerTail");
            tracer.speed = 250;
            tracer.length = 12;
            var beamPointsFromTransforms = tracerEffectPrefab.AddComponent<BeamPointsFromTransforms>();
            beamPointsFromTransforms.target = tracerEffectPrefab.GetComponent<LineRenderer>();
            beamPointsFromTransforms.pointTransforms = new[]
            {
                tracer.headTransform,
                tracer.tailTransform
            };
            tracerEffectPrefab.AddComponent<EventFunctions>();
            var tracerOnTailReachedDefaults = tracerEffectPrefab.AddComponent<MysticsRisky2UtilsTracerOnTailReachedDefaults>();
            tracerOnTailReachedDefaults.destroySelf = true;
            SkillsmasContent.Resources.effectPrefabs.Add(tracerEffectPrefab);

            hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/HitsparkCommando.prefab").WaitForCompletion();
        }

        public class SlowFirePistol : EntityStates.BaseSkillState, SteppedSkillDef.IStepSetter
        {
            public static float recoilAmplitude = 2f;

            public float duration;
            public int step;
            public Ray aimRay;

            void SteppedSkillDef.IStepSetter.SetStep(int i)
            {
                step = i;
            }

            public override void OnEnter()
            {
                base.OnEnter();
                duration = 1f / shotsPerSecond / attackSpeedStat;
                aimRay = GetAimRay();
                StartAimMode(aimRay, 3f, false);
                if (step % 2 == 0)
                {
                    PlayAnimation("Gesture Additive, Left", "FirePistol, Left");
                    FireBullet("MuzzleLeft");
                }
                else
                {
                    PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
                    FireBullet("MuzzleRight");
                }
            }

            public void FireBullet(string targetMuzzle)
            {
                Util.PlayAttackSpeedSound("Play_commando_M1", gameObject, 1.75f);
                if (EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab)
                    EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, gameObject, targetMuzzle, false);
                AddRecoil(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
                characterBody.AddSpreadBloom(0.6f);

                if (isAuthority)
                {
                    new BulletAttack
                    {
                        owner = gameObject,
                        weapon = gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0f,
                        maxSpread = characterBody.spreadBloomAngle,
                        damage = damage / 100f * damageStat,
                        force = 100f,
                        tracerEffectPrefab = tracerEffectPrefab,
                        muzzleName = targetMuzzle,
                        hitEffectPrefab = hitEffectPrefab,
                        isCrit = characterBody.RollCrit(),
                        radius = 0.1f,
                        smartCollision = true
                    }.Fire();
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }
    }
}