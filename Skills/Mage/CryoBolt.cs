using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Mage
{
    public class CryoBolt : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Cryo Bolt",
            "Damage",
            280f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_PRIMARY_ICE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        public static ConfigOptions.ConfigurableValue<float> radius;
        public static ConfigOptions.ConfigurableValue<float> lifetime;

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            FireIceBolt.projectilePrefabStatic = Utils.CreateBlankPrefab("Skillsmas_CryoBolt", true);
            FireIceBolt.projectilePrefabStatic.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_CryoBolt";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_PRIMARY_ICE_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_PRIMARY_ICE_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_FREEZING"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/CryoBolt.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireIceBolt));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Artificer: Cryo Bolt",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireIceBolt));

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/CryoBolt/CryoBoltGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            var objectScaleCurve = ghost.transform.Find("Crystal").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = ghost.transform.Find("SnowParticles").GetComponent<ParticleSystem>().inheritVelocity.curve.curve;
            objectScaleCurve.useOverallCurveOnly = true;
            ghost.AddComponent<DetachParticleOnDestroyAndEndEmission>().particleSystem = ghost.transform.Find("SnowParticles").GetComponent<ParticleSystem>();

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/CryoBolt/CryoBoltProjectile.prefab"), FireIceBolt.projectilePrefabStatic);
            var projectileController = FireIceBolt.projectilePrefabStatic.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Cryo Bolt",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            FireIceBolt.projectilePrefabStatic.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = FireIceBolt.projectilePrefabStatic.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 80f;
            var projectileDamage = FireIceBolt.projectilePrefabStatic.AddComponent<ProjectileDamage>();
            projectileDamage.damageType = DamageType.Freeze2s;
            var projectileImpactExplosion = FireIceBolt.projectilePrefabStatic.AddComponent<ProjectileImpactExplosion>();
            projectileImpactExplosion.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Mage/FrozenImpactEffect.prefab").WaitForCompletion();
            projectileImpactExplosion.destroyOnEnemy = true;
            projectileImpactExplosion.destroyOnWorld = true;
            projectileImpactExplosion.lifetime = 99f;
            projectileImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            radius = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Cryo Bolt",
                "Radius",
                2.5f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileImpactExplosion.blastRadius = newValue
            );
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            var proximityDetonator = FireIceBolt.projectilePrefabStatic.transform.Find("ProximityDetonator").gameObject.AddComponent<SkillsmasProjectileProximityDetonator>();
            proximityDetonator.myTeamFilter = FireIceBolt.projectilePrefabStatic.GetComponent<TeamFilter>();
            proximityDetonator.projectileExplosion = projectileImpactExplosion;

            lifetime = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Cryo Bolt",
                "Lifetime",
                0.2f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    objectScaleCurve.timeMax = newValue;
                    projectileSimple.lifetime = newValue;
                }
            );
            
            SkillsmasContent.Resources.projectilePrefabs.Add(FireIceBolt.projectilePrefabStatic);

            FireIceBolt.muzzleflashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Mage/MuzzleflashMageIce.prefab").WaitForCompletion();
        }

        public class FireIceBolt : EntityStates.Mage.Weapon.FireFireBolt
        {
            public static GameObject projectilePrefabStatic;
            public static GameObject muzzleflashEffectPrefabStatic;

            public override void OnEnter()
            {
                projectilePrefab = projectilePrefabStatic;
                muzzleflashEffectPrefab = muzzleflashEffectPrefabStatic;
                damageCoefficient = damage / 100f;
                force = 0f;
                baseDuration = 0.25f;
                attackSoundString = "Play_mage_shift_wall_build";
                attackSoundPitch = 1.3f;
                base.OnEnter();
            }
        }
    }
}