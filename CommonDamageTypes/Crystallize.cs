using MysticsRisky2Utils;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Skillsmas.DamageTypes
{
    public class Crystallize : BaseGenericLoadable
    {
		public static DamageAPI.ModdedDamageType crystallizeDamageType;

		public static GameObject barrierCrystalPickupPrefab;
		public static GameObject barrierCrystalProjectilePrefab;
		
		public static ConfigOptions.ConfigurableValue<float> flatBarrier;
		public static ConfigOptions.ConfigurableValue<float> fractionalBarrier;

		public override void OnPluginAwake()
        {
			crystallizeDamageType = DamageAPI.ReserveDamageType();

			barrierCrystalPickupPrefab = Utils.CreateBlankPrefab("Skillsmas_BarrierCrystalPickup", true);
			barrierCrystalPickupPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

			barrierCrystalProjectilePrefab = Utils.CreateBlankPrefab("Skillsmas_BarrierCrystalProjectile", true);
			barrierCrystalProjectilePrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
		}

		public override void OnLoad()
		{
			var pickupEffect = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/Crystallize/BarrierCrystalPickupEffect.prefab");
			pickupEffect.AddComponent<DestroyOnTimer>().duration = 1f;
			var effectComponent = pickupEffect.AddComponent<EffectComponent>();
			effectComponent.soundName = "Play_item_proc_bandolierPickup";
			var vfxAttributes = pickupEffect.AddComponent<VFXAttributes>();
			vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
			vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Low;
			SkillsmasContent.Resources.effectPrefabs.Add(pickupEffect);

			Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/Crystallize/BarrierCrystalPickup.prefab"), barrierCrystalPickupPrefab);
			var teamFilter = barrierCrystalPickupPrefab.AddComponent<TeamFilter>();
			var gravitatePickup = barrierCrystalPickupPrefab.transform.Find("GravitatePickup").gameObject.AddComponent<GravitatePickup>();
			gravitatePickup.rigidbody = barrierCrystalPickupPrefab.GetComponent<Rigidbody>();
			gravitatePickup.teamFilter = teamFilter;
			gravitatePickup.acceleration = 5f;
			gravitatePickup.maxSpeed = 40f;
			var pickup = barrierCrystalPickupPrefab.transform.Find("PickupTrigger").gameObject.AddComponent<SkillsmasBarrierPickup>();
			pickup.baseObject = barrierCrystalPickupPrefab;
			pickup.teamFilter = teamFilter;
			pickup.pickupEffect = pickupEffect;
			flatBarrier = ConfigOptions.ConfigurableValue.CreateFloat(
				SkillsmasPlugin.PluginGUID,
				SkillsmasPlugin.PluginName,
				SkillsmasPlugin.config,
				"Keyword: Crystallize",
				"Flat Barrier",
				0f,
				stringsToAffect: new List<string>
				{
					"KEYWORD_SKILLSMAS_CRYSTALLIZE"
				},
				useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
				onChanged: (newValue) => pickup.flatBarrier = newValue
			);
			fractionalBarrier = ConfigOptions.ConfigurableValue.CreateFloat(
				SkillsmasPlugin.PluginGUID,
				SkillsmasPlugin.PluginName,
				SkillsmasPlugin.config,
				"Keyword: Crystallize",
				"Fractional Barrier",
				5f,
				stringsToAffect: new List<string>
				{
					"KEYWORD_SKILLSMAS_CRYSTALLIZE"
				},
				useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
				onChanged: (newValue) => pickup.fractionalBarrier = newValue / 100f
			);

			barrierCrystalPickupPrefab.AddComponent<DestroyOnTimer>().duration = 10f;
			var blink = barrierCrystalPickupPrefab.AddComponent<BeginRapidlyActivatingAndDeactivating>();
			blink.blinkFrequency = 20f;
			blink.delayBeforeBeginningBlinking = 9f;
			blink.blinkingRootObject = barrierCrystalPickupPrefab.transform.Find("Scaler").gameObject;

			var ghost = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/Crystallize/BarrierCrystalProjectileGhost.prefab");
			ghost.AddComponent<ProjectileGhostController>();
			ghost.AddComponent<DetachTrailOnDestroy>().targetTrailRenderers = new[] {
				ghost.transform.Find("Trail").GetComponent<TrailRenderer>()
			};

			Utils.CopyChildren(SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Artificer/Rock/Crystallize/BarrierCrystalProjectile.prefab"), barrierCrystalProjectilePrefab);
			var projectileController = barrierCrystalProjectilePrefab.AddComponent<ProjectileController>();
			projectileController.allowPrediction = true;
			projectileController.ghostPrefab = ghost;
			projectileController.procCoefficient = 0;
			barrierCrystalProjectilePrefab.AddComponent<ProjectileNetworkTransform>();
			var projectileDamage = barrierCrystalProjectilePrefab.AddComponent<ProjectileDamage>();
			var projectileSimple = barrierCrystalProjectilePrefab.AddComponent<ProjectileSimple>();
			projectileSimple.desiredForwardSpeed = 10f;
			projectileSimple.updateAfterFiring = false;
			var projectileImpactExplosion = barrierCrystalProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
			projectileImpactExplosion.impactEffect = null;
			projectileImpactExplosion.destroyOnEnemy = false;
			projectileImpactExplosion.destroyOnWorld = true;
			projectileImpactExplosion.lifetime = 10f;
			projectileImpactExplosion.fireChildren = true;
			projectileImpactExplosion.childrenCount = 1;
			projectileImpactExplosion.childrenProjectilePrefab = barrierCrystalPickupPrefab;

			GenericGameEvents.OnHitEnemy += GenericGameEvents_OnHitEnemy;
		}

		private static void GenericGameEvents_OnHitEnemy(DamageInfo damageInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo attackerInfo, MysticsRisky2UtilsPlugin.GenericCharacterInfo victimInfo)
		{
			if (damageInfo.procCoefficient > 0 && damageInfo.HasModdedDamageType(crystallizeDamageType) && attackerInfo.master && Util.CheckRoll(100f * damageInfo.procCoefficient, attackerInfo.master))
			{
				var direction = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * Quaternion.AngleAxis(-70f, Vector3.right) * Vector3.forward;
				ProjectileManager.instance.FireProjectile(new FireProjectileInfo
				{
					projectilePrefab = barrierCrystalProjectilePrefab,
					position = damageInfo.position,
					rotation = Util.QuaternionSafeLookRotation(direction),
					owner = attackerInfo.gameObject,
					damage = 0f,
					crit = false,
					force = 0f,
					damageColorIndex = DamageColorIndex.Item
				});
			}
		}

		public class SkillsmasBarrierPickup : MonoBehaviour
		{
			public GameObject baseObject;
			public TeamFilter teamFilter;
			public GameObject pickupEffect;
			public float flatBarrier;
			public float fractionalBarrier;
			
			public bool alive = true;

			public void OnTriggerStay(Collider other)
			{
				if (NetworkServer.active && alive && TeamComponent.GetObjectTeam(other.gameObject) == teamFilter.teamIndex)
				{
					var body = other.GetComponent<CharacterBody>();
					if (body)
					{
						var healthComponent = body.healthComponent;
						if (healthComponent)
						{
							body.healthComponent.AddBarrier(flatBarrier + healthComponent.fullHealth * fractionalBarrier);
							if (pickupEffect)
							{
								EffectManager.SpawnEffect(pickupEffect, new EffectData
								{
									origin = transform.position
								}, true);
							}
						}
						Destroy(baseObject);
					}
				}
			}
		}
	}
}