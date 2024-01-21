using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using MysticsRisky2Utils.MonoBehaviours;

namespace Skillsmas.Skills.Commando
{
    public class TrueFMJ : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Phase Beam",
            "Damage",
            280f,
            stringsToAffect: new List<string>
            {
                "COMMANDO_SKILLSMAS_TRUEFMJ_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Phase Beam",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public static GameObject muzzleFlashEffectPrefab;
        public static GameObject tracerEffectPrefab;
        public static GameObject hitEffectPrefab;

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_TrueFMJ";
            skillDef.skillNameToken = "COMMANDO_SKILLSMAS_TRUEFMJ_NAME";
            skillDef.skillDescriptionToken = "COMMANDO_SKILLSMAS_TRUEFMJ_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/PhaseBeam.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireTrueFMJ));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Commando: Phase Beam",
                baseRechargeInterval: 3f,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Commando/CommandoBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(FireTrueFMJ));

            tracerEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Commando/TrueFMJ/TracerCommandoTrueFMJ.prefab");
            tracerEffectPrefab.AddComponent<EffectComponent>();
            tracerEffectPrefab.AddComponent<DestroyOnTimer>().duration = 0.5f;
            var tracer = tracerEffectPrefab.AddComponent<Tracer>();
            tracer.headTransform = tracerEffectPrefab.transform.Find("TracerHead");
            tracer.tailTransform = tracerEffectPrefab.transform.Find("TracerTail");
            tracer.speed = 1000;
            tracer.length = 1000;
            tracer.beamObject = tracerEffectPrefab.transform.Find("Rings").gameObject;
            tracer.beamDensity = 0.3f;
            var beamPointsFromTransforms = tracerEffectPrefab.AddComponent<BeamPointsFromTransforms>();
            beamPointsFromTransforms.target = tracerEffectPrefab.GetComponent<LineRenderer>();
            beamPointsFromTransforms.pointTransforms = new[]
            {
                tracer.headTransform,
                tracer.tailTransform
            };
            var lineWidthOverTime = tracerEffectPrefab.gameObject.AddComponent<MysticsRisky2UtilsLineWidthOverTime>();
            lineWidthOverTime.animationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            lineWidthOverTime.maxDuration = 0.3f;
            SkillsmasContent.Resources.effectPrefabs.Add(tracerEffectPrefab);

            hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/HitsparkCommandoShotgun.prefab").WaitForCompletion();
            muzzleFlashEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/MuzzleflashFMJ.prefab").WaitForCompletion();
        }

        public class FireTrueFMJ : EntityStates.BaseState
        {
            public static float recoilAmplitude = 1.5f;

            public float duration;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = 0.5f / attackSpeedStat;

                if (characterBody) characterBody.SetAimTimer(2f);
                PlayAnimation("Gesture, Additive", "FireFMJ", "FireFMJ.playbackRate", duration);
                PlayAnimation("Gesture, Override", "FireFMJ", "FireFMJ.playbackRate", duration);
                Util.PlaySound("Play_commando_M2", gameObject);
                Util.PlaySound("Play_vagrant_attack1_land", gameObject);
                AddRecoil(-2f * recoilAmplitude, -3f * recoilAmplitude, -1f * recoilAmplitude, 1f * recoilAmplitude);
                if (muzzleFlashEffectPrefab) EffectManager.SimpleMuzzleFlash(muzzleFlashEffectPrefab, gameObject, "MuzzleCenter", false);
                characterBody.AddSpreadBloom(-1);

                if (isAuthority)
                {
                    var aimRay = GetAimRay();
                    new BulletAttack
                    {
                        owner = gameObject,
                        weapon = gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        maxDistance = 1000f,
                        minSpread = 0f,
                        maxSpread = 0f,
                        damage = damage / 100f * damageStat,
                        force = 3000f,
                        tracerEffectPrefab = tracerEffectPrefab,
                        muzzleName = "MuzzleCenter",
                        hitEffectPrefab = hitEffectPrefab,
                        isCrit = characterBody.RollCrit(),
                        radius = 1f,
                        smartCollision = true,
                        stopperMask = LayerIndex.world.mask,
                        falloffModel = BulletAttack.FalloffModel.None
                    }.Fire();
                    if (characterMotor) characterMotor.ApplyForce(-1500f * aimRay.direction, true);
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}