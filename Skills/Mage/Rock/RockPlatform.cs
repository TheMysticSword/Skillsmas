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

namespace Skillsmas.Skills.Mage.Rock
{
    public class RockPlatform : BaseSkill
    {
        public static GameObject rockPlatformPrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Tectonic Shift",
            "Damage",
            200f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_UTILITY_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Tectonic Shift",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> explosionRadius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Tectonic Shift",
            "Explosion Radius",
            12f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            rockPlatformPrefab = Utils.CreateBlankPrefab("Skillsmas_RockPlatform", true);
            rockPlatformPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RockPlatform";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_UTILITY_ROCK_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_UTILITY_ROCK_DESCRIPTION";
            var keywordTokens = new List<string>()
            {
                "KEYWORD_SKILLSMAS_CRYSTALLIZE"
            };
            if (SkillsmasPlugin.artificerExtendedEnabled) keywordTokens.Add("KEYWORD_SKILLSMAS_ARTIFICEREXTENDED_ALTPASSIVE_ROCK");
            skillDef.keywordTokens = keywordTokens.ToArray();
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/TectonicShift.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(RockPlatformLeap));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Artificer: Tectonic Shift",
                baseRechargeInterval: 12f,
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
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(RockPlatformLeap));

            RockPlatformLeap.muzzleflashEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageFire.prefab").WaitForCompletion();
            RockPlatformLeap.explosionEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherDashEffect.prefab").WaitForCompletion();
            RockPlatformLeap.speedCoefficientCurve = AnimationCurve.EaseInOut(0f, 15f, 1f, 0f);
            SkillsmasRockPlatformController.lifetimeExpiredSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Mage/nseMageIcePillarRumble.asset").WaitForCompletion();

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockPlatform/RockPlatform.prefab"), rockPlatformPrefab);
            var rockPlatformController = rockPlatformPrefab.AddComponent<SkillsmasRockPlatformController>();
            rockPlatformController.collisionObject = rockPlatformPrefab.transform.Find("Mover/mdlRockPlatform/Collision").gameObject;
            var objectScaleCurve = rockPlatformPrefab.transform.Find("Mover").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = true;
            objectScaleCurve.timeMax = 0.3f;
            rockPlatformController.enableCollisionTime = objectScaleCurve.timeMax;
            rockPlatformController.lifetime = 11f + objectScaleCurve.timeMax;
            var objectTransformCurve = rockPlatformPrefab.transform.Find("Mover").gameObject.AddComponent<ObjectTransformCurve>();
            objectTransformCurve.loop = false;
            objectTransformCurve.useRotationCurves = false;
            objectTransformCurve.useTranslationCurves = true;
            objectTransformCurve.translationCurveY = AnimationCurve.EaseInOut(0f, -5f, 1f, 0f);
            objectTransformCurve.timeMax = objectScaleCurve.timeMax;

            var sdStone = Addressables.LoadAssetAsync<SurfaceDef>("RoR2/Base/Common/sdStone.asset").WaitForCompletion();
            foreach (var collider in rockPlatformPrefab.transform.Find("Mover/mdlRockPlatform/Collision").GetComponentsInChildren<Collider>())
            {
                var surfaceDefProvider = collider.GetComponent<SurfaceDefProvider>();
                if (surfaceDefProvider != null) continue;
                surfaceDefProvider = collider.gameObject.AddComponent<SurfaceDefProvider>();
                surfaceDefProvider.surfaceDef = sdStone;
            }

            SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockPlatform/RockPlatformLifetimeExpiredEffect.prefab");
            SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab.AddComponent<DestroyOnTimer>().duration = 5f;
            var shakeEmitter = SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab.AddComponent<ShakeEmitter>();
            shakeEmitter.wave = new Wave
            {
                amplitude = 3f,
                frequency = 6.5f
            };
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.radius = 50f;
            shakeEmitter.duration = 0.3f;
            shakeEmitter.shakeOnStart = true;
            var effectComponent = SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_titanboss_step";
            var vfxAttributes = SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            SkillsmasContent.Resources.effectPrefabs.Add(SkillsmasRockPlatformController.lifetimeExpiredEffectPrefab);
        }

        public class SkillsmasRockPlatformController : MonoBehaviour
        {
            public static GameObject lifetimeExpiredEffectPrefab;
            public static NetworkSoundEventDef lifetimeExpiredSound;

            public float lifetime = 8f;
            public float enableCollisionTime = 1f;
            public GameObject collisionObject;
            public float offsetForLifetimeExpiredSound = 2f;
            public int lifetimeExpiredSoundVolume = 4;

            public float age = 0;
            public bool collisionEnabled = false;
            public bool hasPlayedLifetimeExpiredSound = false;

            public void FixedUpdate()
            {
                age += Time.fixedDeltaTime;

                if (age >= enableCollisionTime && !collisionEnabled)
                {
                    collisionEnabled = true;
                    if (collisionObject) collisionObject.SetActive(true);
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
                    if (lifetimeExpiredEffectPrefab)
                    {
                        EffectManager.SpawnEffect(lifetimeExpiredEffectPrefab, new EffectData
                        {
                            origin = transform.position
                        }, false);
                    }
                    Destroy(gameObject);
                }
            }
        }

        public class RockPlatformLeap : EntityStates.GenericCharacterMain
        {
            public static GameObject explosionEffectPrefab;
            public static GameObject muzzleflashEffectPrefab;
            public static AnimationCurve speedCoefficientCurve;

            public float baseDuration = 1f;
            public Vector3 flyVector = Vector3.up;

            public float duration;
            public Vector3 blastPosition;

            public override void OnEnter()
            {
                base.OnEnter();

                duration = baseDuration;

                characterMotor.Motor.ForceUnground();
                characterMotor.velocity = Vector3.zero;
                
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
                        baseForce = 2200f,
                        teamIndex = teamComponent.teamIndex,
                        attackerFiltering = AttackerFiltering.NeverHitSelf
                    };
                    blastAttack.AddModdedDamageType(DamageTypes.Crystallize.crystallizeDamageType);
                    blastAttack.Fire();

                    var rockPlatform = Object.Instantiate(rockPlatformPrefab, blastPosition, Quaternion.identity);
                    NetworkServer.Spawn(rockPlatform);
                }

                Util.PlaySound("Play_item_use_BFG_explode", gameObject);
                PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", duration, 0.1f);

                EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, gameObject, "MuzzleRight", false);
                if (explosionEffectPrefab)
                {
                    EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
                    {
                        origin = blastPosition,
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
                characterMotor.rootMotion += flyVector * (speedCoefficientCurve.Evaluate(fixedAge / duration) * Time.fixedDeltaTime);
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
        }
    }
}