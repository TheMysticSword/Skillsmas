using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Mage.Fire
{
    public class FlameBomb : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> minDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Lit Nano-Rocket",
            "Minimum Damage",
            400f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_FIRE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> maxDamage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Artificer: Lit Nano-Rocket",
            "Maximum Damage",
            2000f,
            stringsToAffect: new List<string>
            {
                "MAGE_SKILLSMAS_SECONDARY_FIRE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;
        
        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            ThrowFlameBomb.projectilePrefabStatic = Utils.CreateBlankPrefab("Skillsmas_FlameBomb", true);
            ThrowFlameBomb.projectilePrefabStatic.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_FlameBomb";
            skillDef.skillNameToken = "MAGE_SKILLSMAS_SECONDARY_FIRE_NAME";
            skillDef.skillDescriptionToken = "MAGE_SKILLSMAS_SECONDARY_FIRE_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_IGNITE"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/LitNanoRocket.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ChargeFlameBomb));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Artificer: Lit Nano-Rocket",
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

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ChargeFlameBomb));
            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ThrowFlameBomb));

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/FlameBomb/FlameBombGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();
            ghost.AddComponent<DetachTrailOnDestroy>().targetTrailRenderers = new[]
            {
                ghost.transform.Find("Trail").GetComponent<TrailRenderer>()
            };

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/FlameBomb/FlameBombProjectile.prefab"), ThrowFlameBomb.projectilePrefabStatic);
            var projectileController = ThrowFlameBomb.projectilePrefabStatic.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Artificer: Lit Nano-Rocket",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            ThrowFlameBomb.projectilePrefabStatic.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = ThrowFlameBomb.projectilePrefabStatic.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 500f;
            projectileSimple.lifetime = 3f;
            var projectileDamage = ThrowFlameBomb.projectilePrefabStatic.AddComponent<ProjectileDamage>();
            projectileDamage.damageType = DamageType.IgniteOnHit;
            var projectileSingleTargetImpact = ThrowFlameBomb.projectilePrefabStatic.AddComponent<ProjectileSingleTargetImpact>();
            projectileSingleTargetImpact.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/QuestVolatileBattery/VolatileBatteryExplosion.prefab").WaitForCompletion();
            projectileSingleTargetImpact.destroyOnWorld = true;
            
            SkillsmasContent.Resources.projectilePrefabs.Add(ThrowFlameBomb.projectilePrefabStatic);

            ChargeFlameBomb.chargeEffectPrefabStatic = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/FlameBomb/ChargeFlameBomb.prefab");
            var objectScaleCurve = ChargeFlameBomb.chargeEffectPrefabStatic.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            objectScaleCurve.useOverallCurveOnly = true;
            objectScaleCurve.timeMax = 3f;
            var rotateObject = ChargeFlameBomb.chargeEffectPrefabStatic.transform.Find("MeshRotator").gameObject.AddComponent<RotateObject>();
            rotateObject.rotationSpeed = new Vector3(280f, 140f, -220f);

            ThrowFlameBomb.muzzleFlashEffectPrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/FireballsOnHit/MuzzleflashFireMeatBall.prefab").WaitForCompletion();
        }

        public class ChargeFlameBomb : EntityStates.Mage.Weapon.BaseChargeBombState
        {
            public static GameObject chargeEffectPrefabStatic;

            public override EntityStates.Mage.Weapon.BaseThrowBombState GetNextState()
            {
                return new ThrowFlameBomb();
            }

            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Mage.Weapon.ChargeIcebomb));
                chargeEffectPrefab = chargeEffectPrefabStatic;
                chargeSoundString = "Play_mage_m2_charge";
                baseDuration = 2f;
                minBloomRadius = 0.1f;
                maxBloomRadius = 0.5f;
                base.OnEnter();
            }
        }

        public class ThrowFlameBomb : EntityStates.Mage.Weapon.BaseThrowBombState
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
                force = 4000f;
                selfForce = 2000f;
                base.OnEnter();
            }
        }
    }
}