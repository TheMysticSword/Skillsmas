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
    public class TrackerGlaive : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Huntress: Tracker Glaive",
            "Damage",
            640f,
            stringsToAffect: new List<string>
            {
                "HUNTRESS_SKILLSMAS_TRACKERGLAIVE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient;

        public static GameObject projectilePrefab;

        public override void OnPluginAwake()
        {
            base.OnPluginAwake();
            projectilePrefab = Utils.CreateBlankPrefab("Skillsmas_TrackerGlaiveProjectile", true);
            projectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_TrackerGlaive";
            skillDef.skillNameToken = "HUNTRESS_SKILLSMAS_TRACKERGLAIVE_NAME";
            skillDef.skillDescriptionToken = "HUNTRESS_SKILLSMAS_TRACKERGLAIVE_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/TrackerGlaive.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ThrowTrackerGlaive));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Huntress: Tracker Glaive",
                baseRechargeInterval: 7f,
                baseMaxStock: 1,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Huntress/HuntressBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(ThrowTrackerGlaive));

            var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/TrackerGlaive/TrackerGlaiveGhost.prefab");
            ghost.AddComponent<ProjectileGhostController>();

            Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Huntress/TrackerGlaive/TrackerGlaiveProjectile.prefab"), projectilePrefab);
            var projectileController = projectilePrefab.AddComponent<ProjectileController>();
            projectileController.allowPrediction = true;
            projectileController.ghostPrefab = ghost;
            procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Huntress: Tracker Glaive",
                "Proc Coefficient",
                1f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => projectileController.procCoefficient = newValue
            );
            projectilePrefab.AddComponent<ProjectileNetworkTransform>();
            var projectileSimple = projectilePrefab.AddComponent<ProjectileSimple>();
            projectileSimple.desiredForwardSpeed = 200f;
            projectileSimple.lifetime = 6f;
            var projectileDamage = projectilePrefab.AddComponent<ProjectileDamage>();
            var projectileSingleTargetImpact = projectilePrefab.AddComponent<ProjectileSingleTargetImpact>();
            projectileSingleTargetImpact.destroyWhenNotAlive = false;
            projectileSingleTargetImpact.destroyOnWorld = true;
            projectileSingleTargetImpact.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/OmniImpactVFXSlashMerc.prefab").WaitForCompletion();
            var projectileStickOnImpact = projectilePrefab.AddComponent<ProjectileStickOnImpact>();
            projectileStickOnImpact.alignNormals = false;
            projectilePrefab.AddComponent<SkillsmasTrackerGlaive>();
            projectilePrefab.AddComponent<SkillsmasAlignToRigidbodyVelocity>();

            SkillsmasContent.Resources.projectilePrefabs.Add(projectilePrefab);

            On.EntityStates.Huntress.HuntressWeapon.FireSeekingArrow.FireOrbArrow += FireSeekingArrow_FireOrbArrow;
        }

        private void FireSeekingArrow_FireOrbArrow(On.EntityStates.Huntress.HuntressWeapon.FireSeekingArrow.orig_FireOrbArrow orig, EntityStates.Huntress.HuntressWeapon.FireSeekingArrow self)
        {
            if (self.firedArrowCount < self.maxArrowCount && self.arrowReloadTimer <= 0f && NetworkServer.active)
            {
                var trackerGlaiveTargets = InstanceTracker.GetInstancesList<SkillsmasTrackerGlaive>()
                .Where(x => x.projectileController.owner == self.gameObject && x.projectileStickOnImpact.stuckBody && x.projectileStickOnImpact.stuckTransform)
                .Select(x => x.projectileStickOnImpact.stuckTransform.GetComponent<HurtBox>())
                .Where(x => x != null)
                .ToList();

                foreach (var trackerGlaiveTarget in trackerGlaiveTargets)
                {
                    var genericDamageOrb = self.CreateArrowOrb();
                    genericDamageOrb.damageValue = self.characterBody.damage * self.orbDamageCoefficient;
                    genericDamageOrb.isCrit = self.isCrit;
                    genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(self.gameObject);
                    genericDamageOrb.attacker = self.gameObject;
                    genericDamageOrb.procCoefficient = self.orbProcCoefficient;
                    var muzzleTransform = self.childLocator.FindChild(self.muzzleString);
                    genericDamageOrb.origin = muzzleTransform.position;
                    genericDamageOrb.target = trackerGlaiveTarget;
                    OrbManager.instance.AddOrb(genericDamageOrb);

                    EffectManager.SimpleMuzzleFlash(self.muzzleflashEffectPrefab, self.gameObject, self.muzzleString, true);
                }
            }

            orig(self);
        }

        public class SkillsmasTrackerGlaive : MonoBehaviour
        {
            public ProjectileController projectileController;
            public ProjectileStickOnImpact projectileStickOnImpact;

            public void Awake()
            {
                projectileController = GetComponent<ProjectileController>();
                projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
            }

            public void OnEnable()
            {
                InstanceTracker.Add(this);
            }

            public void OnDisable()
            {
                InstanceTracker.Remove(this);
            }
        }

        public class ThrowTrackerGlaive : EntityStates.GenericProjectileBaseState
        {
            public override void OnEnter()
            {
                damageCoefficient = damage / 100f;
                force = 500f;
                baseDuration = 1.1f;
                recoilAmplitude = 0f;
                attackSoundString = "Play_huntress_m2_throw";
                baseDelayBeforeFiringProjectile = 0.63f;
                effectPrefab = EntityStates.Huntress.HuntressWeapon.ThrowGlaive.muzzleFlashPrefab;

                base.OnEnter();

                projectilePrefab = TrackerGlaive.projectilePrefab;

                if (isAuthority && characterMotor)
                {
                    characterMotor.velocity.y = EntityStates.Huntress.HuntressWeapon.ThrowGlaive.smallHopStrength;
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && characterMotor)
                {
                    characterMotor.velocity.y = characterMotor.velocity.y + EntityStates.Huntress.HuntressWeapon.ThrowGlaive.antigravityStrength * Time.fixedDeltaTime * (1f - stopwatch / duration);
                }
            }

            public override void PlayAnimation(float duration)
            {
                base.PlayAnimation("FullBody, Override", "ThrowGlaive", "ThrowGlaive.playbackRate", duration);
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}