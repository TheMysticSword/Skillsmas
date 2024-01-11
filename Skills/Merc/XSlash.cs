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
    public class XSlash : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Mercenary: Crossing Storms",
            "Damage",
            1000f,
            stringsToAffect: new List<string>
            {
                "MERC_SKILLSMAS_XSLASH_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;

        public static GameObject projectilePrefab1;
        public static GameObject projectilePrefab2;

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab1 = Utils.CreateBlankPrefab("Skillsmas_XSlash1", true);
            projectilePrefab1.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            projectilePrefab2 = Utils.CreateBlankPrefab("Skillsmas_XSlash2", true);
            projectilePrefab2.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_XSlash";
            skillDef.skillNameToken = "MERC_SKILLSMAS_XSLASH_NAME";
            skillDef.skillDescriptionToken = "MERC_SKILLSMAS_XSLASH_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/CrossingStorms.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(PerformXSlash));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Mercenary: Crossing Storms",
                baseRechargeInterval: 6f,
                baseMaxStock: 2,
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
            customSkillDef.stepGraceDuration = 60f;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Merc/MercBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(PerformXSlash));

            for (var slashIndex = 1; slashIndex <= 2; slashIndex++)
            {
                var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>(string.Format("Assets/Mods/Skillsmas/Skills/Mercenary/XSlash/XSlash{0}Ghost.prefab", slashIndex));
                ghost.AddComponent<ProjectileGhostController>();
                var objectScaleCurve = ghost.transform.Find("Pivot/mdlSlash").gameObject.AddComponent<ObjectScaleCurve>();
                objectScaleCurve.overallCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f);
                objectScaleCurve.useOverallCurveOnly = true;
                objectScaleCurve.timeMax = 0.14f;
                var blink = ghost.AddComponent<BeginRapidlyActivatingAndDeactivating>();
                blink.blinkingRootObject = ghost.transform.Find("Pivot").gameObject;
                blink.delayBeforeBeginningBlinking = 0.3f;
                blink.blinkFrequency = 20f;

                var currentProjectilePrefab = projectilePrefab1;
                switch (slashIndex)
                {
                    case 1:
                        currentProjectilePrefab = projectilePrefab1;
                        break;
                    case 2:
                        currentProjectilePrefab = projectilePrefab2;
                        break;
                    default:
                        continue;
                }

                Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>(string.Format("Assets/Mods/Skillsmas/Skills/Mercenary/XSlash/XSlash{0}Projectile.prefab", slashIndex)), currentProjectilePrefab);
                var projectileController = currentProjectilePrefab.AddComponent<ProjectileController>();
                projectileController.allowPrediction = true;
                projectileController.ghostPrefab = ghost;
                currentProjectilePrefab.AddComponent<ProjectileNetworkTransform>();
                var projectileSimple = currentProjectilePrefab.AddComponent<ProjectileSimple>();
                projectileSimple.desiredForwardSpeed = 60f;
                projectileSimple.enableVelocityOverLifetime = true;
                projectileSimple.velocityOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                projectileSimple.lifetime = 0.5f;
                var projectileDamage = currentProjectilePrefab.AddComponent<ProjectileDamage>();
                var hitboxGroup = currentProjectilePrefab.AddComponent<HitBoxGroup>();
                hitboxGroup.groupName = "XSlash";
                hitboxGroup.hitBoxes = new[]
                {
                    currentProjectilePrefab.transform.Find("HitBox").gameObject.AddComponent<HitBox>()
                };
                var projectileOverlapAttack = currentProjectilePrefab.AddComponent<ProjectileOverlapAttack>();
                projectileOverlapAttack.damageCoefficient = 1f;
                projectileOverlapAttack.resetInterval = -1f;
                projectileOverlapAttack.overlapProcCoefficient = 1f;
                projectileOverlapAttack.fireFrequency = 20f;
                projectileOverlapAttack.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/ImpactMercAssaulter.prefab").WaitForCompletion();

                SkillsmasContent.Resources.projectilePrefabs.Add(currentProjectilePrefab);
            }

            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Mercenary: Crossing Storms",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    projectilePrefab1.GetComponent<ProjectileController>().procCoefficient = newValue;
                    projectilePrefab2.GetComponent<ProjectileController>().procCoefficient = newValue;
                }
            );
        }

        public class PerformXSlash : EntityStates.GenericProjectileBaseState, SteppedSkillDef.IStepSetter
        {
            public int step;

            void SteppedSkillDef.IStepSetter.SetStep(int i)
            {
                step = i;
            }

            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Merc.Weapon.ThrowEvisProjectile));
                damageCoefficient = damage / 100f;
                force = 0f;
                baseDuration = 0.7f;
                recoilAmplitude = 0f;
                attackSoundString = "Play_merc_m1_hard_swing";
                baseDelayBeforeFiringProjectile = 0.3f;

                base.OnEnter();

                projectilePrefab = projectilePrefab1;
                if ((step % 2) == 1) projectilePrefab = projectilePrefab2;

                if (isAuthority && characterMotor)
                {
                    characterMotor.Motor.ForceUnground();

                    var maxVelocity = 5f;
                    if (characterMotor.isGrounded) maxVelocity = 20f;
                    characterMotor.velocity = new Vector3(characterMotor.velocity.x, Mathf.Max(characterMotor.velocity.y, maxVelocity), characterMotor.velocity.z);
                }
            }

            public override void PlayAnimation(float duration)
            {
                var animationStateName = "";
                switch (step)
                {
                    case 0:
                        animationStateName = "GroundLight1";
                        break;
                    case 1:
                        animationStateName = "GroundLight2";
                        break;
                }

                PlayCrossfade("Gesture, Additive", animationStateName, "GroundLight.playbackRate", duration, 0.05f);
                PlayCrossfade("Gesture, Override", animationStateName, "GroundLight.playbackRate", duration, 0.05f);
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                if (firedProjectile) return EntityStates.InterruptPriority.PrioritySkill;
                return EntityStates.InterruptPriority.Pain;
            }
        }
    }
}