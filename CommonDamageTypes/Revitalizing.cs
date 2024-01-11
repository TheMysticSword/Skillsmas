using MysticsRisky2Utils;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
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
                    }
                }
			}
		}
	}
}