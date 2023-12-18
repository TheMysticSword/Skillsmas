using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using R2API.Networking;
using System.Collections.Generic;
using System.Linq;
using RoR2.Orbs;

namespace Skillsmas.Skills.Bandit
{
    public class SuperBleedBarrage : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Bloodbath",
            "Damage",
            600f,
            max: 100000f,
            stringsToAffect: new List<string>
            {
                "BANDIT2_SKILLSMAS_SUPERBLEEDBARRAGE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Bloodbath",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<int> shots = ConfigOptions.ConfigurableValue.CreateInt(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Bandit: Bloodbath",
            "Shots",
            6,
            stringsToAffect: new List<string>
            {
                "BANDIT2_SKILLSMAS_SUPERBLEEDBARRAGE_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_SuperBleedBarrage";
            skillDef.skillNameToken = "BANDIT2_SKILLSMAS_SUPERBLEEDBARRAGE_NAME";
            skillDef.skillDescriptionToken = "BANDIT2_SKILLSMAS_SUPERBLEEDBARRAGE_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/Bloodbath.jpg");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireSidearmSuperBleedBarrageRevolver));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Bandit: Bloodbath",
                baseRechargeInterval: 4f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: true,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Bandit2/Bandit2BodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            FireSidearmSuperBleedBarrageRevolver.effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/MuzzleflashBandit2.prefab").WaitForCompletion();
            FireSidearmSuperBleedBarrageRevolver.tracerEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/TracerBandit2Rifle.prefab").WaitForCompletion();
            FireSidearmSuperBleedBarrageRevolver.hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/HitsparkBandit2Pistol.prefab").WaitForCompletion();
            FireSidearmSuperBleedBarrageRevolver.crosshairOverridePrefabStatic = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Bandit2/Bandit2CrosshairPrepRevolverFire.prefab").WaitForCompletion();
        }

        public class FireSidearmSuperBleedBarrageRevolver : EntityStates.Bandit2.Weapon.BaseSidearmState
        {
            public static GameObject effectPrefab;
            public static GameObject hitEffectPrefab;
            public static GameObject tracerEffectPrefab;
            public static GameObject crosshairOverridePrefabStatic;
            public static float recoilAmplitude = 2f;
            public static float force = 1500f;

            public int shotsDone = 0;
            public float shotsPerSecond = 0f;
            public float shotTimer = 0f;
            public BullseyeSearch search;
            public List<HurtBox> targets;
            public List<HurtBox> shotTargets = new List<HurtBox>();

            public override void OnEnter()
            {
                baseDuration = 0.7f;
                base.OnEnter();

                search = new BullseyeSearch
                {
                    filterByLoS = true,
                    filterByDistinctEntity = true,
                    teamMaskFilter = TeamMask.GetUnprotectedTeams(teamComponent.teamIndex),
                    maxAngleFilter = 80f
                };

                shotsPerSecond = shots / duration;
                shotTimer = 1f; // attempt to fire the first shot immediately
            }

            public void RefreshTargets()
            {
                var aimRay = GetAimRay();
                search.searchOrigin = aimRay.origin;
                search.searchDirection = aimRay.direction;
                search.RefreshCandidates();
                var allTargets = search.GetResults().Where(x => x.healthComponent.body.HasBuff(RoR2Content.Buffs.SuperBleed)).ToList();
                var unshotTargets = allTargets.Where(x => !shotTargets.Contains(x)).ToList();

                if (unshotTargets.Count <= 0)
                {
                    targets = allTargets;
                    shotTargets.Clear();
                }
                else targets = unshotTargets;
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (shotsDone < shots)
                {
                    shotTimer += shotsPerSecond * Time.fixedDeltaTime;
                    while (shotTimer >= 1f)
                    {
                        shotTimer -= 1f;
                        FireBullet();
                        shotsDone++;
                    }
                }
                else
                {
                    if (isAuthority)
                    {
                        outer.SetNextState(new EntityStates.Bandit2.Weapon.ExitSidearmRevolver());
                    }
                }
            }

            public void FireBullet()
            {
                RefreshTargets();

                if (targets.Count > 0)
                {
                    var muzzleName = "MuzzlePistol";
                    var aimRay = GetAimRay();

                    AddRecoil(-3f * recoilAmplitude, -4f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
                    StartAimMode(aimRay, 2f, false);
                    Util.PlaySound("Play_bandit2_R_fire", gameObject);
                    PlayAnimation("Gesture, Additive", "FireSideWeapon", "FireSideWeapon.playbackRate", duration);
                    if (effectPrefab)
                        EffectManager.SimpleMuzzleFlash(effectPrefab, gameObject, muzzleName, false);

                    var target = RoR2Application.rng.NextElementUniform(targets);
                    shotTargets.Add(target);
                    targets.Remove(target);

                    var bulletAttack = new BulletAttack
                    {
                        owner = gameObject,
                        weapon = gameObject,
                        origin = aimRay.origin,
                        aimVector = (target.transform.position - aimRay.origin).normalized,
                        bulletCount = 1,
                        damage = damage / 100f * damageStat,
                        procCoefficient = procCoefficient,
                        force = force,
                        isCrit = RollCrit(),
                        falloffModel = BulletAttack.FalloffModel.None,
                        muzzleName = muzzleName,
                        tracerEffectPrefab = tracerEffectPrefab,
                        hitEffectPrefab = hitEffectPrefab,
                        HitEffectNormal = false,
                        radius = 1f,
                        smartCollision = true
                    };
                    bulletAttack.Fire();
                }
                else
                {
                    if (isAuthority)
                    {
                        outer.SetNextState(new EntityStates.Bandit2.Weapon.ExitSidearmRevolver());
                    }
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }
    }
}