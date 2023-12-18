using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using RoR2.Orbs;

namespace Skillsmas.Skills.Croco
{
    public class PoisonBubble : BaseSkill
    {
        public static GameObject projectilePrefab;
        public static GameObject projectilePrefabGhost;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Acrid: Autonomous Organism",
            "Damage",
            100f,
            stringsToAffect: new List<string>
            {
                "CROCO_SKILLSMAS_POISONBUBBLE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> ticksPerSecond;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab = Utils.CreateBlankPrefab("Skillsmas_PoisonBubbleProjectile", true);
            projectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_PoisonBubble";
            skillDef.skillNameToken = "CROCO_SKILLSMAS_POISONBUBBLE_NAME";
            skillDef.skillDescriptionToken = "CROCO_SKILLSMAS_POISONBUBBLE_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_POISON"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/PoisonBubble.jpg");
            skillDef.activationStateMachineName = "Mouth";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FirePoisonBubbleProjectile));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Acrid: Autonomous Organism",
                baseRechargeInterval: 15f,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Croco/CrocoBodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            projectilePrefabGhost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Acrid/PoisonBubble/BubbleProjectileGhost.prefab");
            projectilePrefabGhost.AddComponent<ProjectileGhostController>();
            var objectScaleCurve = projectilePrefabGhost.transform.Find("Scaler").gameObject.AddComponent<ObjectScaleCurve>();
            objectScaleCurve.overallCurve = projectilePrefabGhost.transform.Find("Scaler/Particle System").GetComponent<ParticleSystem>().sizeBySpeed.size.curve;
            objectScaleCurve.timeMax = 15f;
            objectScaleCurve.useOverallCurveOnly = true;
            projectilePrefabGhost.transform.Find("Scaler/SphereInner").transform.localScale = Vector3.one * 0.4f;
            projectilePrefabGhost.transform.Find("Scaler/Particle System").transform.localScale = Vector3.one * 1.2f;

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Acrid/PoisonBubble/BubbleProjectile.prefab"), projectilePrefab);
            var projectileController = projectilePrefab.AddComponent<ProjectileController>();
            projectileController.ghostPrefab = projectilePrefabGhost;
            projectileController.allowPrediction = true;
            projectilePrefab.AddComponent<ProjectileNetworkTransform>();
            projectilePrefab.AddComponent<TeamFilter>();
            var projectileSimple = projectilePrefab.AddComponent<ProjectileSimple>();
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Acrid: Autonomous Organism",
                "Speed",
                7f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileSimple.desiredForwardSpeed = newValue
            );
            var projectileDamage = projectilePrefab.AddComponent<ProjectileDamage>();
            projectileDamage.damageType = DamageType.PoisonOnHit;
            var hitboxGroup = projectilePrefab.AddComponent<HitBoxGroup>();
            hitboxGroup.groupName = "PoisonBubble";
            var hitboxes = new List<HitBox>();
            foreach (var collider in projectilePrefab.transform.Find("HitBoxes").GetComponentsInChildren<Collider>())
            {
                hitboxes.Add(collider.gameObject.AddComponent<HitBox>());
            }
            hitboxGroup.hitBoxes = hitboxes.ToArray();
            var projectileOverlapAttack = projectilePrefab.AddComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.damageCoefficient = 1f;
            ticksPerSecond = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Acrid: Autonomous Organism",
                "Ticks Per Second",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileOverlapAttack.resetInterval = 1f / newValue
            );
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Acrid: Autonomous Organism",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileOverlapAttack.overlapProcCoefficient = newValue
            );
            // projectileOverlapAttack.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoDiseaseImpactEffect.prefab").WaitForCompletion();

            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Acrid: Autonomous Organism",
                "Radius",
                15f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    projectilePrefab.transform.localScale = Vector3.one * newValue;
                    projectilePrefabGhost.transform.localScale = Vector3.one * newValue;
                }
            );
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Acrid: Autonomous Organism",
                "Lifetime",
                10f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) =>
                {
                    objectScaleCurve.timeMax = newValue;
                    projectileSimple.lifetime = newValue;
                }
            );

            projectilePrefab.AddComponent<LoopSoundPlayer>().loopDef = Addressables.LoadAssetAsync<RoR2.Audio.LoopSoundDef>("RoR2/Base/MiniMushroom/lsdSporeGrenadeGasCloud.asset").WaitForCompletion();

            SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);
        }

        public class FirePoisonBubbleProjectile : EntityStates.Croco.FireSpit
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Croco.FireSpit));
                projectilePrefab = PoisonBubble.projectilePrefab;
                baseDuration = 0.5f;
                damageCoefficient = damage / 100f / ticksPerSecond;
                force = 0f;
                attackString = "Play_acrid_m2_shoot";
                recoilAmplitude = 0.5f;
                bloom = 1f;
                base.OnEnter();
                Util.PlaySound("Play_minimushroom_spore_explode", gameObject);
                if (isAuthority && characterMotor)
                {
                    var knockbackForce = -8000f * GetAimRay().direction;
                    knockbackForce.y = 6000f;
                    characterMotor.ApplyForce(knockbackForce, true, false);
                }
            }
        }
    }
}