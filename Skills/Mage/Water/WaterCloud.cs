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
    public class WaterCloud : BaseSkill
    {
        public static GameObject waterCloudProjectilePrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Cumulonimbus",
            "Damage Per Second",
            100f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_UTILITY_WATER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> tickFrequency;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            waterCloudProjectilePrefab = Utils.CreateBlankPrefab("Skillsmas_LightningPillar", true);
            waterCloudProjectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_WaterCloud";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_UTILITY_WATER_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_UTILITY_WATER_DESCRIPTION";
            var keywordTokens = new List<string>()
            {
                "KEYWORD_SKILLSMAS_REVITALIZING"
            };
            if (SkillsmasPlugin.artificerExtendedEnabled) keywordTokens.Add("KEYWORD_SKILLSMAS_ARTIFICEREXTENDED_ALTPASSIVE_WATER");
            skillDef.keywordTokens = keywordTokens.ToArray();
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Cumulonimbus.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(PrepWaterCloud));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Artificer: Cumulonimbus",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(PrepWaterCloud));

            var expireEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterCloud/WaterCloudExpireEffect.prefab");
            expireEffectPrefab.AddComponent<DestroyOnTimer>().duration = 2f;
            var effectComponent = expireEffectPrefab.AddComponent<EffectComponent>();
            var vfxAttributes = expireEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            SkillsmasContent.Resources.effectPrefabs.Add(expireEffectPrefab);

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterCloud/WaterCloudGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            var objectScaleCurve = ghost.transform.Find("Scaler").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.curveX = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            objectScaleCurve.curveY = AnimationCurve.Constant(0f, 1f, 1f);
            objectScaleCurve.curveZ = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            objectScaleCurve.overallCurve = AnimationCurve.Constant(0f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = false;
            objectScaleCurve.timeMax = 0.4f;
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("Scaler/Cloud/Droplets").GetComponent<ParticleSystem>();
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("Scaler/Cloud/Droplets, Shiny").GetComponent<ParticleSystem>();
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("Scaler/Cloud/LightningFlickering").GetComponent<ParticleSystem>();

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterCloud/WaterCloudProjectile.prefab"), waterCloudProjectilePrefab);
            var projectileController = waterCloudProjectilePrefab.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            projectileController.startSound = "Play_commando_shift";
            projectileController.flightSoundLoop = ScriptableObject.CreateInstance<LoopSoundDef>();
            projectileController.flightSoundLoop.startSoundName = "Play_miniMushroom_selfHeal_loop";
            projectileController.flightSoundLoop.stopSoundName = "Stop_miniMushroom_selfHeal_loop";
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Cumulonimbus",
                "Proc Coefficient",
                0.2f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            waterCloudProjectilePrefab.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = waterCloudProjectilePrefab.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 0f;
            projectileSimple.lifetime = 8f;
            projectileSimple.lifetimeExpiredEffect = expireEffectPrefab;
            var projectileDamage = waterCloudProjectilePrefab.AddComponent<ProjectileDamage>();
            var hitboxGroup = waterCloudProjectilePrefab.AddComponent<HitBoxGroup>();
            hitboxGroup.groupName = "Cloud";
            hitboxGroup.hitBoxes = new[]
            {
                waterCloudProjectilePrefab.transform.Find("HitBox").gameObject.AddComponent<HitBox>(),
                waterCloudProjectilePrefab.transform.Find("HitBox (1)").gameObject.AddComponent<HitBox>(),
                waterCloudProjectilePrefab.transform.Find("HitBox (2)").gameObject.AddComponent<HitBox>()
            };
            var projectileOverlapAttack = waterCloudProjectilePrefab.AddComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.damageCoefficient = 1f;
            projectileOverlapAttack.overlapProcCoefficient = 1f;
            tickFrequency = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Cumulonimbus",
                "Tick Frequency",
                5f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    projectileOverlapAttack.resetInterval = 1f / newValue;
                    projectileOverlapAttack.fireFrequency = Mathf.Max(20f, newValue);

                    projectileOverlapAttack.forceVector = 600f * Vector3.down / newValue;
                }
            );

            SkillsmasContent.Resources.projectilePrefabs.Add(waterCloudProjectilePrefab);

            var fireWallAreaIndicator = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/FirewallAreaIndicator.prefab").WaitForCompletion();
            PrepWaterCloud.areaIndicatorPrefabStatic = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterCloud/WaterCloudAreaIndicator.prefab");
            PrepWaterCloud.areaIndicatorPrefabStatic.transform.Find("Mesh").GetComponent<MeshRenderer>().sharedMaterial = fireWallAreaIndicator.transform.Find("Mesh").GetComponent<MeshRenderer>().sharedMaterial;

            PrepWaterCloud.muzzleflashEffectStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightning.prefab").WaitForCompletion();
            PrepWaterCloud.goodCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/SimpleDotCrosshair.prefab").WaitForCompletion();
            PrepWaterCloud.badCrosshairPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/BadCrosshair.prefab").WaitForCompletion();
        }

        public override void AfterContentPackLoaded()
        {
            waterCloudProjectilePrefab.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(DamageTypes.Revitalizing.revitalizingDamageType);
        }

        public class PrepWaterCloud : PrepCustomWall
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
                if (isAuthority)
                {
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = waterCloudProjectilePrefab,
                        position = position,
                        rotation = rotation,
                        owner = gameObject,
                        damage = damageStat * damage / 100f / tickFrequency,
                        force = 0f,
                        crit = RollCrit()
                    });
                }
            }
        }
    }
}