using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Toolbot
{
    public class Crate : BaseSkill
    {
        public static GameObject crateProjectilePrefab;
        public static GameObject crateGhostPrefab;

        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "MUL-T: Shipping Crate",
            "Damage",
            400f,
            stringsToAffect: new List<string>
            {
                "TOOLBOT_SKILLSMAS_CRATE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            crateProjectilePrefab = Utils.CreateBlankPrefab("Skillsmas_CrateProjectile", true);
            crateProjectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
            crateProjectilePrefab.AddComponent<NetworkTransform>();
            crateProjectilePrefab.layer = LayerIndex.world.intVal;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_Crate";
            skillDef.skillNameToken = "TOOLBOT_SKILLSMAS_CRATE_NAME";
            skillDef.skillDescriptionToken = "TOOLBOT_SKILLSMAS_CRATE_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Crate.jpg");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(AimCrate));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "MUL-T: Shipping Crate",
                baseRechargeInterval: 6f,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Toolbot/ToolbotBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(AimCrate));

            crateGhostPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/MUL-T/Crate/CrateGhost.prefab");
            crateGhostPrefab.AddComponent<ProjectileGhostController>();

            var cratePodClone = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/RoboCratePod.prefab").WaitForCompletion(), "Skillsmas_TempCrateCopy");
            var meshObj = cratePodClone.transform.Find("Base/mdlRoboCrate/Base/RobotCrateMesh");
            Object.Destroy(cratePodClone);
            crateGhostPrefab.transform.Find("Mesh").GetComponent<MeshFilter>().sharedMesh = meshObj.GetComponent<MeshFilter>().sharedMesh;
            crateGhostPrefab.transform.Find("Mesh").GetComponent<MeshRenderer>().sharedMaterial = meshObj.GetComponent<MeshRenderer>().sharedMaterial;

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/MUL-T/Crate/CrateProjectile.prefab"), crateProjectilePrefab);
            var projectileController = crateProjectilePrefab.AddComponent<ProjectileController>();
            projectileController.ghostPrefab = crateGhostPrefab;
            projectileController.allowPrediction = true;
            crateProjectilePrefab.AddComponent<ProjectileNetworkTransform>();
            crateProjectilePrefab.AddComponent<TeamFilter>();

            var projectileSimple = crateProjectilePrefab.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 20f;
            projectileSimple.lifetime = 600f;

            var projectileDamage = crateProjectilePrefab.AddComponent<ProjectileDamage>();

            var hitBoxGroup = crateProjectilePrefab.AddComponent<HitBoxGroup>();
            hitBoxGroup.hitBoxes = new[]
            {
                crateProjectilePrefab.transform.Find("HitBox").gameObject.AddComponent<HitBox>()
            };

            var projectileOverlapAttack = crateProjectilePrefab.AddComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.damageCoefficient = 1f;
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "MUL-T: Shipping Crate",
                "Proc Coefficient",
                1f,
                onChanged: (newValue) => projectileOverlapAttack.overlapProcCoefficient = newValue,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
            );
            projectileOverlapAttack.resetInterval = 10f;
            projectileOverlapAttack.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/ImpactToolbotDash.prefab").WaitForCompletion();

            var crateController = crateProjectilePrefab.AddComponent<SkillsmasCrateController>();

            crateProjectilePrefab.transform.Find("Collider").gameObject.AddComponent<EntityLocator>().entity = crateProjectilePrefab;
            crateProjectilePrefab.transform.Find("Collider/ProjectileBlockingCollider").gameObject.AddComponent<EntityLocator>().entity = crateProjectilePrefab;

            crateController.disabledCollider = crateProjectilePrefab.transform.Find("Collider").GetComponent<Collider>();

            SkillsmasContent.Resources.projectilePrefabs.Add(crateProjectilePrefab);

            On.RoR2.GlobalEventManager.OnHitAll += GlobalEventManager_OnHitAll;
        }

        private void GlobalEventManager_OnHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (hitObject)
            {
                var entity = EntityLocator.GetEntity(hitObject);
                if (entity)
                {
                    var crate = entity.GetComponent<SkillsmasCrateController>();
                    if (crate)
                    {
                        crate.rigidbody.AddForce(damageInfo.force, ForceMode.Impulse);
                    }
                }
            }
        }

        public class SkillsmasCrateController : MonoBehaviour
        {
            public Rigidbody rigidbody;
            public ProjectileOverlapAttack overlapAttack;
            public Collider disabledCollider;
            public float disabledColliderDuration = 0.1f;

            public void Awake()
            {
                rigidbody = GetComponent<Rigidbody>();
                overlapAttack = GetComponent<ProjectileOverlapAttack>();
            }

            public void FixedUpdate()
            {
                var isMoving = rigidbody.velocity.sqrMagnitude >= 25f;
                overlapAttack.enabled = isMoving;

                if (disabledCollider && disabledColliderDuration > 0f)
                {
                    disabledColliderDuration -= Time.fixedDeltaTime;
                    if (disabledColliderDuration <= 0f)
                    {
                        disabledCollider.enabled = true;
                    }
                }
            }
        }

        public class AimCrate : EntityStates.AimThrowableBase
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Toolbot.AimStunDrone));
                maxDistance = 20f;
                rayRadius = 0f;
                endpointVisualizerRadiusScale = 0f;
                damageCoefficient = damage / 100f;
                baseMinimumDuration = 0.25f;
                projectilePrefab = crateProjectilePrefab;
                base.OnEnter();

                detonationRadius = 3f;
                if (endpointVisualizerTransform) endpointVisualizerTransform.localScale = Vector3.one * detonationRadius;
                UpdateVisualizers(currentTrajectoryInfo);

                Util.PlaySound("Play_MULT_m2_aim", gameObject);
                PlayAnimation("Gesture, Additive", "PrepBomb", "PrepBomb.playbackRate", minimumDuration);
                PlayAnimation("Stance, Override", "PutAwayGun");
            }

            public override void OnExit()
            {
                outer.SetNextState(new EntityStates.Toolbot.RecoverAimStunDrone());
                Util.PlaySound("Play_MULT_m2_throw", gameObject);
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}