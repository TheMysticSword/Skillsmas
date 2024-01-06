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
    public class RockBolt : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Geode Bolt",
            "Damage",
            280f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_PRIMARY_ROCK_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> radius;
        public static ConfigOptions.ConfigurableValue<float> lifetime;

        public static GameObject impactEffectPrefab;

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            FireRockBolt.projectilePrefabStatic = Utils.CreateBlankPrefab("Skillsmas_RockBolt", true);
            FireRockBolt.projectilePrefabStatic.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RockBolt";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_PRIMARY_ROCK_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_PRIMARY_ROCK_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_CRYSTALLIZE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/CryoBolt.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireRockBolt));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Artificer: Geode Bolt",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireRockBolt));

            impactEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBolt/RockBoltImpactEffect.prefab");
            impactEffectPrefab.AddComponent<DestroyOnTimer>().duration = 5f;
            var shakeEmitter = impactEffectPrefab.AddComponent<ShakeEmitter>();
            shakeEmitter.wave = new Wave
            {
                amplitude = 0.2f,
                frequency = 8f
            };
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.radius = 40f;
            shakeEmitter.duration = 0.2f;
            shakeEmitter.shakeOnStart = true;
            var effectComponent = impactEffectPrefab.AddComponent<EffectComponent>();
            effectComponent.soundName = "Play_commando_M1";
            var vfxAttributes = impactEffectPrefab.AddComponent<VFXAttributes>();
            vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            SkillsmasContent.Resources.effectPrefabs.Add(impactEffectPrefab);

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBolt/RockBoltGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            
            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/RockBolt/RockBoltProjectile.prefab"), FireRockBolt.projectilePrefabStatic);
            var projectileController = FireRockBolt.projectilePrefabStatic.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Geode Bolt",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            FireRockBolt.projectilePrefabStatic.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = FireRockBolt.projectilePrefabStatic.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 80f;
            projectileSimple.lifetime = 2f;
            var projectileDamage = FireRockBolt.projectilePrefabStatic.AddComponent<ProjectileDamage>();
            var projectileImpactExplosion = FireRockBolt.projectilePrefabStatic.AddComponent<ProjectileImpactExplosion>();
            projectileImpactExplosion.impactEffect = impactEffectPrefab;
            projectileImpactExplosion.destroyOnEnemy = true;
            projectileImpactExplosion.destroyOnWorld = true;
            projectileImpactExplosion.lifetime = 99f;
            projectileImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            radius = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Geode Bolt",
                "Radius",
                2.5f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileImpactExplosion.blastRadius = newValue
            );
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            var proximityDetonator = FireRockBolt.projectilePrefabStatic.transform.Find("ProximityDetonator").gameObject.AddComponent<SkillsmasProjectileProximityDetonator>();
            proximityDetonator.myTeamFilter = FireRockBolt.projectilePrefabStatic.GetComponent<TeamFilter>();
            proximityDetonator.projectileExplosion = projectileImpactExplosion;
            
            SkillsmasContent.Resources.projectilePrefabs.Add(FireRockBolt.projectilePrefabStatic);

            FireRockBolt.muzzleflashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageFire.prefab").WaitForCompletion();
        }

        public override void AfterContentPackLoaded()
        {
            FireRockBolt.projectilePrefabStatic.AddComponent<DamageAPI.ModdedDamageTypeHolderComponent>().Add(DamageTypes.Crystallize.crystallizeDamageType);
        }

        public class FireRockBolt : EntityStates.Mage.Weapon.FireFireBolt
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
                attackSoundString = "Play_golem_impact";
                attackSoundPitch = 0.87f;
                base.OnEnter();
            }
        }
    }
}