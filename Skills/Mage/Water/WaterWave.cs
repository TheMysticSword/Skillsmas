using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using R2API.Networking.Interfaces;
using R2API.Networking;

namespace Skillsmas.Skills.Mage.Water
{
    public class WaterWave : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damageScaling = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Crashing Wave",
            "Damage Scaling",
            125f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SPECIAL_WATER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Crashing Wave",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> radius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Crashing Wave",
            "Radius",
            16f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public static GameObject waveEffectPrefab;
        public static GameObject explosionEffectPrefab;

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_WaterWave";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SPECIAL_WATER_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SPECIAL_WATER_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_REVITALIZING"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/CrashingWave.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(SurfWave));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Crashing Wave",
                baseRechargeInterval: 5f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: true,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(SurfWave));

            explosionEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterWave/WaterWaveExplosion.prefab");
            explosionEffectPrefab.AddComponent<DestroyOnTimer>().duration = 5f;
            var shakeEmitter = explosionEffectPrefab.AddComponent<ShakeEmitter>();
            shakeEmitter.wave = new Wave
            {
                amplitude = 2f,
                frequency = 5f
            };
            shakeEmitter.scaleShakeRadiusWithLocalScale = true;
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.radius = 3f;
            shakeEmitter.duration = 0.6f;
            shakeEmitter.shakeOnStart = true;
            var lightIntensityCurve = explosionEffectPrefab.transform.Find("Point Light").gameObject.AddComponent<LightIntensityCurve>();
            lightIntensityCurve.curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            lightIntensityCurve.timeMax = 0.5f;
            lightIntensityCurve.gameObject.AddComponent<LightScaleFromParent>();
            var effectComponent = explosionEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_acrid_shift_land";
            effectComponent.applyScale = true;
            var vfxAttributes = explosionEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.High;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            SkillsmasContent.Resources.effectPrefabs.Add(explosionEffectPrefab);

            waveEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterWave/WaterWaveEffect.prefab");
            waveEffectPrefab.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = waveEffectPrefab.transform.Find("Splashes").GetComponent<ParticleSystem>();
            waveEffectPrefab.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = waveEffectPrefab.transform.Find("Splashes (1)").GetComponent<ParticleSystem>();
            waveEffectPrefab.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = waveEffectPrefab.transform.Find("Splashes, Trailing").GetComponent<ParticleSystem>();
            waveEffectPrefab.AddComponent<DetachTrailOnDestroy>().targetTrailRenderers = new[]
            {
                waveEffectPrefab.transform.Find("TrailPivot/Trail").GetComponent<TrailRenderer>(),
                waveEffectPrefab.transform.Find("TrailPivot/Trail (1)").GetComponent<TrailRenderer>(),
                waveEffectPrefab.transform.Find("TrailPivot/Trail, Side").GetComponent<TrailRenderer>(),
                waveEffectPrefab.transform.Find("TrailPivot/Trail, Side (1)").GetComponent<TrailRenderer>()
            };

            SurfWave.muzzleflashEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightningLargeWithTrail.prefab").WaitForCompletion();
        }

        public class SurfWave : EntityStates.GenericCharacterMain
        {
            public static GameObject muzzleflashEffect;
            public static float forwardVelocity = 0f;
            public static float forwardVelocityFromMoveSpeed = 2.6f;
            public static float upwardVelocity = 14f;
            public static float upwardVelocityFromMoveSpeed = 0f;
            public static float minDuration = 0.1f;
            public static float maxDuration = 10f;

            public float previousAirControl;
            public bool detonateNextFrame = false;
            public GameObject waveEffect;

            public override void OnEnter()
            {
                base.OnEnter();

                var footPosition = transform.position;
                if (characterBody) {
                    characterBody.SetAimTimer(0f);
                    footPosition = characterBody.footPosition;
                    if (isAuthority) characterBody.isSprinting = true;
                }
                Util.PlaySound("Play_moonBrother_phaseJump_jumpAway", gameObject);
                Util.PlaySound("Play_acrid_shift_fly_loop", gameObject);
                PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", 1f, 0.1f);
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleRight", false);

                previousAirControl = characterMotor.airControl;
                characterMotor.airControl = 0f;

                var surfDirection = GetAimRay().direction;
                if (isAuthority)
                {
                    characterMotor.Motor.ForceUnground();
                    var xzVelocity = new Vector3(surfDirection.x, 0f, surfDirection.z).normalized;
                    xzVelocity *= forwardVelocity + moveSpeedStat * forwardVelocityFromMoveSpeed;
                    var yVelocity = Vector3.up;
                    yVelocity *= upwardVelocity + moveSpeedStat * upwardVelocityFromMoveSpeed;
                    characterMotor.velocity = xzVelocity + yVelocity;

                    characterMotor.onMovementHit += OnMovementHit;
                }
                characterDirection.moveVector = surfDirection;
                characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;

                waveEffect = Object.Instantiate(waveEffectPrefab, footPosition, Util.QuaternionSafeLookRotation(surfDirection));
            }

            private void OnMovementHit(ref CharacterMotor.MovementHitInfo movementHitInfo)
            {
                detonateNextFrame = true;
            }

            public void UpdateWaveEffect()
            {
                waveEffect.transform.SetPositionAndRotation(characterBody.footPosition, Util.QuaternionSafeLookRotation(characterMotor.velocity));
            }

            public override void Update()
            {
                base.Update();
                UpdateWaveEffect();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority)
                {
                    if (characterMotor)
                    {
                        characterMotor.moveDirection = inputBank.moveVector;
                        if (fixedAge >= minDuration && (detonateNextFrame || (characterMotor.Motor.GroundingStatus.IsStableOnGround && !characterMotor.Motor.LastGroundingStatus.IsStableOnGround)))
                        {
                            DoImpactAuthority();
                            outer.SetNextStateToMain();
                        }
                    }
                    if (fixedAge >= maxDuration)
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }

            public void DoImpactAuthority()
            {
                var footPosition = characterBody.footPosition;
                
                EffectManager.SpawnEffect(explosionEffectPrefab, new EffectData
                {
                    origin = footPosition,
                    scale = radius
                }, true);

                var blastAttack = new BlastAttack
                {
                    attacker = gameObject,
                    baseDamage = healthComponent.fullHealth * damageScaling / 100f,
                    baseForce = 1600f,
                    crit = RollCrit(),
                    falloffModel = BlastAttack.FalloffModel.SweetSpot,
                    procCoefficient = procCoefficient,
                    radius = radius,
                    position = footPosition,
                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                    teamIndex = teamComponent.teamIndex
                };
                blastAttack.AddModdedDamageType(DamageTypes.Revitalizing.revitalizingDamageType);
                blastAttack.Fire();
            }

            public override void OnExit()
            {
                Util.PlaySound("Stop_acrid_shift_fly_loop", gameObject);
                if (isAuthority)
                {
                    characterMotor.onMovementHit -= OnMovementHit;
                }
                characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                characterMotor.airControl = previousAirControl;
                if (waveEffect) Destroy(waveEffect);
                base.OnExit();
            }
        }
    }
}