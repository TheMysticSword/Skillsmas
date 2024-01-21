using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using R2API.Networking;
using System.Collections.Generic;
using System.Linq;
using RoR2.Orbs;

namespace Skillsmas.Skills.Bandit
{
    public class ChainKillBuff : BaseSkill
    {
        public static DamageAPI.ModdedDamageType chainKillBuffDamageType;
        public static GameObject orbPrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Murder Party",
            "Damage",
            600f,
            max: 100000f,
            stringsToAffect: new List<string>
            {
                "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> markDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Murder Party",
            "Mark Duration",
            3f,
            stringsToAffect: new List<string>
            {
                "KEYWORD_SKILLSMAS_KILLCHAIN",
                "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<int> maxChainKills = ConfigOptions.ConfigurableValue.CreateInt(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Murder Party",
            "Max Chain Kills",
            5,
            stringsToAffect: new List<string>
            {
                "KEYWORD_SKILLSMAS_KILLCHAIN",
                "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> bonusPerMarkKill = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Murder Party",
            "Bonus Per Mark Kill",
            10f,
            stringsToAffect: new List<string>
            {
                "KEYWORD_SKILLSMAS_KILLCHAIN",
                "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> buffDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Murder Party",
            "Buff Duration",
            20f,
            stringsToAffect: new List<string>
            {
                "KEYWORD_SKILLSMAS_KILLCHAIN",
                "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            chainKillBuffDamageType = DamageAPI.ReserveDamageType();
            NetworkingAPI.RegisterMessageType<SyncResetSpecial>();
            NetworkingAPI.RegisterMessageType<SyncSetSpecialOnCooldown>();
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_ChainKillBuff";
            skillDef.skillNameToken = "BANDIT2_SKILLSMAS_CHAINKILLBUFF_NAME";
            skillDef.skillDescriptionToken = "BANDIT2_SKILLSMAS_CHAINKILLBUFF_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SLAYER",
                "KEYWORD_SKILLSMAS_KILLCHAIN"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/MurderParty.jpg");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(PrepSidearmChainKillBuffRevolver));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Bandit: Murder Party",
                baseRechargeInterval: 20f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: true,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Bandit2/Bandit2BodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(PrepSidearmChainKillBuffRevolver));
            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireSidearmChainKillBuffRevolver));

            PrepSidearmChainKillBuffRevolver.crosshairOverridePrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/Bandit2CrosshairPrepRevolver.prefab").WaitForCompletion();

            FireSidearmChainKillBuffRevolver.effectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/MuzzleflashBandit2.prefab").WaitForCompletion();
            FireSidearmChainKillBuffRevolver.tracerEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/TracerBandit2Rifle.prefab").WaitForCompletion();
            FireSidearmChainKillBuffRevolver.hitEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/HitsparkBandit2Pistol.prefab").WaitForCompletion();
            FireSidearmChainKillBuffRevolver.crosshairOverridePrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/Bandit2CrosshairPrepRevolverFire.prefab").WaitForCompletion();

            {
                orbPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Bandit/ChainKillBuff/ChainKillBuffOrb.prefab");
                var effectComponent = orbPrefab.AddComponent<EffectComponent>();
                effectComponent.positionAtReferencedTransform = false;
                effectComponent.parentToReferencedTransform = false;
                effectComponent.applyScale = true;
                var vfxAttributes = orbPrefab.AddComponent<VFXAttributes>();
                vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
                vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
                var orbEffect = orbPrefab.AddComponent<OrbEffect>();
                orbEffect.startVelocity1 = new Vector3(0f, 25f, 0f);
                orbEffect.startVelocity2 = new Vector3(0f, 50f, 0f);
                orbEffect.endVelocity1 = new Vector3(0f, 0f, 0f);
                orbEffect.endVelocity2 = new Vector3(0f, 0f, 0f);
                orbEffect.movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                orbEffect.faceMovement = true;
                orbEffect.callArrivalIfTargetIsGone = false;
                var destroyOnTimer = orbPrefab.transform.Find("Trail").gameObject.AddComponent<DestroyOnTimer>();
                destroyOnTimer.duration = 0.5f;
                destroyOnTimer.enabled = false;
                var onArrivalDefaults = orbPrefab.AddComponent<MysticsRisky2Utils.MonoBehaviours.MysticsRisky2UtilsOrbEffectOnArrivalDefaults>();
                onArrivalDefaults.orbEffect = orbEffect;
                onArrivalDefaults.transformsToUnparentChildren = new[] {
                    orbPrefab.transform.Find("Trail")
                };
                onArrivalDefaults.componentsToEnable = new MonoBehaviour[]
                {
                    destroyOnTimer
                };
                SkillsmasContent.Resources.effectPrefabs.Add(orbPrefab);
            }

            GenericGameEvents.OnTakeDamage += GenericGameEvents_OnTakeDamage;
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
        }

        private void GenericGameEvents_OnTakeDamage(DamageReport damageReport)
        {
            if (damageReport.damageInfo.HasModdedDamageType(chainKillBuffDamageType))
            {
                damageReport.victimBody.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyHit, 0.001f);
            }
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
        {
            if (damageReport.damageInfo != null && damageReport.victimBody.HasBuff(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyHit))
            {
                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2ResetEffect"), new EffectData
                {
                    origin = damageReport.damageInfo.position
                }, true);

                var attackerBody = damageReport.attackerBody;
                if (attackerBody && attackerBody.HasBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillActive))
                {
                    var buffCount = attackerBody.GetBuffCount(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyKill);
                    if (buffCount < (maxChainKills - 1))
                    {
                        if (buffCount <= 0 || damageReport.victimBody.HasBuff(SkillsmasContent.Buffs.Skillsmas_EnemyMark))
                        {
                            attackerBody.AddBuff(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyKill);
                            attackerBody.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillActive, markDuration);

                            var skillLocator = attackerBody.skillLocator;
                            if (skillLocator && skillLocator.specialBonusStockSkill)
                            {
                                if (NetworkServer.active && skillLocator.networkIdentity.clientAuthorityOwner != null)
                                    new SyncResetSpecial(skillLocator.networkIdentity.netId).Send(NetworkDestination.Clients);
                                skillLocator.specialBonusStockSkill.Reset();
                            }

                            var aimRay = new Ray(attackerBody.transform.position, attackerBody.transform.forward);
                            if (attackerBody.inputBank)
                                aimRay = new Ray(attackerBody.inputBank.aimOrigin, attackerBody.inputBank.aimDirection);
                            var search = new BullseyeSearch
                            {
                                searchOrigin = aimRay.origin,
                                maxDistanceFilter = 60f,
                                teamMaskFilter = TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex),
                                filterByLoS = true,
                                maxAngleFilter = 90f,
                                searchDirection = aimRay.direction
                            };
                            search.RefreshCandidates();
                            var results = search.GetResults().Where(x => x.healthComponent.alive);
                            if (results.Count() <= 0)
                            {
                                search.maxAngleFilter = 180f;
                                search.RefreshCandidates();
                                results = search.GetResults().Where(x => x.healthComponent.alive);
                            }
                            if (results.Count() <= 0)
                            {
                                search.maxDistanceFilter = 200f;
                                search.filterByLoS = false;
                                search.RefreshCandidates();
                                results = search.GetResults().Where(x => x.healthComponent.alive);
                            }
                            var result = results.OrderBy(x => x.healthComponent.health).FirstOrDefault();
                            if (result != null)
                            {
                                result.healthComponent.body.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_EnemyMark, markDuration);

                                var effectData = new EffectData
                                {
                                    origin = damageReport.victimBody.corePosition,
                                    genericFloat = 0.4f,
                                    scale = 1f
                                };
                                effectData.SetHurtBoxReference(result);
                                EffectManager.SpawnEffect(orbPrefab, effectData, true);
                            }
                        }
                    }
                    else
                    {
                        attackerBody.AddBuff(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyKill);
                        attackerBody.ClearTimedBuffs(SkillsmasContent.Buffs.Skillsmas_ChainKillActive);
                    }
                }
            }
        }

        private void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (NetworkServer.active && buffDef == SkillsmasContent.Buffs.Skillsmas_ChainKillActive)
            {
                var buffCount = self.GetBuffCount(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyKill);
                for (var i = 0; i < buffCount; i++)
                {
                    self.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillBonusDamage, buffDuration);
                    self.RemoveBuff(SkillsmasContent.Buffs.Skillsmas_MarkedEnemyKill);
                }

                var skillLocator = self.skillLocator;
                if (skillLocator && skillLocator.specialBonusStockSkill)
                {
                    if (NetworkServer.active && skillLocator.networkIdentity.clientAuthorityOwner != null)
                        new SyncSetSpecialOnCooldown(skillLocator.networkIdentity.netId).Send(NetworkDestination.Clients);
                    skillLocator.specialBonusStockSkill.RemoveAllStocks();
                }
            }
        }

        public class SyncResetSpecial : INetMessage
        {
            NetworkInstanceId objID;

            public SyncResetSpecial()
            {
            }

            public SyncResetSpecial(NetworkInstanceId objID)
            {
                this.objID = objID;
            }

            public void Deserialize(NetworkReader reader)
            {
                objID = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;
                GameObject obj = Util.FindNetworkObject(objID);
                if (obj)
                {
                    var component = obj.GetComponent<SkillLocator>();
                    if (component && component.specialBonusStockSkill)
                    {
                        component.specialBonusStockSkill.Reset();
                    }
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(objID);
            }
        }

        public class SyncSetSpecialOnCooldown : INetMessage
        {
            NetworkInstanceId objID;

            public SyncSetSpecialOnCooldown()
            {
            }

            public SyncSetSpecialOnCooldown(NetworkInstanceId objID)
            {
                this.objID = objID;
            }

            public void Deserialize(NetworkReader reader)
            {
                objID = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;
                GameObject obj = Util.FindNetworkObject(objID);
                if (obj)
                {
                    var component = obj.GetComponent<SkillLocator>();
                    if (component && component.specialBonusStockSkill)
                    {
                        component.specialBonusStockSkill.RemoveAllStocks();
                    }
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(objID);
            }
        }

        public class PrepSidearmChainKillBuffRevolver : EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState
        {
            public static GameObject crosshairOverridePrefabStatic;

            public bool hadBuff = false;

            public override void OnEnter()
            {
                baseDuration = 0.7f;
                crosshairOverridePrefab = crosshairOverridePrefabStatic;
                enterSoundString = "Play_bandit2_R_load";
                base.OnEnter();
                if (NetworkServer.active && !characterBody.HasBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillActive))
                {
                    characterBody.ClearTimedBuffs(SkillsmasContent.Buffs.Skillsmas_ChainKillBonusDamage);
                    characterBody.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillActive, markDuration + duration);
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority)
                {
                    var hasBuff = characterBody.HasBuff(SkillsmasContent.Buffs.Skillsmas_ChainKillActive);
                    if (!hadBuff && hasBuff)
                    {
                        hadBuff = true;
                    }
                    if (hadBuff && !hasBuff)
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }

            public override EntityStates.EntityState GetNextState()
            {
                return new FireSidearmChainKillBuffRevolver();
            }
        }

        public class FireSidearmChainKillBuffRevolver : EntityStates.Bandit2.Weapon.BaseFireSidearmRevolverState
        {
            public static GameObject effectPrefabStatic;
            public static GameObject hitEffectPrefabStatic;
            public static GameObject tracerEffectPrefabStatic;
            public static GameObject crosshairOverridePrefabStatic;

            public override void OnEnter()
            {
                baseDuration = 1f;
                effectPrefab = effectPrefabStatic;
                hitEffectPrefab = hitEffectPrefabStatic;
                tracerEffectPrefab = tracerEffectPrefabStatic;
                crosshairOverridePrefab = crosshairOverridePrefabStatic;
                damageCoefficient = damage / 100f;
                force = 1500f;
                minSpread = 0f;
                maxSpread = 0f;
                attackSoundString = "Play_bandit2_R_fire";
                recoilAmplitude = 1f;
                bulletRadius = 1f;
                base.OnEnter();
            }

            public override void ModifyBullet(BulletAttack bulletAttack)
            {
                base.ModifyBullet(bulletAttack);
                bulletAttack.AddModdedDamageType(chainKillBuffDamageType);
            }
        }
    }
}