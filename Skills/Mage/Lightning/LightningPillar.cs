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
using System.Linq;
using RoR2.Orbs;

namespace Skillsmas.Skills.Mage.Lightning
{
    public class LightningPillar : BaseSkill
    {
        public static GameObject lightningPillarPrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Thunderbolt",
            "Damage",
            100f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_UTILITY_LIGHTNING_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Thunderbolt",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> zapInterval = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Thunderbolt",
            "Zap Interval",
            2.7f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> zapRadius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Thunderbolt",
            "Zap Radius",
            30f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<int> maxZaps = ConfigOptions.ConfigurableValue.CreateInt(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Thunderbolt",
            "Max Zaps",
            4,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            lightningPillarPrefab = Utils.CreateBlankPrefab("Skillsmas_LightningPillar", true);
            lightningPillarPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_LightningPillar";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_UTILITY_LIGHTNING_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_UTILITY_LIGHTNING_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_STUNNING"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Thunderbolt.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(PrepLightningPillar));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Artificer: Thunderbolt",
                baseRechargeInterval: 12f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: true,
                isCombatSkill: true,
                mustKeyPress: false
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(PrepLightningPillar));

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/LightningPillar/LightningPillar.prefab"), lightningPillarPrefab);
            var teamFilter = lightningPillarPrefab.AddComponent<TeamFilter>();
            var nbd = lightningPillarPrefab.AddComponent<NetworkedBodyAttachment>();
            nbd.shouldParentToAttachedBody = false;
            var lightningPillarController = lightningPillarPrefab.AddComponent<SkillsmasLightningPillarController>();
            lightningPillarController.lightningEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab").WaitForCompletion();
            lightningPillarController.lightningPrepareEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeOrbEffect.prefab").WaitForCompletion();
            
            PrepLightningPillar.areaIndicatorPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotMortarAreaIndicator.prefab").WaitForCompletion();
            PrepLightningPillar.muzzleflashEffectStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightning.prefab").WaitForCompletion();
            PrepLightningPillar.goodCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/SimpleDotCrosshair.prefab").WaitForCompletion();
            PrepLightningPillar.badCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/BadCrosshair.prefab").WaitForCompletion();
        }

        public class SkillsmasLightningPillarController : MonoBehaviour, INetworkedBodyAttachmentListener
        {
            public CharacterBody ownerBody;
            
            public float zapTimer = 0f;
            public float zapPrepareTime = 0.5f;
            public bool playedPrepareEffect = false;
            public int zapCount = 0;

            public BullseyeSearch enemySearch;
            
            public GameObject lightningEffect;
            public GameObject lightningPrepareEffect;

            public void Awake()
            {
                enemySearch = new BullseyeSearch
                {
                    filterByDistinctEntity = true,
                    maxDistanceFilter = zapRadius,
                    searchOrigin = transform.position,
                    sortMode = BullseyeSearch.SortMode.Distance,
                    teamMaskFilter = TeamMask.allButNeutral
                };
            }

            public void Start()
            {
                EffectManager.SpawnEffect(lightningEffect, new EffectData
                {
                    origin = transform.position
                }, false);
            }

            public void OnAttachedBodyDiscovered(NetworkedBodyAttachment networkedBodyAttachment, CharacterBody attachedBody)
            {
                ownerBody = attachedBody;
                enemySearch.teamMaskFilter.RemoveTeam(attachedBody.teamComponent.teamIndex);

                Zap();
            }

            public void FixedUpdate()
            {
                if (ownerBody)
                {
                    zapTimer += Time.fixedDeltaTime;
                    if (!playedPrepareEffect && zapTimer >= (zapInterval - zapPrepareTime))
                    {
                        playedPrepareEffect = true;
                        if (ownerBody.hasEffectiveAuthority)
                        {
                            EffectManager.SpawnEffect(lightningPrepareEffect, new EffectData
                            {
                                origin = transform.position,
                                scale = 1f,
                                genericFloat = zapPrepareTime
                            }, true);
                        }
                    }
                    if (zapTimer >= zapInterval)
                    {
                        zapTimer -= zapInterval;
                        Zap();
                        playedPrepareEffect = false;
                    }
                }
            }

            public void Zap()
            {
                if (NetworkServer.active)
                {
                    EffectManager.SpawnEffect(lightningEffect, new EffectData
                    {
                        origin = transform.position
                    }, true);

                    var crit = ownerBody.RollCrit();
                    var team = ownerBody.teamComponent.teamIndex;
                    var zapDamage = damage / 100f * ownerBody.damage;
                    var zapProcCoefficient = procCoefficient.Value;

                    enemySearch.RefreshCandidates();
                    var enemyHurtBoxes = enemySearch.GetResults();
                    foreach (var enemyHurtBox in enemyHurtBoxes)
                    {
                        var orb = new LightningOrb
                        {
                            origin = transform.position,
                            target = enemyHurtBox,
                            attacker = ownerBody.gameObject,
                            inflictor = gameObject,
                            teamIndex = team,
                            damageValue = zapDamage,
                            bouncesRemaining = 0,
                            isCrit = crit,
                            lightningType = LightningOrb.LightningType.Tesla,
                            procCoefficient = zapProcCoefficient,
                            damageType = DamageType.Stun1s
                        };
                        OrbManager.instance.AddOrb(orb);
                    }
                }

                zapCount++;
                if (zapCount >= maxZaps)
                {
                    Destroy(gameObject);
                }
            }
        }

        public class PrepLightningPillar : PrepCustomWall
        {
            public static GameObject areaIndicatorPrefabStatic;
            public static GameObject muzzleflashEffectStatic;
            public static GameObject goodCrosshairPrefabStatic;
            public static GameObject badCrosshairPrefabStatic;

            public override void OnEnter()
            {
                areaIndicatorPrefab = areaIndicatorPrefabStatic;
                muzzleflashEffect = muzzleflashEffectStatic;
                goodCrosshairPrefab = goodCrosshairPrefabStatic;
                badCrosshairPrefab = badCrosshairPrefabStatic;
                base.OnEnter();
            }

            public override void CreateWall(Vector3 position, Quaternion rotation)
            {
                if (NetworkServer.active)
                {
                    var fireWall = Object.Instantiate(lightningPillarPrefab, position, rotation);
                    fireWall.GetComponent<TeamFilter>().teamIndex = teamComponent.teamIndex;
                    fireWall.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(characterBody.gameObject);
                }
            }
        }
    }
}