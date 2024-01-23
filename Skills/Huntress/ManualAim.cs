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
using RoR2.Orbs;

namespace Skillsmas.Skills.Huntress
{
    public class ManualAim : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> minimumDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Huntress: Take Aim",
            "Minimum Damage",
            100f,
            stringsToAffect: new List<string>
            {
                "HUNTRESS_SKILLSMAS_MANUALAIM_DESCRIPTION",
                "HUNTRESS_SKILLSMAS_MANUALAIMPRIMARY_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> maximumDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Huntress: Take Aim",
            "Maximum Damage",
            900f,
            stringsToAffect: new List<string>
            {
                "HUNTRESS_SKILLSMAS_MANUALAIM_DESCRIPTION",
                "HUNTRESS_SKILLSMAS_MANUALAIMPRIMARY_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> projectileSpeed;
        public static ConfigOptions.ConfigurableValue<bool> useSideCamera = ConfigOptions.ConfigurableValue.CreateBool(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Huntress: Take Aim",
            "Use Side Camera",
            true
        );

        public static GameObject projectilePrefab;
        public static GameObject projectileChargedPrefab;
        public static GameObject chargeEffectPrefab;
        public static GameObject chargeFullEffectPrefab;
        public static GameObject muzzleFlashEffectPrefab;
        public static GameObject crosshairOverridePrefab;

        public static SkillDef primarySkillDef;
        public static CharacterCameraParams sideCameraParams;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab = Utils.CreateBlankPrefab("Skillsmas_ManualAimArrowProjectile", true);
            projectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            projectileChargedPrefab = Utils.CreateBlankPrefab("Skillsmas_ManualAimArrowChargedProjectile", true);
            projectileChargedPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_TrackerGlaive";
            skillDef.skillNameToken = "HUNTRESS_SKILLSMAS_MANUALAIM_NAME";
            skillDef.skillDescriptionToken = "HUNTRESS_SKILLSMAS_MANUALAIM_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_IGNITE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/TakeAim.png");
            skillDef.activationStateMachineName = "Body";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ManualAimState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Huntress: Take Aim",
                baseRechargeInterval: 12f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 0,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: false,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Huntress/HuntressBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ManualAimState));
            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ChargeArrow));

            {
                var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimArrowGhost.prefab");
                ghost.AddComponent<ProjectileGhostController>();

                Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimArrowProjectile.prefab"), projectilePrefab);
                var projectileController = projectilePrefab.AddComponent<ProjectileController>();
                projectileController.allowPrediction = true;
                projectileController.ghostPrefab = ghost;
                projectilePrefab.AddComponent<ProjectileNetworkTransform>();
                var projectileSimple = projectilePrefab.AddComponent<ProjectileSimple>();
                projectileSimple.desiredForwardSpeed = 120f;
                projectileSimple.lifetime = 10f;
                var projectileDamage = projectilePrefab.AddComponent<ProjectileDamage>();
                var projectileSingleTargetImpact = projectilePrefab.AddComponent<ProjectileSingleTargetImpact>();
                projectileSingleTargetImpact.destroyOnWorld = true;
                projectileSingleTargetImpact.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/OmniImpactVFXHuntress.prefab").WaitForCompletion();
                projectileSingleTargetImpact.hitSoundString = "Play_MULT_m1_smg_impact";
                projectilePrefab.AddComponent<SkillsmasAlignToRigidbodyVelocity>();

                SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);
            }

            {
                var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimArrowChargedGhost.prefab");
                ghost.AddComponent<ProjectileGhostController>();

                Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimArrowProjectile.prefab"), projectileChargedPrefab);
                var projectileController = projectileChargedPrefab.AddComponent<ProjectileController>();
                projectileController.allowPrediction = true;
                projectileController.ghostPrefab = ghost;
                projectileChargedPrefab.AddComponent<ProjectileNetworkTransform>();
                var projectileSimple = projectileChargedPrefab.AddComponent<ProjectileSimple>();
                projectileSimple.desiredForwardSpeed = 120f;
                projectileSimple.lifetime = 10f;
                var projectileDamage = projectileChargedPrefab.AddComponent<ProjectileDamage>();
                projectileDamage.damageType = DamageType.IgniteOnHit;
                var projectileImpactExplosion = projectileChargedPrefab.AddComponent<ProjectileImpactExplosion>();
                projectileImpactExplosion.blastDamageCoefficient = 1f;
                projectileImpactExplosion.blastProcCoefficient = 1f;
                projectileImpactExplosion.blastRadius = 1f;
                projectileImpactExplosion.destroyOnEnemy = true;
                projectileImpactExplosion.destroyOnWorld = true;
                projectileImpactExplosion.explosionEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/OmniExplosionVFXQuick.prefab").WaitForCompletion();
                projectileImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
                projectileImpactExplosion.lifetime = 99f;
                projectileChargedPrefab.AddComponent<SkillsmasAlignToRigidbodyVelocity>();

                SkillsmasContent.Resources.projectilePrefabs.Add(projectileChargedPrefab);
            }

            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Huntress: Take Aim",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    projectilePrefab.GetComponent<ProjectileController>().procCoefficient = newValue;
                    projectileChargedPrefab.GetComponent<ProjectileController>().procCoefficient = newValue;
                }
            );

            projectileSpeed = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Huntress: Take Aim",
                "Projectile Speed",
                160f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    projectilePrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed = newValue;
                    projectileChargedPrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed = newValue;
                }
            );

            primarySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            primarySkillDef.skillName = "Skillsmas_ManualAimPrimary";
            ((ScriptableObject)primarySkillDef).name = primarySkillDef.skillName;
            primarySkillDef.skillNameToken = "HUNTRESS_SKILLSMAS_MANUALAIMPRIMARY_NAME";
            primarySkillDef.skillDescriptionToken = "HUNTRESS_SKILLSMAS_MANUALAIMPRIMARY_DESCRIPTION";
            primarySkillDef.keywordTokens = new[]
            {
                "KEYWORD_AGILE",
                "KEYWORD_IGNITE"
            };
            primarySkillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/ManualAim.png");
            primarySkillDef.activationStateMachineName = "Weapon";
            primarySkillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ChargeArrow));
            primarySkillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            primarySkillDef.baseRechargeInterval = 0;
            primarySkillDef.baseMaxStock = 1;
            primarySkillDef.requiredStock = 0;
            primarySkillDef.stockToConsume = 0;
            primarySkillDef.resetCooldownTimerOnUse = false;
            primarySkillDef.fullRestockOnAssign = true;
            primarySkillDef.dontAllowPastMaxStocks = true;
            primarySkillDef.beginSkillCooldownOnSkillEnd = true;
            primarySkillDef.cancelSprintingOnActivation = false;
            primarySkillDef.forceSprintDuringState = false;
            primarySkillDef.canceledFromSprinting = false;
            primarySkillDef.isCombatSkill = true;
            primarySkillDef.mustKeyPress = false;
            SkillsmasContent.Resources.skillDefs.Add(primarySkillDef);

            sideCameraParams = ScriptableObject.CreateInstance<CharacterCameraParams>();
            sideCameraParams.data.idealLocalCameraPos = new Vector3(1.5f, 0f, -6f);
            sideCameraParams.data.pivotVerticalOffset = 0.8f;

            chargeEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimChargingEffect.prefab");
            var objectScaleCurve = chargeEffectPrefab.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = true;
            objectScaleCurve.timeMax = 3f;
            chargeFullEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimChargeFullEffect.prefab");

            muzzleFlashEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/ManualAim/ManualAimFullChargeMuzzleFlash.prefab");
            var effectComponent = muzzleFlashEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_mage_m1_shoot";
            effectComponent.positionAtReferencedTransform = true;
            effectComponent.parentToReferencedTransform = true;
            muzzleFlashEffectPrefab.AddComponent<DestroyOnTimer>().duration = 1f;
            SkillsmasContent.Resources.effectPrefabs.Add(muzzleFlashEffectPrefab);

            crosshairOverridePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/StandardCrosshair.prefab").WaitForCompletion();
        }

        public class ManualAimState : EntityStates.GenericCharacterMain, EntityStates.ISkillState
        {
            public GenericSkill primarySkillSlot;
            public CameraTargetParams.CameraParamsOverrideHandle cameraParamsOverrideHandle;
            public HuntressTracker huntressTracker;
            public bool huntressTrackerWasEnabled;

            public GenericSkill activatorSkillSlot { get; set; }

            public override void OnEnter()
            {
                base.OnEnter();

                if (skillLocator) primarySkillSlot = skillLocator.primary;
                if (primarySkillSlot) primarySkillSlot.SetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);

                Util.PlaySound("Play_loader_R_activate", gameObject);

                if (cameraTargetParams && useSideCamera)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.AddParamsOverride(new CameraTargetParams.CameraParamsOverrideRequest
                    {
                        cameraParamsData = sideCameraParams.data,
                        priority = 1f
                    }, 0.4f);
                }

                huntressTracker = GetComponent<HuntressTracker>();
                if (huntressTracker)
                {
                    huntressTrackerWasEnabled = huntressTracker.enabled;
                    if (huntressTrackerWasEnabled)
                    {
                        huntressTracker.enabled = false;
                        huntressTracker.trackingTarget = null;
                    }
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= 0.1f && inputBank.skill4.justPressed)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override void OnExit()
            {
                if (isAuthority)
                {
                    var weaponStateMachine = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
                    if (weaponStateMachine) weaponStateMachine.SetNextStateToMain();
                }
                if (primarySkillSlot) primarySkillSlot.UnsetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
                activatorSkillSlot.DeductStock(1);
                Util.PlaySound("Play_engi_seekerMissile_lockOn", gameObject);
                if (cameraTargetParams && cameraParamsOverrideHandle.isValid)
                {
                    cameraParamsOverrideHandle = cameraTargetParams.RemoveParamsOverride(cameraParamsOverrideHandle, 0.4f);
                }
                if (huntressTracker && huntressTrackerWasEnabled)
                {
                    huntressTracker.enabled = true;
                }
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }

        public class ChargeArrow : EntityStates.BaseSkillState
        {
            public static float baseChargeDuration = 1f;
            public static float minBloomRadius = 0f;
            public static float maxBloomRadius = 0.5f;
            public static float minChargeDuration = 0.3f;
            public static string muzzleName = "Muzzle";
            public static float huntressFullChargeAnimationTime = 0.34f;

            public float chargeDuration;
            public Animator animator;
            public Transform muzzleTransform;
            public GameObject chargeEffectInstance;
            public RoR2.UI.CrosshairUtils.OverrideRequest crosshairOverrideRequest;
            public uint loopSoundInstanceId;
            public bool playedFullChargeEffects = false;

            public override void OnEnter()
            {
                base.OnEnter();
                chargeDuration = baseChargeDuration / attackSpeedStat;
                
                animator = GetModelAnimator();

                var childLocator = GetModelChildLocator();
                if (childLocator)
                {
                    muzzleTransform = childLocator.FindChild(muzzleName) ?? characterBody.coreTransform;
                    if (muzzleTransform && chargeEffectPrefab)
                    {
                        chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
                        chargeEffectInstance.transform.parent = muzzleTransform;

                        var scaleParticleSystemDuration = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                        if (scaleParticleSystemDuration) scaleParticleSystemDuration.newDuration = chargeDuration;
                        var objectScaleCurve = chargeEffectInstance.GetComponent<ObjectScaleCurve>();
                        if (objectScaleCurve) objectScaleCurve.timeMax = chargeDuration;
                    }
                }

                loopSoundInstanceId = Util.PlayAttackSpeedSound("Play_huntress_R_aim_loop", gameObject, attackSpeedStat);

                if (crosshairOverridePrefab)
                    crosshairOverrideRequest = RoR2.UI.CrosshairUtils.RequestOverrideForBody(characterBody, crosshairOverridePrefab, RoR2.UI.CrosshairUtils.OverridePriority.Skill);
            }

            public float CalcCharge()
            {
                return Mathf.Clamp01(fixedAge / chargeDuration);
            }

            public override void Update()
            {
                base.Update();
                characterBody.SetAimTimer(1f);
                characterBody.SetSpreadBloom(Util.Remap(CalcCharge(), 0f, 1f, minBloomRadius, maxBloomRadius), true);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                var charge = CalcCharge();
                PlayHuntressChargingAnimation("Gesture, Override", charge);
                PlayHuntressChargingAnimation("Gesture, Additive", charge);
                if (!playedFullChargeEffects && charge >= 1f)
                {
                    playedFullChargeEffects = true;

                    PlayFullChargeEffects();
                }

                if (isAuthority && !IsKeyDownAuthority() && fixedAge >= minChargeDuration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public void PlayHuntressChargingAnimation(string layerName, float charge)
            {
                animator.speed = 1f;
                animator.Update(0f);
                var layerIndex = animator.GetLayerIndex(layerName);
                if (layerIndex >= 0)
                {
                    animator.SetFloat("FireSeekingShot.playbackRate", 1f);
                    animator.PlayInFixedTime("FireSeekingShot", layerIndex, Util.Remap(charge, 0f, 1f, 0f, huntressFullChargeAnimationTime));
                    animator.Update(0f);
                    var length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
                    animator.SetFloat("FireSeekingShot.playbackRate", length / 0.3f);
                }
            }

            public void PlayHuntressReleaseAnimation(string layerName)
            {
                animator.speed = 1f;
                animator.Update(0f);
                var layerIndex = animator.GetLayerIndex(layerName);
                if (layerIndex >= 0)
                {
                    animator.SetFloat("FireSeekingShot.playbackRate", 1f);
                    animator.PlayInFixedTime("FireSeekingShot", layerIndex, huntressFullChargeAnimationTime);
                    animator.Update(0f);
                    var length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
                    animator.SetFloat("FireSeekingShot.playbackRate", length / 0.3f);
                }
            }

            public void PlayFullChargeEffects()
            {
                AkSoundEngine.StopPlayingID(loopSoundInstanceId);
                if (chargeEffectInstance) Destroy(chargeEffectInstance);

                EffectManager.SimpleMuzzleFlash(muzzleFlashEffectPrefab, gameObject, muzzleName, false);

                chargeEffectInstance = Object.Instantiate(chargeFullEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
                chargeEffectInstance.transform.parent = muzzleTransform;

                loopSoundInstanceId = Util.PlaySound("Play_item_proc_fireRingTornado_start", gameObject);
            }

            public void FireArrowAuthority()
            {
                var charge = CalcCharge();
                var aimRay = GetAimRay();

                Util.PlaySound("Play_huntress_m1_shoot", gameObject);
                if (charge >= 1f)
                {
                    Util.PlaySound("Play_clayboss_m1_shoot", gameObject);
                }

                var fireProjectileInfo = new FireProjectileInfo
                {
                    damage = Util.Remap(charge, 0f, 1f, minimumDamage, maximumDamage) / 100f * damageStat,
                    crit = characterBody.RollCrit(),
                    position = aimRay.origin,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    owner = gameObject,
                    force = 400f * charge,
                    projectilePrefab = charge < 1f ? projectilePrefab : projectileChargedPrefab
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }

            public override void OnExit()
            {
                if (crosshairOverrideRequest != null) crosshairOverrideRequest.Dispose();
                AkSoundEngine.StopPlayingID(loopSoundInstanceId);
                if (!outer.destroying)
                {
                    // PlayAnimation("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", 0.3f);
                    // PlayAnimation("Gesture, Additive", "FireSeekingShot", "FireSeekingShot.playbackRate", 0.3f);
                    PlayHuntressReleaseAnimation("Gesture, Override");
                    PlayHuntressReleaseAnimation("Gesture, Additive");
                    if (isAuthority) FireArrowAuthority();
                }
                if (chargeEffectInstance) Destroy(chargeEffectInstance);
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}