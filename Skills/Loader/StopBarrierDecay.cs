using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Skillsmas.Skills.Loader
{
    public class StopBarrierDecay : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> attackSpeed = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Loader: Reinforce",
            "Attack Speed",
            40f,
            stringsToAffect: new List<string>
            {
                "LOADER_SKILLSMAS_STOPBARRIERDECAY_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> buffDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Loader: Reinforce",
            "Buff Duration",
            7f,
            stringsToAffect: new List<string>
            {
                "LOADER_SKILLSMAS_STOPBARRIERDECAY_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_StopBarrierDecay";
            skillDef.skillNameToken = "LOADER_SKILLSMAS_STOPBARRIERDECAY_NAME";
            skillDef.skillDescriptionToken = "LOADER_SKILLSMAS_STOPBARRIERDECAY_DESCRIPTION";
            skillDef.icon = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();
            skillDef.activationStateMachineName = "Pylon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(StopBarrierDecayState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Loader: Reinforce",
                baseRechargeInterval: 20f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: false,
                forceSprintDuringState: false,
                canceledFromSprinting: false,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = false;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Loader/LoaderBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            StopBarrierDecayState.muzzleFlashPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Loader/OmniImpactVFXLoaderLightning.prefab").WaitForCompletion();
        }

        public class StopBarrierDecayState : EntityStates.BaseState
        {
            public static GameObject muzzleFlashPrefab;
            public static float baseDuration = 0.25f;

            public float duration;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;
                if (NetworkServer.active)
                {
                    characterBody.AddTimedBuff(SkillsmasContent.Buffs.Skillsmas_StopBarrierDecay, buffDuration);
                }
                EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, gameObject, "MuzzleCenter", false);
                Util.PlaySound("Play_mage_m1_impact_lightning", gameObject);
                Util.PlaySound("Play_loader_R_shock", gameObject);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && age >= duration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Frozen;
            }
        }
    }
}