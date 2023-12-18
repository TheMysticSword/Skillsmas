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

namespace Skillsmas.Skills.Captain
{
    public class LendMicrobots : BaseSkill
    {
        public static GameObject targetIndicatorPrefab;
        public static SkillDef primarySkillDef;
        
        public static ConfigOptions.ConfigurableValue<float> duration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "Captain: Helping Hand",
            "Duration",
            10f,
            stringsToAffect: new List<string>
            {
                "CAPTAIN_SKILLSMAS_LENDMICROBOTS_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_LendMicrobots";
            skillDef.skillNameToken = "CAPTAIN_SKILLSMAS_LENDMICROBOTS_NAME";
            skillDef.skillDescriptionToken = "CAPTAIN_SKILLSMAS_LENDMICROBOTS_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/HelpingHand.jpg");
            skillDef.activationStateMachineName = "Skillswap";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(LendMicrobotsState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Captain: Helping Hand",
                baseRechargeInterval: 12f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 1,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: true,
                isCombatSkill: false,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Captain/CaptainSecondarySkillFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            targetIndicatorPrefab = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("Prefabs/WoodSpriteIndicator"), "Skillsmas_LendMicrobotsIndicator", false);
            targetIndicatorPrefab.GetComponentInChildren<SpriteRenderer>().sprite = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/Skills/Captain/LendMicrobots/texLendMicrobotsIndicator.png");
            targetIndicatorPrefab.GetComponentInChildren<SpriteRenderer>().color = new Color32(178, 255, 253, 255);
            targetIndicatorPrefab.GetComponentInChildren<SpriteRenderer>().transform.rotation = Quaternion.identity;
            targetIndicatorPrefab.GetComponentInChildren<TMPro.TextMeshPro>().color = new Color32(178, 255, 253, 255);
            targetIndicatorPrefab.GetComponentInChildren<InputBindingDisplayController>().actionName = "PrimarySkill";

            LendMicrobotsState.effectMuzzlePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeMuzzleEffect.prefab").WaitForCompletion();
            LendMicrobotsState.crosshairOverridePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeCrosshair.prefab").WaitForCompletion();

            primarySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            primarySkillDef.skillName = "Skillsmas_LendMicrobotsConfirm";
            ((ScriptableObject)primarySkillDef).name = primarySkillDef.skillName;
            primarySkillDef.skillNameToken = skillDef.skillNameToken;
            primarySkillDef.skillDescriptionToken = skillDef.skillDescriptionToken;
            primarySkillDef.icon = skillDef.icon;
            primarySkillDef.activationStateMachineName = "Weapon";
            primarySkillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(LendMicrobotsPrimaryPressState));
            primarySkillDef.interruptPriority = EntityStates.InterruptPriority.PrioritySkill;
            primarySkillDef.baseRechargeInterval = 0;
            primarySkillDef.baseMaxStock = 1;
            primarySkillDef.requiredStock = 0;
            primarySkillDef.stockToConsume = 0;
            primarySkillDef.resetCooldownTimerOnUse = false;
            primarySkillDef.fullRestockOnAssign = true;
            primarySkillDef.dontAllowPastMaxStocks = true;
            primarySkillDef.beginSkillCooldownOnSkillEnd = true;
            primarySkillDef.cancelSprintingOnActivation = true;
            primarySkillDef.forceSprintDuringState = false;
            primarySkillDef.canceledFromSprinting = true;
            primarySkillDef.isCombatSkill = false;
            primarySkillDef.mustKeyPress = true;
            SkillsmasContent.Resources.skillDefs.Add(primarySkillDef);
        }

        public class LendMicrobotsState : EntityStates.BaseSkillState
        {
            public static GameObject effectMuzzlePrefab;
            public static GameObject crosshairOverridePrefab;
            
            public BullseyeSearch allySearch;
            public Indicator indicator;
            public GenericSkill primarySkillSlot;
            public Animator modelAnimator;
            public GameObject effectMuzzleInstance;
            public RoR2.UI.CrosshairUtils.OverrideRequest crosshairOverrideRequest;
            public bool deductStock = false;

            public static BullseyeSearch CreateAllySearch(CharacterBody owner)
            {
                var allySearch = new BullseyeSearch
                {
                    maxAngleFilter = 10f,
                    maxDistanceFilter = 100f,
                    filterByLoS = true,
                    sortMode = BullseyeSearch.SortMode.Angle,
                    teamMaskFilter = TeamMask.none
                };
                allySearch.teamMaskFilter.AddTeam(owner.teamComponent.teamIndex);
                return allySearch;
            }

            public static HurtBox GetTarget(GameObject selfGameObject, BullseyeSearch allySearch, Ray aimRay)
            {
                float extraRaycastDistance;
                aimRay = CameraRigController.ModifyAimRayIfApplicable(aimRay, selfGameObject, out extraRaycastDistance);
                allySearch.searchOrigin = aimRay.origin;
                allySearch.searchDirection = aimRay.direction;
                allySearch.maxDistanceFilter = 100f + extraRaycastDistance;
                allySearch.RefreshCandidates();
                allySearch.FilterOutGameObject(selfGameObject);
                var result = allySearch.GetResults().FirstOrDefault();
                return result;
            }
            
            public override void OnEnter()
            {
                base.OnEnter();

                allySearch = CreateAllySearch(characterBody);
                indicator = new Indicator(gameObject, targetIndicatorPrefab);

                if (skillLocator) primarySkillSlot = skillLocator.primary;

                if (primarySkillSlot) primarySkillSlot.SetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
                
                modelAnimator = GetModelAnimator();
                if (modelAnimator) modelAnimator.SetBool("PrepAirstrike", true);
                PlayCrossfade("Gesture, Override", "PrepAirstrike", 0.1f);
                PlayCrossfade("Gesture, Additive", "PrepAirstrike", 0.1f);
                var muzzleTransform = FindModelChild("MuzzleHandRadio");
                if (effectMuzzlePrefab && muzzleTransform)
                    effectMuzzleInstance = Object.Instantiate(effectMuzzlePrefab, muzzleTransform);
                if (crosshairOverridePrefab)
                    crosshairOverrideRequest = RoR2.UI.CrosshairUtils.RequestOverrideForBody(characterBody, crosshairOverridePrefab, RoR2.UI.CrosshairUtils.OverridePriority.Skill);
                Util.PlaySound("Play_captain_shift_start", gameObject);
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                var result = GetTarget(gameObject, allySearch, GetAimRay());
                if (result)
                {
                    indicator.active = true;
                    indicator.targetTransform = result.transform;
                }
                else
                {
                    indicator.targetTransform = null;
                    indicator.active = false;
                }

                if (characterDirection) characterDirection.moveVector = GetAimRay().direction;
                if (isAuthority)
                {
                    if (inputBank.skill1.justPressed)
                    {
                        if (result) deductStock = true;
                        outer.SetNextStateToMain();
                        return;
                    }
                    if (characterBody.isSprinting)
                    {
                        outer.SetNextStateToMain();
                        return;
                    }
                }
            }

            public override void OnExit()
            {
                indicator.active = false;

                if (primarySkillSlot) primarySkillSlot.UnsetSkillOverride(this, primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
                Util.PlaySound("Play_captain_shift_end", gameObject);
                if (effectMuzzleInstance) Destroy(effectMuzzleInstance);
                if (modelAnimator) modelAnimator.SetBool("PrepAirstrike", false);
                if (crosshairOverrideRequest != null) crosshairOverrideRequest.Dispose();

                if (deductStock)
                {
                    activatorSkillSlot.DeductStock(1);
                }
                else
                {
                    activatorSkillSlot.DeductStock(1);
                    activatorSkillSlot.rechargeStopwatch = activatorSkillSlot.finalRechargeInterval - 2f;
                }

                base.OnExit();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Pain;
            }
        }

        public class LendMicrobotsPrimaryPressState : EntityStates.BaseState
        {
            public static float baseDuration = 0.1f;

            public float duration;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;

                var allySearch = LendMicrobotsState.CreateAllySearch(characterBody);
                var result = LendMicrobotsState.GetTarget(gameObject, allySearch, GetAimRay());
                if (result)
                {
                    Util.PlaySound("Play_loader_R_toss", gameObject);

                    if (NetworkServer.active)
                    {
                        var body = result.healthComponent.body;
                        if (body.master)
                        {
                            RoR2.Orbs.ItemTransferOrb.DispatchItemTransferOrb(
                                characterBody.corePosition,
                                body.master.inventory,
                                SkillsmasContent.Items.Skillsmas_TemporaryMicrobots.itemIndex,
                                1
                            );
                        }
                    }
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }

        public class LendMicrobotsSecondaryPressState : EntityStates.BaseState
        {
            public static float baseDuration = 0.1f;

            public float duration;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = baseDuration / attackSpeedStat;
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Skill;
            }
        }
    }
}