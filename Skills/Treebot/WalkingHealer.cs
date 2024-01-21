using RoR2;
using UnityEngine;
using RoR2.Skills;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.Networking;
using System.Collections.Generic;
using RoR2.Audio;

namespace Skillsmas.Skills.Treebot
{
    public class WalkingHealer : BaseSkill
    {
        public static ConfigOptions.ConfigurableValue<float> duration = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "REX: DIRECTIVE: Stimulate",
            "Duration",
            8f,
            stringsToAffect: new List<string>
            {
                "TREEBOT_SKILLSMAS_WALKINGHEALER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> healing = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "REX: DIRECTIVE: Stimulate",
            "Healing",
            20f,
            stringsToAffect: new List<string>
            {
                "TREEBOT_SKILLSMAS_WALKINGHEALER_DESCRIPTION"
            },
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );
        public static ConfigOptions.ConfigurableValue<float> radius = ConfigOptions.ConfigurableValue.CreateFloat(
            SkillsmasPlugin.PluginGUID,
            SkillsmasPlugin.PluginName,
            SkillsmasPlugin.config,
            "REX: DIRECTIVE: Stimulate",
            "Radius",
            13f,
            useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
        );

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_WalkingHealer";
            skillDef.skillNameToken = "TREEBOT_SKILLSMAS_WALKINGHEALER_NAME";
            skillDef.skillDescriptionToken = "TREEBOT_SKILLSMAS_WALKINGHEALER_DESCRIPTION";
            skillDef.keywordTokens = new[]
            {
                "KEYWORD_SKILLSMAS_HARMLESS"
            };
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/DirectiveStimulate.png");
            skillDef.activationStateMachineName = "Weapon";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(WalkingHealerState));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "REX: DIRECTIVE: Stimulate",
                baseRechargeInterval: 12f,
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
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Treebot/TreebotBodySpecialFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            SkillsmasContent.Resources.entityStateTypes.Add(typeof(WalkingHealerState));

            WalkingHealerState.rangeIndicatorPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineHealing/ShrineHealingWard.prefab").WaitForCompletion(), "Skillsmas_WalkingHealerRangeIndicator", false);
            Object.Destroy(WalkingHealerState.rangeIndicatorPrefab.GetComponent<HealingWard>());
            Object.Destroy(WalkingHealerState.rangeIndicatorPrefab.GetComponent<TeamFilter>());
            Object.Destroy(WalkingHealerState.rangeIndicatorPrefab.GetComponent<NetworkIdentity>());

            WalkingHealerState.loopSoundDef = ScriptableObject.CreateInstance<LoopSoundDef>();
            WalkingHealerState.loopSoundDef.startSoundName = "Play_miniMushroom_selfHeal_loop";
            WalkingHealerState.loopSoundDef.stopSoundName = "Stop_miniMushroom_selfHeal_loop";
        }

        public class WalkingHealerState : EntityStates.GenericCharacterMain
        {
            public static GameObject rangeIndicatorPrefab;
            public static LoopSoundDef loopSoundDef;

            public float duration;
            public LoopSoundManager.SoundLoopPtr soundLoopPtr;
            public SphereSearch allySearch;
            public TeamMask teamMask;
            public Transform rangeIndicator;
            public float rangeIndicatorScaleVelocity;
            public float currentRadius;
            public float healInterval = 0.25f;
            public float healTimer;

            public override void OnEnter()
            {
                base.OnEnter();
                duration = WalkingHealer.duration;

                Util.PlaySound("Play_treeBot_R_impact", gameObject);
                soundLoopPtr = LoopSoundManager.PlaySoundLoopLocal(gameObject, loopSoundDef);
                PlayAnimation("Gesture, Additive", "FireFlower", "FireFlower.playbackRate", 1f);

                currentRadius = radius + characterBody.bestFitRadius;
                allySearch = new SphereSearch
                {
                    radius = currentRadius,
                    queryTriggerInteraction = QueryTriggerInteraction.Ignore,
                    mask = LayerIndex.entityPrecise.mask
                };
                teamMask.AddTeam(teamComponent.teamIndex);

                rangeIndicator = Object.Instantiate(rangeIndicatorPrefab, characterBody.corePosition, Quaternion.identity).transform;
            }

            public override void Update()
            {
                base.Update();
                if (rangeIndicator)
                {
                    rangeIndicator.transform.position = transform.position;
                    var newScale = Mathf.SmoothDamp(rangeIndicator.localScale.x, currentRadius, ref rangeIndicatorScaleVelocity, 0.2f);
                    rangeIndicator.localScale = new Vector3(newScale, newScale, newScale);
                }
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();

                if (NetworkServer.active)
                {
                    healTimer -= Time.fixedDeltaTime;
                    if (healTimer <= 0)
                    {
                        healTimer += healInterval;
                        Heal();
                    }
                }

                if (fixedAge >= duration && isAuthority)
                {
                    outer.SetNextStateToMain();
                }
            }

            public void Heal()
            {
                allySearch.origin = characterBody.corePosition;
                allySearch.radius = currentRadius;
                allySearch.RefreshCandidates();
                allySearch.FilterCandidatesByDistinctHurtBoxEntities();
                allySearch.FilterCandidatesByHurtBoxTeam(teamMask);
                foreach (var ally in allySearch.GetHurtBoxes())
                {
                    ally.healthComponent.HealFraction(healing / 100f * healInterval, default);
                }
            }

            public override void OnExit()
            {
                LoopSoundManager.StopSoundLoopLocal(soundLoopPtr);
                Util.PlaySound("Play_treeBot_sprint_end", gameObject);
                PlayAnimation("Gesture, Additive", "FireFlower", "FireFlower.playbackRate", 1f);

                if (rangeIndicator) Destroy(rangeIndicator.gameObject);

                base.OnExit();
            }

            public override bool CanExecuteSkill(GenericSkill skillSlot)
            {
                return false;
            }

            public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
            {
                return EntityStates.InterruptPriority.Pain;
            }
        }
    }
}