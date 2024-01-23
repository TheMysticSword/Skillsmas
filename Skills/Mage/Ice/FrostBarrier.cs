using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Mage.Ice
{
    public class FrostBarrier : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> barrier = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Barrier",
            12f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SPECIAL_ICE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Damage",
            800f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SPECIAL_ICE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> radius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Radius",
            14f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> selfFreezeDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Self Freeze Duration",
            0.4f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> enemyFreezeDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Flash Frost",
            "Enemy Freeze Duration",
            2f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_FrostBarrier";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SPECIAL_ICE_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SPECIAL_ICE_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_FREEZING"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/FlashFrost.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FrostBarrierState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Flash Frost",
                baseRechargeInterval: 8f,
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
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FrostBarrierState));

            FrostBarrierState.explosionEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteIce/AffixWhiteExplosion.prefab").WaitForCompletion();
            FrostBarrierState.muzzleflashEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageIceLarge.prefab").WaitForCompletion();
        }

        public class FrostBarrierState : EntityStates.BaseState
        {
            public static GameObject explosionEffectPrefab;
            public static GameObject muzzleflashEffect;
            public static float baseDuration = 0.1f;

            public float duration;
            public int totalFrozenTargets = 0;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;
                var explosionRadius = radius + characterBody.radius;

                PlayAnimation("Gesture, Additive", "FireWall");

                EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
                {
                    origin = transform.position,
                    rotation = Util.QuaternionSafeLookRotation(transform.forward),
                    scale = explosionRadius
                }, false);

                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleRight", false);

                if (NetworkServer.active)
                {
                    FreezeBodyServer(characterBody, selfFreezeDuration);

                    var crit = RollCrit();

                    var search = new BullseyeSearch
                    {
                        searchOrigin = characterBody.corePosition,
                        maxDistanceFilter = explosionRadius,
                        filterByDistinctEntity = true,
                        teamMaskFilter = TeamMask.GetUnprotectedTeams(teamComponent.teamIndex)
                    };
                    search.RefreshCandidates();
                    var results = search.GetResults();
                    foreach (var result in results)
                    {
                        FreezeBodyServer(result.healthComponent.body, enemyFreezeDuration);

                        var damageInfo = new DamageInfo
                        {
                            damage = damage / 100f * damageStat,
                            attacker = gameObject,
                            procCoefficient = procCoefficient,
                            position = result.transform.position,
                            crit = crit
                        };
                        result.healthComponent.TakeDamage(damageInfo);
                        GlobalEventManager.instance.OnHitEnemy(damageInfo, result.healthComponent.gameObject);
                        GlobalEventManager.instance.OnHitAll(damageInfo, result.healthComponent.gameObject);
                    }

                    healthComponent.AddBarrier(totalFrozenTargets * barrier / 100f * healthComponent.fullBarrier);
                }

                if (SkillsmasPlugin.artificerExtendedEnabled)
                    SoftDependencies.ArtificerExtendedSupport.TriggerAltPassiveSkillCast(outer.gameObject);
            }

            public void FreezeBodyServer(CharacterBody targetBody, float duration)
            {
                var setStateOnHurt = targetBody.GetComponent<SetStateOnHurt>();
                if (setStateOnHurt && setStateOnHurt.targetStateMachine && setStateOnHurt.spawnedOverNetwork && setStateOnHurt.canBeFrozen)
                {
                    setStateOnHurt.SetFrozen(duration);
                    totalFrozenTargets++;
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && age >= duration)
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