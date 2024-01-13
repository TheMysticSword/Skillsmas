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

namespace Skillsmas.Skills.Mage.Fire
{
    public class FireWall : BaseSkill
    {
        public static GameObject fireWallPrefab;

        public static ConfigOptions.ConfigurableValue<float> damagePerSecond = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Brimstone",
            "Damage Per Second",
            100f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_UTILITY_FIRE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Brimstone",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> hitFrequency = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Brimstone",
            "Hit Frequency",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> lifetime = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Brimstone",
            "Lifetime",
            8f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            fireWallPrefab = Utils.CreateBlankPrefab("Skillsmas_FireWall", true);
            fireWallPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_FireWall";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_UTILITY_FIRE_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_UTILITY_FIRE_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_IGNITE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Brimstone.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(PrepFireWall));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Artificer: Brimstone",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(PrepFireWall));

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/FireWall/FireWall.prefab"), fireWallPrefab);
            var teamFilter = fireWallPrefab.AddComponent<TeamFilter>();
            var nbd = fireWallPrefab.AddComponent<NetworkedBodyAttachment>();
            nbd.shouldParentToAttachedBody = false;
            var fireWallController = fireWallPrefab.AddComponent<SkillsmasFireWallController>();
            fireWallController.lifetimeExpiredSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Mage/nseMageIcePillarRumble.asset").WaitForCompletion();
            fireWallController.sidePillarTransforms.Add(fireWallPrefab.transform.Find("Crystal"));
            fireWallController.sidePillarTransforms.Add(fireWallPrefab.transform.Find("Crystal (1)"));
            fireWallController.wallRenderer = fireWallPrefab.transform.Find("Wall").GetComponent<Renderer>();
            fireWallController.sidePillarExplosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab").WaitForCompletion();
            // fireWallController.wallExplosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/BrittleDeath.prefab").WaitForCompletion();
            fireWallController.flashMaterial = SkillsmasPlugin.AssetBundle.LoadAsset<Material>("Assets/Mods/Skillsmas/Skills/Artificer/FireWall/matFireWallTemporaryFlash.mat");
            fireWallPrefab.AddComponent<HitBoxGroup>().hitBoxes = new[]
            {
                fireWallPrefab.transform.Find("Wall/ColliderDamaging").gameObject.AddComponent<HitBox>()
            };
            var enhanceZone = fireWallPrefab.transform.Find("Wall/ColliderProjectiles").gameObject.AddComponent<SkillsmasProjectileEnhanceZone>();
            enhanceZone.teamFilter = teamFilter;
            enhanceZone.enhancementDamageTypes.Add(DamageType.IgniteOnHit);
            enhanceZone = fireWallPrefab.transform.Find("Wall/ColliderProjectiles/ColliderProjectilesFakeActor").gameObject.AddComponent<SkillsmasProjectileEnhanceZone>();
            enhanceZone.teamFilter = teamFilter;
            enhanceZone.enhancementDamageTypes.Add(DamageType.IgniteOnHit);

