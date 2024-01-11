using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using RoR2.Orbs;

namespace Skillsmas.Skills.Croco
{
    public class PoisonExtension : BaseSkill
    {
        public static DamageAPI.ModdedDamageType poisonExtensionFirstHitDamageType;
        public static GameObject projectilePrefab;

        public static ConfigOptions.ConfigurableValue<float> durationMultiplier = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Acrid: Neutropenia",
            "Duration Multiplier",
            4f,
            stringsToAffect: new List<string>
            {
                "CROCO_SKILLSMAS_POISONEXTENSION_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<int> maxTargets = ConfigOptions.ConfigurableValue.CreateInt(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Acrid: Neutropenia",
            "Max Targets",
            20,
            stringsToAffect: new List<string>
            {
                "CROCO_SKILLSMAS_POISONEXTENSION_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> range = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Acrid: Neutropenia",
            "Range",
            30f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            poisonExtensionFirstHitDamageType = DamageAPI.ReserveDamageType();
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_PoisonExtension";
            skillDef.skillNameToken = "CROCO_SKILLSMAS_POISONEXTENSION_NAME";
            skillDef.skillDescriptionToken = "CROCO_SKILLSMAS_POISONEXTENSION_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Neutropenia.png");
            skillDef.activationStateMachineName = "Mouth";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FirePoisonExtensionProjectile));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Acrid: Neutropenia",
                baseRechargeInterval: 10f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FirePoisonExtensionProjectile));

            projectilePrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoDiseaseProjectile.prefab").WaitForCompletion(), "Skillsmas_PoisonExtensionProjectile");
            projectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(poisonExtensionFirstHitDamageType);
            Object.Destroy(projectilePrefab.GetComponent<ProjectileProximityBeamController>());
            Object.Destroy(projectilePrefab.GetComponent<ProjectileStickOnImpact>());
            var impactExplosion = projectilePrefab.GetComponent<ProjectileImpactExplosion>();
            var singleTargetImpact = projectilePrefab.AddComponent<ProjectileSingleTargetImpact>();
            singleTargetImpact.impactEffect = impactExplosion.impactEffect;
            singleTargetImpact.destroyOnWorld = true;
            singleTargetImpact.hitSoundString = "Play_item_proc_behemoth";
            Object.Destroy(impactExplosion);
            SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);

            GenericGameEvents.OnTakeDamage += GenericGameEvents_OnTakeDamage;
        }

        private void GenericGameEvents_OnTakeDamage(DamageReport damageReport)
        {
            if (NetworkServer.active && damageReport.damageInfo != null)
            {
                if (damageReport.damageInfo.HasModdedDamageType(poisonExtensionFirstHitDamageType) && damageReport.victimBody)
                {
                    var orb = new PoisonExtensionOrb();

                    var dotController = DotController.FindDotController(damageReport.victimBody.gameObject);
                    if (dotController)
                    {
                        for (var i = dotController.dotStackList.Count - 1; i >= 0; i--)
                        {
                            DotController.DotStack dotStack = dotController.dotStackList[i];
                            if (dotStack.dotIndex == DotController.DotIndex.Poison || dotStack.dotIndex == DotController.DotIndex.Blight)
                            {
                                orb.stackInfo.Add(new PoisonExtensionStackInfo
                                {
                                    dotIndex = dotStack.dotIndex,
                                    duration = dotStack.timer * durationMultiplier
                                });
                                dotController.RemoveDotStackAtServer(i);
                            }
                        }
                    }

                    if (orb.stackInfo.Count > 0)
                    {
                        orb.bouncedObjects = new List<HealthComponent>();
                        orb.attacker = damageReport.attacker;
                        orb.inflictor = damageReport.damageInfo.inflictor;
                        orb.teamIndex = damageReport.attackerTeamIndex;
                        orb.damageValue = 0f;
                        orb.isCrit = false;
                        orb.origin = damageReport.damageInfo.position;
                        orb.bouncesRemaining = maxTargets;
                        orb.lightningType = LightningOrb.LightningType.CrocoDisease;
                        orb.procCoefficient = 0f;
                        orb.target = damageReport.victimBody.mainHurtBox;
                        orb.damageColorIndex = DamageColorIndex.Poison;
                        orb.range = range;
                        orb.firstHit = true;
                        OrbManager.instance.AddOrb(orb);
                        OrbManager.instance.ForceImmediateArrival(orb);
                    }
                }
            }
        }

        public class FirePoisonExtensionProjectile : EntityStates.Croco.FireSpit
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Croco.FireDiseaseProjectile));
                projectilePrefab = PoisonExtension.projectilePrefab;
                baseDuration = 0.5f;
                damageCoefficient = 0f;
                force = 0f;
                attackString = "Play_acrid_R_shoot";
                recoilAmplitude = 2f;
                bloom = 1f;
                base.OnEnter();
            }
        }

        public class PoisonExtensionStackInfo
        {
            public DotController.DotIndex dotIndex;
            public float duration = 0f;
        }

        public class PoisonExtensionOrb : LightningOrb
        {
            public bool firstHit = false;
            public List<PoisonExtensionStackInfo> stackInfo = new List<PoisonExtensionStackInfo>();

            public override void Begin()
            {
                duration = 0.6f;
                targetsToFindPerBounce = 2;
                if (!firstHit)
                {
                    EffectData effectData = new EffectData
                    {
                        origin = origin,
                        genericFloat = duration
                    };
                    effectData.SetHurtBoxReference(target);
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/CrocoDiseaseOrbEffect"), effectData, true);
                }
            }

            public override void OnArrival()
            {
                if (target)
                {
                    if (!firstHit)
                    {
                        var healthComponent = target.healthComponent;
                        if (healthComponent)
                        {
                            var body = healthComponent.body;
                            foreach (var x in stackInfo)
                            {
                                DotController.InflictDot(body.gameObject, attacker, x.dotIndex, x.duration);
                            }
                        }
                    }
                    
                    if (bouncesRemaining > 0)
                    {
                        for (var i = 0; i < targetsToFindPerBounce; i++)
                        {
                            if (bouncedObjects != null)
                            {
                                if (canBounceOnSameTarget)
                                {
                                    bouncedObjects.Clear();
                                }
                                bouncedObjects.Add(target.healthComponent);
                            }
                            var nextTarget = PickNextTarget(target.transform.position);
                            if (nextTarget)
                            {
                                var orb = new PoisonExtensionOrb
                                {
                                    search = search,
                                    origin = target.transform.position,
                                    target = nextTarget,
                                    attacker = attacker,
                                    inflictor = inflictor,
                                    teamIndex = teamIndex,
                                    damageValue = 0f,
                                    bouncesRemaining = bouncesRemaining - 1,
                                    isCrit = isCrit,
                                    bouncedObjects = bouncedObjects,
                                    lightningType = lightningType,
                                    procChainMask = procChainMask,
                                    procCoefficient = 0f,
                                    damageColorIndex = damageColorIndex,
                                    damageCoefficientPerBounce = 0f,
                                    speed = speed,
                                    range = range,
                                    damageType = damageType,
                                    failedToKill = true,
                                    stackInfo = stackInfo
                                };
                                OrbManager.instance.AddOrb(orb);
                            }
                        }
                        return;
                    }
                }
            }
        }
    }
}