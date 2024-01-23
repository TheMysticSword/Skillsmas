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

namespace Skillsmas.Skills.Mage.Water
{
    public class WaterBomb : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> minDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Minimum Damage",
            400f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_WATER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> maxDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Maximum Damage",
            2000f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_WATER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Proc Coefficient",
            0.2f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> tickFrequency = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Tick Frequency",
            10f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> duration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Duration",
            2f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<bool> useSideCamera = ConfigOptions.ConfigurableValue.CreateBool(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Use Side Camera",
            true
        );
        public static ConfigOptions.ConfigurableValue<bool> streamCancelledFromSprinting = ConfigOptions.ConfigurableValue.CreateBool(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Concentrated Nano-Stream",
            "Stream Cancelled From Sprinting",
            true
        );

        public static CharacterCameraParams sideCameraParams;
        public static float sideCameraTransitionTime = 0.8f;

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_WaterBomb";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SECONDARY_WATER_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SECONDARY_WATER_DESCRIPTION";
            var keywordTokens = new List<string>()
            {
                "KEYWORD_SKILLSMAS_REVITALIZING"
            };
            if (SkillsmasPlugin.artificerExtendedEnabled) keywordTokens.Add("KEYWORD_SKILLSMAS_ARTIFICEREXTENDED_ALTPASSIVE_WATER");
            skillDef.keywordTokens = keywordTokens.ToArray();
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/ConcentratedNanoStream.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ChargeWaterBomb));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Concentrated Nano-Stream",
                baseRechargeInterval: 5f,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ChargeWaterBomb));
            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ThrowWaterBomb));

            ChargeWaterBomb.chargeEffectPrefabStatic = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterBomb/ChargeWaterBomb.prefab");
            var rings = new List<Transform>()
            {
                ChargeWaterBomb.chargeEffectPrefabStatic.transform.Find("RingRotator/Ring"),
                ChargeWaterBomb.chargeEffectPrefabStatic.transform.Find("RingRotator/Ring (1)"),
                ChargeWaterBomb.chargeEffectPrefabStatic.transform.Find("RingRotator/Ring (2)")
            };
            int i = 1;
            foreach (var ring in rings)
            {
                var objectScaleCurve = ring.gameObject.AddComponent<ObjectScaleCurve>();
                objectScaleCurve.overallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                objectScaleCurve.useOverallCurveOnly = true;
                objectScaleCurve.timeMax = 3f / rings.Count * i;
                var rotateObject = ring.gameObject.AddComponent<RotateObject>();
                rotateObject.rotationSpeed = new Vector3(0f, 480f / rings.Count * i, 0f);

                i++;
            }

            ThrowWaterBomb.beamPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterBomb/WaterBeam.prefab");
            ThrowWaterBomb.beamPrefab.transform.Find("Pivot/Start/SpinningTrails").gameObject.AddComponent<RotateObject>().rotationSpeed = new Vector3(0f, 0f, 360f);
            {
                var objectScaleCurve = ThrowWaterBomb.beamPrefab.transform.Find("Pivot").gameObject.AddComponent<ObjectScaleCurve>();
                objectScaleCurve.curveX = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                objectScaleCurve.curveY = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                objectScaleCurve.curveZ = AnimationCurve.Constant(0f, 1f, 1f);
                objectScaleCurve.overallCurve = AnimationCurve.Constant(0f, 1f, 1f);
                objectScaleCurve.useOverallCurveOnly = false;
                objectScaleCurve.timeMax = 0.2f;
                objectScaleCurve.enabled = false;
                var destroyOnTimer = ThrowWaterBomb.beamPrefab.AddComponent<DestroyOnTimer>();
                destroyOnTimer.duration = 0.2f;
                destroyOnTimer.enabled = false;
            }

            ThrowWaterBomb.muzzleFlashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightningLarge.prefab").WaitForCompletion();
            ThrowWaterBomb.loopSoundDef = Addressables.LoadAssetAsync<LoopSoundDef>("RoR2/Base/Brother/lsdBrotherFirePillar.asset").WaitForCompletion();

            sideCameraParams = ScriptableObject.CreateInstance<CharacterCameraParams>();
            sideCameraParams.data.idealLocalCameraPos = new Vector3(5.5f, 0f, -8f);
            sideCameraParams.data.pivotVerticalOffset = 0.8f;
        }

        public class ChargeWaterBomb : EntityStates.Mage.Weapon.BaseChargeBombState
        {
            public static GameObject chargeEffectPrefabStatic;

            public CameraTargetParams.CameraParamsOverrideHandle cameraParamsOverrideHandle;

            public override EntityStates.Mage.Weapon.BaseThrowBombState GetNextState()
            {
                return new ThrowWaterBomb();
            }

            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Mage.Weapon.ChargeIcebomb));
                chargeEffectPrefab = chargeEffectPrefabStatic;
                chargeSoundString = "Play_mage_m2_iceSpear_charge";
                baseDuration = 2f;
                minBloomRadius = 0.1f;
                maxBloomRadius = 0.5f;
                base.OnEnter();

                if (cameraTargetParams && useSideCamera)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
                    {
                        cameraParamsData = sideCameraParams.data,
                        priority = 1f
                    }, sideCameraTransitionTime);
                }
            }

            public override void OnExit()
            {
                if (cameraTargetParams && cameraParamsOverrideHandle.isValid)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.RemoveParamsOverride(cameraParamsOverrideHandle, sideCameraTransitionTime);
                }
                base.OnExit();
            }
        }

        public class ThrowWaterBomb : EntityStates.Mage.Weapon.BaseThrowBombState
        {
            public static GameObject beamPrefab;
            public static GameObject muzzleFlashEffectPrefabStatic;
            public static LoopSoundDef loopSoundDef;
            
            public GameObject beamInstance;
            public LoopSoundManager.SoundLoopPtr soundLoopPtr;
            public float tickInterval = 0.1f;
            public float tickTimer;
            public float beamFadeInTime = 0.14f;
            public float animationEntryDuration = 0.1f;
            public bool animationStarted = false;
            
            public float beamDamage;
            public float beamForce;
            public bool beamIsCrit;
            public float beamSize;

            public CameraTargetParams.CameraParamsOverrideHandle cameraParamsOverrideHandle;
            
            public override void OnEnter()
            {
                projectilePrefab = null;
                muzzleflashEffectPrefab = muzzleFlashEffectPrefabStatic;
                baseDuration = WaterBomb.duration;
                minDamageCoefficient = minDamage / 100f;
                maxDamageCoefficient = maxDamage / 100f;
                force = 300f;
                selfForce = 0f;
                
                base.OnEnter();

                tickInterval = 1f / tickFrequency / attackSpeedStat;
                animationEntryDuration = 0.1f / attackSpeedStat;

                beamDamage = Util.Remap(charge, 0f, 1f, minDamageCoefficient, maxDamageCoefficient) / duration;
                beamForce = charge * force;
                if (isAuthority)
                {
                    beamIsCrit = characterBody.RollCrit();
                    characterBody.isSprinting = false;
                }
                beamSize = Util.Remap(charge, 0f, 1f, 0.5f, 1f);
                
                Util.PlayAttackSpeedSound("Play_acrid_shift_land", gameObject, attackSpeedStat);
                soundLoopPtr = LoopSoundManager.PlaySoundLoopLocalRtpc(gameObject, loopSoundDef, "attackSpeed", Util.CalculateAttackSpeedRtpcValue(attackSpeedStat));
                PlayAnimation("Gesture, Additive", "PrepFlamethrower", "Flamethrower.playbackRate", animationEntryDuration);
                characterBody.SetAimTimer(duration + 1f);

                beamInstance = Object.Instantiate(beamPrefab);
                UpdateBeamTransform();
                RoR2Application.onLateUpdate += UpdateBeamTransformInLateUpdate;

                if (cameraTargetParams && useSideCamera)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
                    {
                        cameraParamsData = sideCameraParams.data,
                        priority = 1f
                    }, 0.2f);
                }
            }

            public void UpdateBeamTransform()
            {
                if (beamInstance)
                {
                    var beamRay = GetBeamRay();

                    beamInstance.transform.SetPositionAndRotation(beamRay.origin, Util.QuaternionSafeLookRotation(beamRay.direction));
                    
                    var scale = beamSize;
                    if (age <= beamFadeInTime)
                        scale *= Util.Remap(age, 0f, beamFadeInTime, 0f, 1f);
                    beamInstance.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }

            public override void Update()
            {
                base.Update();
                UpdateBeamTransform();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (age >= animationEntryDuration && !animationStarted)
                {
                    animationStarted = true;
                    PlayAnimation("Gesture, Additive", "Flamethrower", "Flamethrower.playbackRate", duration - age);
                }

                tickTimer -= Time.fixedDeltaTime;
                if (tickTimer <= 0)
                {
                    tickTimer += tickInterval;

                    characterBody.SetSpreadBloom(RoR2Application.rng.RangeFloat(0.4f, 0.5f), true);

                    if (isAuthority)
                    {
                        var beamRay = GetBeamRay();
                        var bulletAttack = new BulletAttack
                        {
                            origin = beamRay.origin,
                            aimVector = beamRay.direction,
                            minSpread = 0f,
                            maxSpread = 0f,
                            maxDistance = 35f,
                            hitMask = LayerIndex.CommonMasks.bullet,
                            stopperMask = 0,
                            bulletCount = 1,
                            radius = 3f * beamSize,
                            smartCollision = false,
                            queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                            procCoefficient = procCoefficient,
                            owner = gameObject,
                            weapon = gameObject,
                            damage = beamDamage * damageStat * tickInterval,
                            damageType = DamageType.Generic,
                            falloffModel = BulletAttack.FalloffModel.None,
                            force = beamForce * tickInterval,
                            tracerEffectPrefab = null,
                            isCrit = beamIsCrit,
                            HitEffectNormal = false
                        };
                        bulletAttack.AddModdedDamageType(DamageTypes.Revitalizing.revitalizingDamageType);
                        bulletAttack.Fire();
                    }
                }

                if (isAuthority)
                {
                    if (characterBody.isSprinting && streamCancelledFromSprinting)
                    {
                        outer.SetNextStateToMain();
                    }
                }
            }

            public override void OnExit()
            {
                RoR2Application.onLateUpdate -= UpdateBeamTransformInLateUpdate;
                if (beamInstance)
                {
                    var destroyOnTimer = beamInstance.GetComponent<DestroyOnTimer>();
                    if (destroyOnTimer) destroyOnTimer.enabled = true;

                    var objectScaleCurve = beamInstance.GetComponentInChildren<ObjectScaleCurve>();
                    if (objectScaleCurve) objectScaleCurve.enabled = true;
                }
                LoopSoundManager.StopSoundLoopLocal(soundLoopPtr);
                PlayCrossfade("Gesture, Additive", "ExitFlamethrower", 0.1f);
                if (cameraTargetParams && cameraParamsOverrideHandle.isValid)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.RemoveParamsOverride(cameraParamsOverrideHandle, sideCameraTransitionTime);
                }
                base.OnExit();
            }

            public Ray GetBeamRay()
            {
                var beamRay = GetAimRay();
                SkillsmasUtils.UncorrectAimRay(gameObject, ref beamRay, 35f);
                beamRay.origin += 1f * beamRay.direction;
                return beamRay;
            }

            public void UpdateBeamTransformInLateUpdate()
            {
                try
                {
                    UpdateBeamTransform();
                }
                catch (System.Exception) { }
            }

            public override void PlayThrowAnimation()
            {
                
            }
        }
    }
}