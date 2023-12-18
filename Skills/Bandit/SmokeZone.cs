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
    public class SmokeZone : BaseSkill
    {
        public static GameObject zonePrefab;

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_SmokeZone";
            skillDef.skillNameToken = "BANDIT2_SKILLSMAS_SMOKEZONE_NAME";
            skillDef.skillDescriptionToken = "BANDIT2_SKILLSMAS_SMOKEZONE_DESCRIPTION";
            skillDef.icon = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffCloakIcon.tif").WaitForCompletion();
            skillDef.activationStateMachineName = "Stealth";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(ThrowSmokeDevice));
            skillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            SetUpValuesAndOptions(
                "Bandit: Smoke Machine",
                baseRechargeInterval: 16f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: false,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Bandit2/Bandit2BodyUtilityFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            zonePrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Bandit/SmokeZone/SmokeZone.prefab");

            var detachHelper = zonePrefab.AddComponent<DetachParticleOnDestroyAndEndEmission>();
            detachHelper.particleSystem = zonePrefab.transform.Find("VisualIndicator/Particle System").GetComponent<ParticleSystem>();

            zonePrefab.AddComponent<TeamFilter>();
            var buffWard = zonePrefab.AddComponent<BuffWard>();
            buffWard.buffDuration = 1f;
            buffWard.interval = 0.5f;
            buffWard.floorWard = false;
            buffWard.shape = BuffWard.BuffWardShape.Sphere;
            buffWard.rangeIndicator = zonePrefab.transform.Find("VisualIndicator");
            buffWard.expires = true;
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Bandit: Smoke Machine",
                "Duration",
                10f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => buffWard.expireDuration = newValue
            );
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                "Bandit: Smoke Machine",
                "Radius",
                18f,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => buffWard.radius = newValue
            );
            RoR2Application.onLoad += () => buffWard.buffDef = RoR2Content.Buffs.Cloak;
        }

        public class ThrowSmokeDevice : EntityStates.BaseState
        {
            public static float baseDuration = 0.2f;

            public float duration;
            public bool fired = false;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;
                PlayAnimation("Gesture, Additive", "ThrowSmokebomb", "ThrowSmokebomb.playbackRate", duration);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (fixedAge > duration)
                {
                    if (!fired)
                    {
                        FireSmokeZone();
                        fired = true;
                    }
                    outer.SetNextStateToMain();
                }
            }

            public void FireSmokeZone()
            {
                Util.PlaySound(EntityStates.Bandit2.StealthMode.exitStealthSound, gameObject);

                if (characterMotor)
                {
                    characterMotor.velocity = new Vector3(characterMotor.velocity.x, EntityStates.Bandit2.StealthMode.shortHopVelocity, characterMotor.velocity.z);
                }

                if (EntityStates.Bandit2.StealthMode.smokeBombEffectPrefab)
                {
                    EffectManager.SimpleMuzzleFlash(EntityStates.Bandit2.StealthMode.smokeBombEffectPrefab, gameObject, EntityStates.Bandit2.StealthMode.smokeBombMuzzleString, false);
                }

                if (NetworkServer.active)
                {
                    var zone = Object.Instantiate(zonePrefab, transform.position, Quaternion.identity);
                    zone.GetComponent<TeamFilter>().teamIndex = teamComponent.teamIndex;
                    NetworkServer.Spawn(zone);
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.PrioritySkill;
            }
        }
    }
}