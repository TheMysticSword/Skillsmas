using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;

namespace Skillsmas.Skills.Loader
{
    public class QuickGrapple : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> launchPower = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Loader: Zip Fist",
            "Launch Power",
            7500f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> antigravityDuration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Loader: Zip Fist",
            "Antigravity Duration",
            0.3f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_QuickGrapple";
            skillDef.skillNameToken = "LOADER_SKILLSMAS_QUICKGRAPPLE_NAME";
            skillDef.skillDescriptionToken = "LOADER_SKILLSMAS_QUICKGRAPPLE_DESCRIPTION";
            skillDef.icon = Addressables.LoadAssetAsync<Sprite>("RoR2/Junk/Common/texBuffTempestSpeedIcon.png").WaitForCompletion();
            skillDef.activationStateMachineName = "Hook";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(FireQuickHook));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Any;
            SetUpValuesAndOptions(
                "Loader: Zip Fist",
                baseRechargeInterval: 5f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 0,
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

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Loader/LoaderBodySecondaryFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            FireQuickHook.projectilePrefabStatic = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Loader/LoaderHook.prefab").WaitForCompletion(), "LoaderQuickHook");
            SkillsmasContent.Resources.projectilePrefabs.Add(FireQuickHook.projectilePrefabStatic);
            FireQuickHook.projectilePrefabStatic.GetComponent<ProjectileGrappleController>().ownerHookStateType = new EntityStates.SerializableEntityStateType(typeof(FireQuickHook));
        }

        public class FireQuickHook : EntityStates.Loader.FireHook
        {
            public static GameObject projectilePrefabStatic;

            public bool retracted = false;

            public override void OnEnter()
            {
                projectilePrefab = projectilePrefabStatic;
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && isStuck && !retracted && hookInstance)
                {
                    if (characterBody && characterMotor)
                    {
                        var direction = (hookInstance.transform.position - characterBody.aimOrigin).normalized;
                        characterMotor.ApplyForce(launchPower * direction, true, true);

                        retracted = true;
                        outer.SetNextState(new QuickHookAntigravity());
                    }
                }
            }
        }

        public class QuickHookAntigravity : EntityStates.EntityState
        {
            public override void OnEnter()
            {
                base.OnEnter();
                if (isAuthority && characterMotor)
                {
                    var gravityParameters = characterMotor.gravityParameters;
                    gravityParameters.environmentalAntiGravityGranterCount++;
                    characterMotor.gravityParameters = gravityParameters;

                    characterMotor.onHitGroundAuthority += CharacterMotor_onHitGroundAuthority;
                }
            }

            private void CharacterMotor_onHitGroundAuthority(ref CharacterMotor.HitGroundInfo hitGroundInfo)
            {
                characterMotor.onHitGroundAuthority -= CharacterMotor_onHitGroundAuthority;
                outer.SetNextStateToMain();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (fixedAge >= antigravityDuration)
                {
                    outer.SetNextStateToMain();
                }
            }

            public override void OnExit()
            {
                if (isAuthority && characterMotor)
                {
                    var gravityParameters = characterMotor.gravityParameters;
                    gravityParameters.environmentalAntiGravityGranterCount--;
                    characterMotor.gravityParameters = gravityParameters;
                }
                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Pain;
            }
        }
    }
}