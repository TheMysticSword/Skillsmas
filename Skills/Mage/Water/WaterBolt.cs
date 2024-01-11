using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Mage.Water
{
    public class WaterBolt : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Water Bolt",
            "Damage",
            280f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_PRIMARY_WATER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> radius;
        public static ConfigOptions.ConfigurableValue<float> lifetime;

        public static GameObject explosionEffectPrefab;

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            FireWaterBolt.projectilePrefabStatic = Utils.CreateBlankPrefab("Skillsmas_WaterBolt", true);
            FireWaterBolt.projectilePrefabStatic.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_WaterBolt";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_PRIMARY_WATER_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_PRIMARY_WATER_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_REVITALIZING"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/WaterBolt.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireWaterBolt));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Artificer: Water Bolt",
                baseRechargeInterval: 1.3f,
                baseMaxStock: 4,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: false
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var customSkillDef = skillDef as SteppedSkillDef;
            customSkillDef.stepCount = 2;
            customSkillDef.stepGraceDuration = 3f;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyPrimaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireWaterBolt));

            explosionEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterBolt/WaterBoltExplosionEffect.prefab");
            explosionEffectPrefab.AddComponent<DestroyOnTimer>().duration = 5f;
            var effectComponent = explosionEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_artifactBoss_attack1_explode";
            var vfxAttributes = explosionEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            SkillsmasContent.Resources.effectPrefabs.Add(explosionEffectPrefab);

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterBolt/WaterBoltGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            
            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/WaterBolt/WaterBoltProjectile.prefab"), FireWaterBolt.projectilePrefabStatic);
            var projectileController = FireWaterBolt.projectilePrefabStatic.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Water Bolt",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            FireWaterBolt.projectilePrefabStatic.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = FireWaterBolt.projectilePrefabStatic.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 80f;
            projectileSimple.lifetime = 2f;
            var projectileDamage = FireWaterBolt.projectilePrefabStatic.AddComponent<ProjectileDamage>();
            var projectileImpactExplosion = FireWaterBolt.projectilePrefabStatic.AddComponent<ProjectileImpactExplosion>();
            projectileImpactExplosion.explosionEffect = explosionEffectPrefab;
            projectileImpactExplosion.destroyOnEnemy = true;
            projectileImpactExplosion.destroyOnWorld = true;
            projectileImpactExplosion.lifetime = 99f;
            projectileImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            radius = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Water Bolt",
                "Radius",
                2.5f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileImpactExplosion.blastRadius = newValue
            );
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            var proximityDetonator = FireWaterBolt.projectilePrefabStatic.transform.Find("ProximityDetonator").gameObject.AddComponent<SkillsmasProjectileProximityDetonator>();
            proximityDetonator.myTeamFilter = FireWaterBolt.projectilePrefabStatic.GetComponent<TeamFilter>();
            proximityDetonator.projectileExplosion = projectileImpactExplosion;
            
            SkillsmasContent.Resources.projectilePrefabs.Add(FireWaterBolt.projectilePrefabStatic);

            FireWaterBolt.muzzleflashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightning.prefab").WaitForCompletion();
        }

        public override void AfterContentPackLoaded()
        {
            FireWaterBolt.projectilePrefabStatic.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(DamageTypes.Revitalizing.revitalizingDamageType);
        }

        public class FireWaterBolt : EntityStates.Mage.Weapon.FireFireBolt
        {
            public static GameObject projectilePrefabStatic;
            public static GameObject muzzleflashEffectPrefabStatic;

            public override void OnEnter()
            {
                projectilePrefab = projectilePrefabStatic;
                muzzleflashEffectPrefab = muzzleflashEffectPrefabStatic;
                damageCoefficient = damage / 100f;
                force = 300f;
                baseDuration = 0.25f;
                attackSoundString = "Play_item_use_cleanse";
                attackSoundPitch = 1.17f;
                base.OnEnter();
            }
        }
    }
}