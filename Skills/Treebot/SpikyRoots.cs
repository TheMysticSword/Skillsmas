using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Skillsmas.Skills.Treebot
{
    public class SpikyRoots : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "REX: Uprooting",
            "Damage",
            240f,
            stringsToAffect: new List<string>
            {
                "TREEBOT_SKILLSMAS_SPIKYROOTS_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> hpCost = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "REX: Uprooting",
            "HP Cost",
            10f,
            stringsToAffect: new List<string>()
            {
                "TREEBOT_SKILLSMAS_SPIKYROOTS_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> healing;

        public static GameObject projectilePrefab;
        public static GameObject rootSpawnEffectPrefab;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab = Utils.CreateBlankPrefab("Skillsmas_SpikyRootsProjectile", true);
            projectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_SpikyRoots";
            skillDef.skillNameToken = "TREEBOT_SKILLSMAS_SPIKYROOTS_NAME";
            skillDef.skillDescriptionToken = "TREEBOT_SKILLSMAS_SPIKYROOTS_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_PERCENT_HP"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Uprooting.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireRoot));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "REX: Uprooting",
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
            customSkillDef.stepCount = 3;
            customSkillDef.stepGraceDuration = 60f;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Treebot/TreebotBodyPrimaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireRoot));

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/REX/SpikyRoots/SpikyRootGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            var objectTransformCurve = ghost.transform.Find("Pivot").gameObject.AddComponent<ObjectTransformCurve>();
            objectTransformCurve.translationCurveX = AnimationCurve.Constant(0f, 1f, 0f);
            objectTransformCurve.translationCurveY = AnimationCurve.EaseInOut(0f, -9f, 1f, 0f);
            objectTransformCurve.translationCurveZ = AnimationCurve.Constant(0f, 1f, 0f);
            objectTransformCurve.useTranslationCurves = true;
            objectTransformCurve.useRotationCurves = false;
            objectTransformCurve.loop = false;
            objectTransformCurve.timeMax = 0.17f;
            var objectScaleCurve = ghost.transform.Find("Pivot/Scaler").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.useOverallCurveOnly = false;
            objectScaleCurve.curveX = AnimationCurve.EaseInOut(0.8f, 1f, 1f, 0f);
            objectScaleCurve.curveY = AnimationCurve.EaseInOut(0.5f, 1f, 1f, 0.3f);
            objectScaleCurve.curveZ = AnimationCurve.EaseInOut(0.8f, 1f, 1f, 0f);
            objectScaleCurve.overallCurve = AnimationCurve.Constant(0f, 1f, 1f);
            objectScaleCurve.timeMax = 0.4f;

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/REX/SpikyRoots/SpikyRootProjectile.prefab"), projectilePrefab);
            var projectileController = projectilePrefab.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "REX: Uprooting",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            projectilePrefab.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = projectilePrefab.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 0f;
            projectileSimple.lifetime = 0.4f;
            var projectileDamage = projectilePrefab.AddComponent<ProjectileDamage>();
            var hitboxGroup = projectilePrefab.AddComponent<HitBoxGroup>();
            hitboxGroup.groupName = "Root";
            hitboxGroup.hitBoxes = new[]
            {
                projectilePrefab.transform.Find("HitBox").gameObject.AddComponent<HitBox>(),
                projectilePrefab.transform.Find("HitBox (1)").gameObject.AddComponent<HitBox>()
            };
            var projectileOverlapAttack = projectilePrefab.AddComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.damageCoefficient = 1f;
            projectileOverlapAttack.resetInterval = -1f;
            projectileOverlapAttack.overlapProcCoefficient = 1f;
            projectileOverlapAttack.fireFrequency = 20f;
            projectileOverlapAttack.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/OmniImpactVFXSlashSyringe.prefab").WaitForCompletion();
            var healOwner = projectilePrefab.AddComponent<SkillsmasProjectileHealOwnerOnDamageInflicted>();
            healOwner.maxInstancesOfHealing = 1;
            var inflictTimedBuff = projectilePrefab.AddComponent<ProjectileInflictTimedBuff>();
            RoR2Application.onLoad += () => inflictTimedBuff.buffDef = RoR2Content.Buffs.Entangle;
            inflictTimedBuff.duration = 0.5f;
            var soundAdder = projectilePrefab.AddComponent<SkillsmasProjectileSoundAdder>();
            soundAdder.projectileOverlapAttack = projectileOverlapAttack;
            soundAdder.impactSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            soundAdder.impactSoundEventDef.eventName = "Play_treeBot_m1_impact";
            SkillsmasContent.Resources.networkSoundEventDefs.Add(soundAdder.impactSoundEventDef);

            healing = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "REX: Uprooting",
                "Healing",
                5f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => healOwner.fractionalHealing = newValue / 100f
            );

            SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);

            rootSpawnEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotMortarMuzzleflash.prefab").WaitForCompletion();
        }

        public class FireRoot : EntityStates.BaseSkillState, SteppedSkillDef.IStepSetter
        {
            public static float distanceInitial = 5f;
            public static float distanceBetweenRoots = 6f;
            public static int rootCount = 4;
            public static float floorMaxDistance = 5f;
            public static float baseDuration = 0.6f;
            public static float firingBaseDuration = 0.2f;

            public float duration;
            public int step;
            public float fireInterval;
            public float fireTimer;
            public List<Vector3> rootPositions;
            public int rootsFired = 0;

            void SteppedSkillDef.IStepSetter.SetStep(int i)
            {
                step = i;
            }

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;
                var firingDuration = firingBaseDuration / attackSpeedStat;

                Util.PlayAttackSpeedSound("Play_treeBot_R_yank", gameObject, 1.8f);
                PlayAnimation("Gesture, Additive", "FireBomb", "FireBomb.playbackRate", firingDuration);

                if (NetworkServer.active && healthComponent)
                {
                    healthComponent.TakeDamage(new DamageInfo
                    {
                        damage = healthComponent.combinedHealth * hpCost / 100f,
                        position = characterBody.corePosition,
                        attacker = null,
                        inflictor = null,
                        damageType = DamageType.NonLethal | DamageType.BypassArmor,
                        procCoefficient = 0f
                    });
                }

                if (isAuthority)
                {
                    fireInterval = firingDuration / rootCount;
                    rootPositions = new List<Vector3>();

                    var footPosition = characterBody.footPosition;
                    var aimRay = GetAimRay();
                    var forwardDirection = aimRay.direction;
                    forwardDirection.y = 0f;
                    forwardDirection.Normalize();

                    var rootPosition = footPosition + distanceInitial * forwardDirection;
                    var nextRootsShouldFloat = false;
                    var floorUpDistance = 20f;

                    for (var i = 0; i < rootCount; i++)
                    {
                        if (!nextRootsShouldFloat)
                        {
                            if (Physics.Raycast(new Ray(rootPosition + Vector3.up * floorUpDistance, Vector3.down), out var raycastInfo, floorUpDistance + floorMaxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                                rootPosition = raycastInfo.point;
                            else
                                nextRootsShouldFloat = true;
                        }

                        rootPositions.Add(rootPosition);

                        rootPosition += distanceBetweenRoots * forwardDirection;
                    }
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority)
                {
                    if (rootsFired < rootCount)
                    {
                        fireTimer -= Time.fixedDeltaTime;
                        while (fireTimer <= 0)
                        {
                            fireTimer += fireInterval;
                            FireRootAuthority();
                        }
                    }

                    if (fixedAge >= duration)
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }

            public void FireRootAuthority()
            {
                var rootPosition = rootPositions[rootsFired % rootPositions.Count];
                var rootRotation = Quaternion.AngleAxis(RoR2Application.rng.RangeFloat(0f, 360f), Vector3.up) * Vector3.forward;

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = projectilePrefab,
                    position = rootPosition,
                    rotation = Util.QuaternionSafeLookRotation(rootRotation),
                    damage = damage / 100f * damageStat,
                    crit = characterBody.RollCrit(),
                    force = 0f,
                    owner = gameObject
                });

                if (rootSpawnEffectPrefab)
                {
                    EffectManager.SpawnEffect(rootSpawnEffectPrefab, new EffectData
                    {
                        origin = rootPosition,
                        rotation = Util.QuaternionSafeLookRotation(Vector3.up)
                    }, true);
                }

                rootsFired++;
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }
    }
}