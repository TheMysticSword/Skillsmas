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

namespace Skillsmas.Skills.Mage.Rock
{
    public class RockMeteor : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Superbolide",
            "Damage",
            2000f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SPECIAL_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> radius;
        public static ConfigOptions.ConfigurableValue<float> petrificationDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Superbolide",
            "Petrification Duration",
            4f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SPECIAL_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public static GameObject projectilePrefab;
        public static GameObject explosionEffectPrefab;
        public static DamageAPI.ModdedDamageType petrifyDamageType;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab = Utils.CreateBlankPrefab("Skillsmas_RockMeteor", true);
            projectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            petrifyDamageType = DamageAPI.ReserveDamageType();
            NetworkingAPI.RegisterMessageType<SyncPetrified>();
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RockMeteor";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SPECIAL_ROCK_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SPECIAL_ROCK_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_CRYSTALLIZE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Superbolide.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(DropMeteor));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Superbolide",
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
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(DropMeteor));

            explosionEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockMeteor/RockMeteorExplosionEffect.prefab");
            explosionEffectPrefab.AddComponent<DestroyOnTimer>().duration = 10f;
            var shakeEmitter = explosionEffectPrefab.AddComponent<ShakeEmitter>();
            shakeEmitter.wave = new Wave
            {
                amplitude = 8f,
                frequency = 3f
            };
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.radius = 160f;
            shakeEmitter.duration = 0.6f;
            shakeEmitter.shakeOnStart = true;
            var ppDuration = explosionEffectPrefab.AddComponent<PostProcessDuration>();
            ppDuration.ppVolume = explosionEffectPrefab.GetComponentInChildren<PostProcessVolume>();
            ppDuration.ppWeightCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            ppDuration.maxDuration = 1.2f;
            var effectComponent = explosionEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_captain_utility_variant_impact";
            var vfxAttributes = explosionEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.High;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            SkillsmasContent.Resources.effectPrefabs.Add(explosionEffectPrefab);

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockMeteor/RockMeteorGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            var objectTransformCurve = ghost.transform.Find("Pivot").gameObject.AddComponent<ObjectTransformCurve>();
            objectTransformCurve.useRotationCurves = false;
            objectTransformCurve.useTranslationCurves = true;
            objectTransformCurve.translationCurveZ = AnimationCurve.Linear(0f, -1000f, 1f, -1f);
            var rotateObject = ghost.transform.Find("Pivot/Meteor").gameObject.AddComponent<RotateObject>();
            rotateObject.rotationSpeed = new Vector3(480f, -90f, 240f);
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("Pivot/FlameParticles").GetComponent<ParticleSystem>();
            
            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockMeteor/RockMeteorProjectile.prefab"), projectilePrefab);
            var projectileController = projectilePrefab.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Superbolide",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            projectilePrefab.AddComponent<ProjectileNetworkTransform>();
            var projectileDamage = projectilePrefab.AddComponent<ProjectileDamage>();
            var projectileImpactExplosion = projectilePrefab.AddComponent<ProjectileImpactExplosion>();
            projectileImpactExplosion.explosionEffect = explosionEffectPrefab;
            projectileImpactExplosion.destroyOnEnemy = false;
            projectileImpactExplosion.destroyOnWorld = false;
            projectileImpactExplosion.lifetime = 2f;
            objectTransformCurve.timeMax = projectileImpactExplosion.lifetime;
            projectileImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            radius = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Superbolide",
                "Radius",
                22f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileImpactExplosion.blastRadius = newValue
            );
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            projectileImpactExplosion.lifetimeExpiredSound = Addressables.LoadAssetAsync<NetworkSoundEventDef>("RoR2/Base/Captain/nseCaptainAirstrikeAltPreImpact.asset").WaitForCompletion();
            projectileImpactExplosion.offsetForLifetimeExpiredSound = 1.3f;
            SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);

            DropMeteor.muzzleflashEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageFire.prefab").WaitForCompletion();
            PetrifiedState.petrifiedMaterial = SkillsmasPlugin.AssetBundle.LoadAsset<Material>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockMeteor/matRockMeteorPetrifiedOverlay.mat");

            GenericGameEvents.OnHitEnemy += GenericGameEvents_OnHitEnemy;
        }

        public override void AfterContentPackLoaded()
        {
            var moddedDamageTypeHolder = projectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>();
            moddedDamageTypeHolder.Add(DamageTypes.Crystallize.crystallizeDamageType);
            moddedDamageTypeHolder.Add(petrifyDamageType);
            PetrifiedState.petrifiedEffectPrefab = RockBolt.impactEffectPrefab;
        }

        private static void GenericGameEvents_OnHitEnemy(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo)
        {
            if (damageInfo.HasModdedDamageType(petrifyDamageType) && victimInfo.body)
            {
                var setStateOnHurt = victimInfo.body.GetComponent<SetStateOnHurt>();
                if (setStateOnHurt && setStateOnHurt.targetStateMachine && setStateOnHurt.spawnedOverNetwork && setStateOnHurt.canBeFrozen)
                {
                    SetPetrifiedServer(setStateOnHurt, petrificationDuration);
                }
            }
        }

        public class DropMeteor : EntityStates.BaseState
        {
            public static GameObject muzzleflashEffect;
            public static float baseDuration = 2f;
            public static float entryDuration = 0.3f;

            public float duration;
            public bool playedEntryAnimation = false;
            public Vector3 hitPosition;
            public AimAnimator.DirectionOverrideRequest animatorDirectionOverrideRequest;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration;

                if (characterBody) characterBody.SetAimTimer(duration + 1f);
                PlayAnimation("Gesture, Additive", "PrepFlamethrower", "Flamethrower.playbackRate", entryDuration);

                var aimAnimator = GetAimAnimator();
                if (aimAnimator)
                    animatorDirectionOverrideRequest = aimAnimator.RequestDirectionOverride(GetAimDirection);

                if (isAuthority)
                {
                    var aimRay = GetAimRay();
                    
                    var aimDirectionQuaternion = Util.QuaternionSafeLookRotation(aimRay.direction);
                    var hitDirection = Quaternion.AngleAxis(aimDirectionQuaternion.eulerAngles.y, Vector3.up) * Quaternion.AngleAxis(70f, Vector3.right) * Vector3.forward;

                    var inFrontPosition = transform.position;
                    var aimDirectionXZ = Util.Vector3XZToVector2XY(aimRay.direction).normalized;
                    inFrontPosition += 21f * new Vector3(aimDirectionXZ.x, 0f, aimDirectionXZ.y);
                    hitPosition = inFrontPosition;
                    if (Physics.Raycast(new Ray(inFrontPosition, hitDirection), out var raycastHit, 1000f, LayerIndex.world.mask))
                    {
                        hitPosition = raycastHit.point;
                    }
                    else
                    {
                        if (SceneInfo.instance)
                        {
                            var nodes = SceneInfo.instance.groundNodes;
                            if (nodes)
                            {
                                var nodeIndex = nodes.FindClosestNode(hitPosition, HullClassification.Human);
                                if (!nodes.GetNodePosition(nodeIndex, out hitPosition))
                                {
                                    hitPosition = inFrontPosition;
                                }
                            }
                        }
                    }

                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = projectilePrefab,
                        position = hitPosition,
                        rotation = Util.QuaternionSafeLookRotation(hitDirection),
                        owner = gameObject,
                        damage = damageStat * damage / 100f,
                        force = 4000f,
                        crit = RollCrit()
                    });
                }
            }

            public override void OnSerialize(NetworkWriter writer)
            {
                base.OnSerialize(writer);
                writer.Write(hitPosition);
            }

            public override void OnDeserialize(NetworkReader reader)
            {
                base.OnDeserialize(reader);
                hitPosition = reader.ReadVector3();
            }

            public Vector3 GetAimDirection()
            {
                return (hitPosition - transform.position).normalized;
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (isAuthority)
                {
                    characterBody.isSprinting = false;
                }
                characterDirection.moveVector = GetAimDirection();

                if (age >= entryDuration && !playedEntryAnimation)
                {
                    playedEntryAnimation = true;

                    PlayAnimation("Gesture, Additive", "Flamethrower", "Flamethrower.playbackRate", duration);

                    EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleLeft", false);
                    EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleRight", false);

                    Util.PlaySound("Play_grandParent_attack1_throw", gameObject);
                }

                if (isAuthority && age >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override void Update()
            {
                base.Update();
                characterBody.SetSpreadBloom(Util.Remap(age / duration, 0f, 1f, 0.1f, 0.8f), true);
            }

            public override void OnExit()
            {
                PlayCrossfade("Gesture, Additive", "ExitFlamethrower", 0.1f);
                if (animatorDirectionOverrideRequest != null)
                    animatorDirectionOverrideRequest.Dispose();
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Pain;
            }
        }

        public class PetrifiedState : EntityStates.BaseState
        {
            private Animator modelAnimator;
            private TemporaryOverlay temporaryOverlay;
            public float duration = 0.35f;
            public static GameObject petrifiedEffectPrefab;
            public static Material petrifiedMaterial;

            public override void OnEnter()
            {
                base.OnEnter();
                if (sfxLocator && sfxLocator.barkSound != "")
                {
                    Util.PlaySound(sfxLocator.barkSound, gameObject);
                }
                var modelTransform = GetModelTransform();
                if (modelTransform)
                {
                    var characterModel = modelTransform.GetComponent<CharacterModel>();
                    if (characterModel)
                    {
                        temporaryOverlay = gameObject.AddComponent<TemporaryOverlay>();
                        temporaryOverlay.duration = duration;
                        temporaryOverlay.originalMaterial = petrifiedMaterial;
                        temporaryOverlay.alphaCurve = AnimationCurve.Constant(0f, 1f, 1f);
                        temporaryOverlay.animateShaderAlpha = true;
                        temporaryOverlay.AddToCharacerModel(characterModel);
                    }
                }
                modelAnimator = GetModelAnimator();
                if (modelAnimator)
                {
                    modelAnimator.enabled = false;
                    if (petrifiedEffectPrefab)
                        EffectManager.SpawnEffect(petrifiedEffectPrefab, new EffectData
                        {
                            origin = characterBody.corePosition,
                            scale = characterBody ? characterBody.radius : 1f
                        }, false);
                }
                if (rigidbody && !rigidbody.isKinematic)
                {
                    rigidbody.velocity = Vector3.zero;
                    if (rigidbodyMotor)
                    {
                        rigidbodyMotor.moveVector = Vector3.zero;
                    }
                }
                if (characterDirection)
                {
                    characterDirection.moveVector = characterDirection.forward;
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override void OnExit()
            {
                if (modelAnimator)
                {
                    modelAnimator.enabled = true;
                }
                if (temporaryOverlay)
                {
                    Destroy(temporaryOverlay);
                }
                if (petrifiedEffectPrefab)
                    EffectManager.SpawnEffect(petrifiedEffectPrefab, new EffectData
                    {
                        origin = characterBody.corePosition,
                        scale = characterBody ? characterBody.radius : 1f
                    }, false);
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Frozen;
            }
        }

        public class SyncPetrified : INetMessage
        {
            NetworkInstanceId objID;
            float duration;

            public SyncPetrified()
            {
            }

            public SyncPetrified(NetworkInstanceId objID, float duration)
            {
                this.objID = objID;
                this.duration = duration;
            }

            public void Deserialize(NetworkReader reader)
            {
                objID = reader.ReadNetworkId();
                duration = reader.ReadSingle();
            }

            public void OnReceived()
            {
                if (NetworkServer.active) return;
                var obj = Util.FindNetworkObject(objID);
                if (obj)
                {
                    var setStateOnHurt = obj.GetComponent<SetStateOnHurt>();
                    if (setStateOnHurt)
                    {
                        SetPetrifiedLocal(setStateOnHurt, duration);
                    }
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(objID);
                writer.Write(duration);
            }
        }

        public static void SetPetrifiedServer(SetStateOnHurt setStateOnHurt, float duration)
        {
            if (!NetworkServer.active) return;
            if (!setStateOnHurt.canBeFrozen) return;
            if (setStateOnHurt.hasEffectiveAuthority)
            {
                SetPetrifiedLocal(setStateOnHurt, duration);
            }
            else
            {
                new SyncPetrified(setStateOnHurt.GetComponent<NetworkIdentity>().netId, duration).Send(NetworkDestination.Clients);
            }
        }

        public static void SetPetrifiedLocal(SetStateOnHurt setStateOnHurt, float duration)
        {
            if (setStateOnHurt.targetStateMachine)
            {
                var petrifiedState = new PetrifiedState();
                petrifiedState.duration = duration;
                setStateOnHurt.targetStateMachine.SetInterruptState(petrifiedState, EntityStates.InterruptPriority.Frozen);
            }
            var array = setStateOnHurt.idleStateMachine;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].SetNextState(new EntityStates.Idle());
            }
        }
    }
}