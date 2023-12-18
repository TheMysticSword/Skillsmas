using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Engi
{
    public class EnergyShield : BaseSkill
    {
        public static GameObject shieldPrefab;

        public static ConfigOptions.ConfigurableValue<float> damagePerSecond = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Engineer: Energy Shield",
            "Damage Per Second",
            400f,
            stringsToAffect: new List<string>
            {
                "ENGI_SKILLSMAS_ENERGYSHIELD_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Engineer: Energy Shield",
            "Proc Coefficient",
            0.2f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> hitFrequency = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Engineer: Energy Shield",
            "Hit Frequency",
            5f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            shieldPrefab = Utils.CreateBlankPrefab("Skillsmas_EnergyShieldWall", true);
            shieldPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            shieldPrefab.AddComponent<NetworkTransform>();
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_EnergyShield";
            skillDef.skillNameToken = "ENGI_SKILLSMAS_ENERGYSHIELD_NAME";
            skillDef.skillDescriptionToken = "ENGI_SKILLSMAS_ENERGYSHIELD_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_AGILE"
            };
            skillDef.icon = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/RoboBallBoss/texBuffEngiShieldIcon.tif").WaitForCompletion();
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ChannelEnergyShield));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Engineer: Energy Shield",
                baseRechargeInterval: 1f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: false
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Engi/EngiBodyPrimaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Engi/EnergyShield/EnergyShield.prefab"), shieldPrefab);
            var hitBoxGroup = shieldPrefab.AddComponent<HitBoxGroup>();
            hitBoxGroup.hitBoxes = new[]
            {
                shieldPrefab.transform.Find("mdlEnergyShield/DamageCollider").gameObject.AddComponent<HitBox>()
            };
            var nbd = shieldPrefab.AddComponent<NetworkedBodyAttachment>();
            nbd.shouldParentToAttachedBody = false;
            nbd.forceHostAuthority = false;
            shieldPrefab.AddComponent<TeamFilter>();
            shieldPrefab.AddComponent<SkillsmasEnergyShieldAttachment>();

            shieldPrefab.AddComponent<LoopSoundPlayer>().loopDef = Addressables.LoadAssetAsync<RoR2.Audio.LoopSoundDef>("RoR2/Base/ElementalRings/lsdFireTornado.asset").WaitForCompletion();

            ChannelEnergyShield.endEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/BubbleShieldEndEffect.prefab").WaitForCompletion();
        }

        public class SkillsmasEnergyShieldAttachment : MonoBehaviour, INetworkedBodyAttachmentListener
        {
            public CharacterBody ownerBody;
            public OverlapAttack overlapAttack;
            
            public float tickInterval = 1f / 20f;
            public float tickTimer = 0f;
            public float resetInterval = 1f / 3f;
            public float resetTimer = 0f;
            public float critTimer = 0f;
            public float critDuration = 4f;

            public void Awake()
            {
                resetInterval = 1f / hitFrequency;

                overlapAttack = new OverlapAttack
                {
                    hitBoxGroup = GetComponent<HitBoxGroup>(),
                    inflictor = gameObject,
                    procCoefficient = procCoefficient,
                    pushAwayForce = 500f,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = DamageType.Generic
                };
            }

            public void OnAttachedBodyDiscovered(NetworkedBodyAttachment networkedBodyAttachment, CharacterBody attachedBody)
            {
                ownerBody = attachedBody;
                overlapAttack.attacker = attachedBody.gameObject;
                overlapAttack.teamIndex = attachedBody.teamComponent.teamIndex;
            }

            public void FixedUpdate()
            {
                if (ownerBody && ownerBody.hasEffectiveAuthority)
                {
                    transform.position = ownerBody.corePosition;
                    var rot = ownerBody.inputBank.aimDirection;
                    rot.y = 0;
                    rot.Normalize();
                    transform.localRotation = Util.QuaternionSafeLookRotation(rot);

                    tickTimer += Time.fixedDeltaTime;
                    if (tickTimer >= tickInterval)
                    {
                        tickTimer -= tickInterval;
                        overlapAttack.damage = damagePerSecond / 100f * resetInterval * ownerBody.damage;
                        var hitAnyone = overlapAttack.Fire();
                        if (hitAnyone && !overlapAttack.isCrit)
                        {
                            overlapAttack.isCrit = ownerBody.RollCrit();
                            if (overlapAttack.isCrit) critTimer = critDuration;
                        }
                    }

                    resetTimer += Time.fixedDeltaTime;
                    if (resetTimer >= resetInterval)
                    {
                        resetTimer -= resetInterval;
                        overlapAttack.ResetIgnoredHealthComponents();
                    }

                    if (critTimer > 0f)
                    {
                        critTimer -= Time.fixedDeltaTime;
                        if (critTimer <= 0f)
                        {
                            overlapAttack.isCrit = false;
                        }
                    }
                }
            }
        }

        public class ChannelEnergyShield : EntityStates.BaseState
        {
            public static GameObject endEffectPrefab;

            public GameObject shieldInstance;

            public override void OnEnter()
            {
                base.OnEnter();
                Util.PlaySound("Play_engi_M2_land", gameObject);
                if (NetworkServer.active)
                {
                    shieldInstance = Object.Instantiate(shieldPrefab);
                    shieldInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(characterBody.gameObject);
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && (!inputBank || !inputBank.skill1.down))
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }

            public override void OnExit()
            {
                if (NetworkServer.active)
                {
                    if (shieldInstance) Object.Destroy(shieldInstance);
                }
                EffectManager.SpawnEffect(endEffectPrefab, new EffectData
                {
                    origin = transform.position,
                    rotation = transform.rotation,
                    scale = 10f
                }, false);
                Util.PlaySound("Play_engi_R_place", gameObject);
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }
    }
}