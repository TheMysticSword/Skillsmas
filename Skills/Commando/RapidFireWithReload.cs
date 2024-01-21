using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Commando
{
    public class RapidFireWithReload : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> damage = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Burst Fire",
            "Damage",
            100f,
            stringsToAffect: new List<string>
            {
                "COMMANDO_SKILLSMAS_RAPIDFIREWITHRELOAD_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> procCoefficient = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Burst Fire",
            "Proc Coefficient",
            1f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> shotsPerSecond = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Commando: Burst Fire",
            "Shots Per Second",
            10f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override System.Type GetSkillDefType()
        {
            return typeof(SteppedSkillDef);
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_RapidFireWithReload";
            skillDef.skillNameToken = "COMMANDO_SKILLSMAS_RAPIDFIREWITHRELOAD_NAME";
            skillDef.skillDescriptionToken = "COMMANDO_SKILLSMAS_RAPIDFIREWITHRELOAD_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/BurstFire.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(RapidFireState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Commando: Burst Fire",
                baseRechargeInterval: 2f,
                baseMaxStock: 60,
                rechargeStock: 60,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: false
            );
            skillDef.resetCooldownTimerOnUse = true;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var customSkillDef = skillDef as SteppedSkillDef;
            customSkillDef.stepCount = 12;
            customSkillDef.stepGraceDuration = 1f;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Commando/CommandoBodyPrimaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(RapidFireState));

            configMaxStock.stringsToAffect.Add(skillDef.skillDescriptionToken);
            On.EntityStates.Commando.DodgeState.OnEnter += DodgeState_OnEnter;
        }

        private void DodgeState_OnEnter(On.EntityStates.Commando.DodgeState.orig_OnEnter orig, EntityStates.Commando.DodgeState self)
        {
            // fix for tactical dive force-setting primary stocks to 12

            GenericSkill primary = null;
            if (self.skillLocator.primary &&
                self.skillLocator.primary.baseSkill != null &&
                self.skillLocator.primary.baseSkill.skillName == "Skillsmas_RapidFireWithReload")
            {
                primary = self.skillLocator.primary;
            }

            var primaryRechargeStopwatch = 0f;
            var primaryStock = 0;
            if (primary)
            {
                primaryRechargeStopwatch = primary.rechargeStopwatch;
                primaryStock = primary.stock;
            }
            orig(self);
            if (primary)
            {
                primary.rechargeStopwatch = primaryRechargeStopwatch;
                primary.stock = primaryStock;
            }
        }

        public class RapidFireState : EntityStates.BaseSkillState, SteppedSkillDef.IStepSetter
        {
            public static float recoilAmplitude = 0.4f;

            public float duration;
            public int step;
            public Ray aimRay;

            void SteppedSkillDef.IStepSetter.SetStep(int i)
            {
                step = i;
            }

            public override void OnEnter()
            {
                base.OnEnter();
                duration = 1f / shotsPerSecond / attackSpeedStat;
                aimRay = GetAimRay();
                StartAimMode(aimRay, 3f, false);
                if ((step % 12) < 6)
                {
                    PlayAnimation("Gesture Additive, Left", "FirePistol, Left");
                    FireBullet("MuzzleLeft");
                }
                else
                {
                    PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
                    FireBullet("MuzzleRight");
                }
            }

            public void FireBullet(string targetMuzzle)
            {
                Util.PlayAttackSpeedSound("Play_commando_R", gameObject, 1.8f);
                if (EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab)
                    EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, gameObject, targetMuzzle, false);
                AddRecoil(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
                characterBody.AddSpreadBloom(0.1f);

                if (isAuthority)
                {
                    new BulletAttack
                    {
                        owner = gameObject,
                        weapon = gameObject,
                        origin = aimRay.origin,
                        aimVector = aimRay.direction,
                        minSpread = 0f,
                        maxSpread = characterBody.spreadBloomAngle,
                        damage = damage / 100f * damageStat,
                        procCoefficient = procCoefficient,
                        force = 100f,
                        tracerEffectPrefab = EntityStates.Commando.CommandoWeapon.FirePistol2.tracerEffectPrefab,
                        muzzleName = targetMuzzle,
                        hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab,
                        isCrit = characterBody.RollCrit(),
                        radius = 0.1f,
                        smartCollision = true
                    }.Fire();
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration)
                {
                    if (activatorSkillSlot.stock <= 0)
                    {
                        outer.SetNextState(new Reload());
                        return;
                    }
                    outer.SetNextStateToMain();
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }

        public class Reload : EntityStates.Commando.CommandoWeapon.ReloadPistols
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Commando.CommandoWeapon.ReloadPistols));
                enterSoundString = "Play_bandit2_R_load";
                base.OnEnter();
            }
        }
    }
}