            PrepFireWall.areaIndicatorPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/FirewallAreaIndicator.prefab").WaitForCompletion();
            PrepFireWall.muzzleflashEffectStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageFire.prefab").WaitForCompletion();
            PrepFireWall.goodCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/SimpleDotCrosshair.prefab").WaitForCompletion();
            PrepFireWall.badCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/BadCrosshair.prefab").WaitForCompletion();
        }

        public class SkillsmasFireWallController : MonoBehaviour, INetworkedBodyAttachmentListener
        {
            public CharacterBody ownerBody;
            public OverlapAttack overlapAttack;

            public float tickInterval = 1f / 20f;
            public float tickTimer = 0f;
            public float resetInterval = 1f / 3f;
            public float resetTimer = 0f;

            public float age = 0f;

            public NetworkSoundEventDef lifetimeExpiredSound;
            public float offsetForLifetimeExpiredSound = 2f;
            public int lifetimeExpiredSoundVolume = 4;
            public bool hasPlayedLifetimeExpiredSound = false;

            public List<Transform> sidePillarTransforms = new List<Transform>();
            public Renderer wallRenderer;
            public GameObject sidePillarExplosionEffect;
            public GameObject wallExplosionEffect;

            public Material flashMaterial;
            public Material flashMaterialInstance;
            public float flashDuration = 0.3f;

            public string spawnSoundString = "Play_engi_R_place";
            
            public void Awake()
            {
                resetInterval = 1f / hitFrequency;

                overlapAttack = new OverlapAttack
                {
                    hitBoxGroup = GetComponent<HitBoxGroup>(),
                    damageType = DamageType.IgniteOnHit,
                    forceVector = Vector3.zero,
                    hitEffectPrefab = null,
                    inflictor = gameObject,
                    procCoefficient = procCoefficient,
                    pushAwayForce = 0f
                };
            }

            public void Start()
            {
                Util.PlaySound(spawnSoundString, gameObject);

                if (flashMaterial && wallRenderer)
                {
                    flashMaterialInstance = Instantiate(flashMaterial);
                    var wallMaterials = wallRenderer.materials.ToList();
                    wallMaterials.Add(flashMaterialInstance);
                    wallRenderer.materials = wallMaterials.ToArray();
                }
            }

            public void OnAttachedBodyDiscovered(NetworkedBodyAttachment networkedBodyAttachment, CharacterBody attachedBody)
            {
                ownerBody = attachedBody;
                overlapAttack.attacker = attachedBody.gameObject;
                overlapAttack.damage = damagePerSecond / 100f * resetInterval * attachedBody.damage;
                overlapAttack.isCrit = attachedBody.RollCrit();
                overlapAttack.teamIndex = attachedBody.teamComponent.teamIndex;
            }

            public void FixedUpdate()
            {
                age += Time.fixedDeltaTime;

                if (flashMaterialInstance)
                {
                    var flashPower = 1f - Mathf.Clamp01(age / flashDuration);
                    flashMaterialInstance.SetFloat("_ExternalAlpha", flashPower);
                }

                if (age >= lifetime - offsetForLifetimeExpiredSound)
                {
                    if (!hasPlayedLifetimeExpiredSound)
                    {
                        hasPlayedLifetimeExpiredSound = true;
                        if (NetworkServer.active && lifetimeExpiredSound)
                        {
                            for (var i = 0; i < lifetimeExpiredSoundVolume; i++)
                                PointSoundManager.EmitSoundServer(lifetimeExpiredSound.index, transform.position);
                        }
                    }
                }

                if (age >= lifetime)
                {
                    OnLifetimeExpired();
                    return;
                }

                if (ownerBody && ownerBody.hasEffectiveAuthority)
                {
                    tickTimer += Time.fixedDeltaTime;
                    if (tickTimer >= tickInterval)
                    {
                        tickTimer -= tickInterval;
                        overlapAttack.Fire();
                    }

                    resetTimer += Time.fixedDeltaTime;
                    if (resetTimer >= resetInterval)
                    {
                        resetTimer -= resetInterval;
                        overlapAttack.ResetIgnoredHealthComponents();
                    }
                }
            }

            public void OnLifetimeExpired()
            {
                if (sidePillarExplosionEffect)
                {
                    foreach (var sidePillarTransform in sidePillarTransforms)
                    {
                        EffectManager.SpawnEffect(sidePillarExplosionEffect, new EffectData
                        {
                            origin = sidePillarTransform.position,
                            scale = 4f
                        }, false);
                    }
                }
                if (wallExplosionEffect && wallRenderer)
                {
                    EffectManager.SpawnEffect(wallExplosionEffect, new EffectData
                    {
                        origin = wallRenderer.transform.position
                    }, false);
                }
                Destroy(gameObject);
            }

            public void OnDestroy()
            {
                if (flashMaterialInstance) Destroy(flashMaterialInstance);
            }
        }

        public class PrepFireWall : PrepCustomWall
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
                    var fireWall = Object.Instantiate(fireWallPrefab, position, rotation);
                    fireWall.GetComponent<TeamFilter>().teamIndex = teamComponent.teamIndex;
                    fireWall.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(characterBody.gameObject);
                }
            }
        }
    }
}