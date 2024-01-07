using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Mage.Rock
{
    public class RockBomb : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> minDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Moulded Nano-Boulder",
            "Minimum Damage",
            400f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> maxDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Moulded Nano-Boulder",
            "Maximum Damage",
            2000f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;

        public static GameObject lifetimeExpiredEffect;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            ThrowRockBomb.projectilePrefabStatic = Utils.CreateBlankPrefab("Skillsmas_RockBomb", true);
            ThrowRockBomb.projectilePrefabStatic.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RockBomb";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SECONDARY_ROCK_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SECONDARY_ROCK_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_CRYSTALLIZE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/MouldedNanoBoulder.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ChargeRockBomb));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Moulded Nano-Boulder",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ChargeRockBomb));
            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ThrowRockBomb));

            lifetimeExpiredEffect = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBomb/RockBombLifetimeExpiredEffect.prefab");
            lifetimeExpiredEffect.AddComponent<DestroyOnTimer>().duration = 5f;
            var shakeEmitter = lifetimeExpiredEffect.AddComponent<ShakeEmitter>();
            shakeEmitter.wave = new Wave
            {
                amplitude = 3f,
                frequency = 9f
            };
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.radius = 30f;
            shakeEmitter.duration = 0.2f;
            shakeEmitter.shakeOnStart = true;
            var effectComponent = lifetimeExpiredEffect.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_golem_impact";
            var vfxAttributes = lifetimeExpiredEffect.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            SkillsmasContent.Resources.effectPrefabs.Add(lifetimeExpiredEffect);

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBomb/RockBombGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            var objectScaleCurve = ghost.transform.Find("Mesh").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = true;
            objectScaleCurve.timeMax = 0.14f;
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("ParticleDirtyRocks").GetComponent<ParticleSystem>();
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("ParticleDust").GetComponent<ParticleSystem>();

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBomb/RockBombProjectile.prefab"), ThrowRockBomb.projectilePrefabStatic);
            var projectileController = ThrowRockBomb.projectilePrefabStatic.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Moulded Nano-Boulder",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            ThrowRockBomb.projectilePrefabStatic.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = ThrowRockBomb.projectilePrefabStatic.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 40f;
            projectileSimple.lifetime = 4f;
            projectileSimple.lifetimeExpiredEffect = lifetimeExpiredEffect;
            var projectileDamage = ThrowRockBomb.projectilePrefabStatic.AddComponent<ProjectileDamage>();
            var hitboxGroup = ThrowRockBomb.projectilePrefabStatic.AddComponent<HitBoxGroup>();
            hitboxGroup.groupName = "Boulder";
            hitboxGroup.hitBoxes = new[]
            {
                ThrowRockBomb.projectilePrefabStatic.transform.Find("HitBox").gameObject.AddComponent<HitBox>()
            };
            var projectileOverlapAttack = ThrowRockBomb.projectilePrefabStatic.AddComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.damageCoefficient = 1f;
            projectileOverlapAttack.resetInterval = -1f;
            projectileOverlapAttack.overlapProcCoefficient = 1f;
            projectileOverlapAttack.fireFrequency = 20f;

            SkillsmasContent.Resources.projectilePrefabs.Add(ThrowRockBomb.projectilePrefabStatic);

            ChargeRockBomb.chargeEffectPrefabStatic = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBomb/ChargeRockBomb.prefab");
            objectScaleCurve = ChargeRockBomb.chargeEffectPrefabStatic.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = true;
            objectScaleCurve.timeMax = 3f;
            var rotateObject = ChargeRockBomb.chargeEffectPrefabStatic.transform.Find("MeshRotator").gameObject.AddComponent<RotateObject>();
            rotateObject.rotationSpeed = new Vector3(-360f, 140f, -220f);

            ThrowRockBomb.muzzleFlashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBruiser/ClayShockwaveEffect.prefab").WaitForCompletion();
        }

        public override void AfterContentPackLoaded()
        {
            ThrowRockBomb.projectilePrefabStatic.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(DamageTypes.Crystallize.crystallizeDamageType);
            ThrowRockBomb.projectilePrefabStatic.GetComponent<ProjectileOverlapAttack>().impactEffect = RockBolt.impactEffectPrefab;
        }

        public class ChargeRockBomb : EntityStates.Mage.Weapon.BaseChargeBombState
        {
            public static GameObject chargeEffectPrefabStatic;

            public override EntityStates.Mage.Weapon.BaseThrowBombState GetNextState()
            {
                return new ThrowRockBomb();
            }

            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Mage.Weapon.ChargeIcebomb));
                chargeEffectPrefab = chargeEffectPrefabStatic;
                chargeSoundString = "Play_titanboss_R_laser_preshoot";
                baseDuration = 2f;
                minBloomRadius = 0.1f;
                maxBloomRadius = 0.5f;
                base.OnEnter();
            }
        }

        public class ThrowRockBomb : EntityStates.Mage.Weapon.BaseThrowBombState
        {
            public static GameObject projectilePrefabStatic;
            public static GameObject muzzleFlashEffectPrefabStatic;

            public override void OnEnter()
            {
                projectilePrefab = projectilePrefabStatic;
                muzzleflashEffectPrefab = muzzleFlashEffectPrefabStatic;
                baseDuration = 0.4f;
                minDamageCoefficient = minDamage / 100f;
                maxDamageCoefficient = maxDamage / 100f;
                force = 3000f;
                selfForce = 1000f;
                base.OnEnter();
                Util.PlaySound("Play_clayBruiser_attack2_shoot", gameObject);
            }
        }
    }
}