using MysticsRisky2Utils;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Skillsmas.DamageTypes
{
    public class Revitalizing : BaseGenericLoadable
    {
		public static DamageAPI.ModdedDamageType revitalizingDamageType;

		public static GameObject revitalizingEffectPrefab;

		public static ConfigOptions.ConfigurableValue<float> flatHealing = ConfigOptions.ConfigurableValue.CreateFloat(
			SkillsmasPlugin.PluginGUID,
			SkillsmasPlugin.PluginName,
			SkillsmasPlugin.config,
			"Keyword: Revitalizing",
			"Flat Healing",
			1f,
			stringsToAffect: new List<string>
			{
				"KEYWORD_SKILLSMAS_REVITALIZING"
			},
			useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
		);
		public static ConfigOptions.ConfigurableValue<float> fractionalHealing = ConfigOptions.ConfigurableValue.CreateFloat(
			SkillsmasPlugin.PluginGUID,
			SkillsmasPlugin.PluginName,
			SkillsmasPlugin.config,
			"Keyword: Revitalizing",
			"Fractional Healing",
			1f,
			stringsToAffect: new List<string>
			{
				"KEYWORD_SKILLSMAS_REVITALIZING"
			},
			useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
		);

		public static ConfigOptions.ConfigurableValue<float> energeticResonanceChance = ConfigOptions.ConfigurableValue.CreateFloat(
			SkillsmasPlugin.PluginGUID,
			SkillsmasPlugin.PluginName,
			SkillsmasPlugin.config,
			"Keyword: Revitalizing",
			"Energetic Resonance Chance",
			5f,
			description: "Effect granted by the Energetic Resonance passive from the ArtificerExtended mod",
			stringsToAffect: new List<string>
			{
				"KEYWORD_SKILLSMAS_ARTIFICEREXTENDED_ALTPASSIVE_WATER"
			},
			useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry
		);

		public override void OnPluginAwake()
        {
			revitalizingDamageType = DamageAPI.ReserveDamageType();
		}

		public override void OnLoad()
		{
			revitalizingEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Water/Revitalizing/RevitalizingEffect.prefab");
			revitalizingEffectPrefab.AddComponent<DestroyOnTimer>().duration = 1f;
			var effectComponent = revitalizingEffectPrefab.AddComponent<EffectComponent>();
			effectComponent.applyScale = true;
			effectComponent.parentToReferencedTransform = true;
			var vfxAttributes = revitalizingEffectPrefab.AddComponent<VFXAttributes>();
			vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
			vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Low;
			SkillsmasContent.Resources.effectPrefabs.Add(revitalizingEffectPrefab);

			GenericGameEvents.OnHitEnemy += GenericGameEvents_OnHitEnemy;
		}

		private static void GenericGameEvents_OnHitEnemy(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo)
		{
			if (damageInfo.procCoefficient > 0 && damageInfo.HasModdedDamageType(revitalizingDamageType) && attackerInfo.body && attackerInfo.healthComponent)
			{
				var healAmount = (fractionalHealing + attackerInfo.healthComponent.fullHealth * flatHealing / 100f) * damageInfo.procCoefficient;
				var spawnEffect = Util.CheckRoll(100f * damageInfo.procCoefficient);
				var cleanse = false;
				if (SkillsmasPlugin.artificerExtendedEnabled)
				{
					if (SoftDependencies.ArtificerExtendedSupport.BodyHasAltPassive(attackerInfo.gameObject))
					{
						var waterPower = SoftDependencies.ArtificerExtendedSupport.GetWaterPower(attackerInfo.gameObject);
						cleanse = Util.CheckRoll(energeticResonanceChance * waterPower, attackerInfo.master);
					}
				}
				foreach (var teamMember in TeamComponent.GetTeamMembers(attackerInfo.teamIndex))
                {
					if (teamMember.body && teamMember.body.healthComponent && teamMember.body.healthComponent.alive)
                    {
						teamMember.body.healthComponent.Heal(healAmount, default);

						if (spawnEffect)
						{
							var effectData = new EffectData
							{
								origin = teamMember.body.corePosition,
								scale = teamMember.body.radius
							};
							effectData.SetHurtBoxReference(teamMember.gameObject);
							EffectManager.SpawnEffect(revitalizingEffectPrefab, effectData, true);
						}

						if (cleanse)
						{
							var debuffCount = 0;
							var debuffsOnThisBody = new List<BuffIndex>();
							var debuffDotIndices = new List<DotController.DotIndex>();
							var nullDotIndex = (DotController.DotIndex)(-1);
							foreach (var buffIndex in BuffCatalog.debuffBuffIndices)
                            {
								if (teamMember.body.HasBuff(buffIndex) && teamMember.body.timedBuffs.Any(x => x.buffIndex == buffIndex))
								{
									debuffCount++;
									debuffsOnThisBody.Add(buffIndex);
									debuffDotIndices.Add(nullDotIndex);
								}
                            }
							var dotController = DotController.FindDotController(teamMember.gameObject);
							if (dotController)
                            {
								for (var dotIndex = (DotController.DotIndex)0; dotIndex < (DotController.DotIndex)(DotAPI.VanillaDotCount + DotAPI.CustomDotCount); dotIndex++)
								{
									if (dotController.HasDotActive(dotIndex))
									{
										var dotDef = DotController.GetDotDef(dotIndex);
										if (dotDef.associatedBuff != null)
										{
											debuffCount++;
											debuffsOnThisBody.Add(dotDef.associatedBuff.buffIndex);
											debuffDotIndices.Add(dotIndex);
										}
									}
								}
                            }
							if (debuffCount > 0)
                            {
								var randomDebuff = RoR2Application.rng.RangeInt(0, debuffCount);
								var randomDebuffIndex = debuffsOnThisBody[randomDebuff];
								var randomDebuffDotIndex = debuffDotIndices[randomDebuff];
								if (randomDebuffDotIndex != nullDotIndex)
								{
									if (dotController)
									{
										for (var dotStackIndex = 0; dotStackIndex < dotController.dotStackList.Count; dotStackIndex++)
										{
											var dotStack = dotController.dotStackList[dotStackIndex];
											if (dotStack.dotIndex == randomDebuffDotIndex)
											{
												dotController.RemoveDotStackAtServer(dotStackIndex);
												break;
											}
										}
									}
								}
								else
								{
									for (var timedBuffIndex = 0; timedBuffIndex < teamMember.body.timedBuffs.Count; timedBuffIndex++)
									{
										var timedBuff = teamMember.body.timedBuffs[timedBuffIndex];
										if (timedBuff.buffIndex == randomDebuffIndex)
										{
											teamMember.body.timedBuffs.RemoveAt(timedBuffIndex);
											teamMember.body.RemoveBuff(randomDebuffIndex);
											break;
										}
									}
								}
                            }
						}
                    }
                }
			}
		}
	}
}