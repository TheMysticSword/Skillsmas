using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Skillsmas
{
    public static class SkillsmasUtils
    {
        public static CameraRigController FindCameraRigController(GameObject viewer)
        {
            foreach (var cameraRigController in CameraRigController.readOnlyInstancesList)
            {
                if (cameraRigController.target == viewer &&
                    cameraRigController._localUserViewer.cachedBodyObject == viewer &&
                    !cameraRigController.hasOverride)
                {
                    return cameraRigController;
                }
            }
            return null;
        }

        public static void UncorrectAimRay(GameObject viewer, ref Ray aimRay, float finalPointDistance = 1000f)
        {
            var cameraRigController = FindCameraRigController(viewer);
            if (cameraRigController)
            {
                var finalPoint = cameraRigController.currentCameraState.position;
                finalPointDistance += (finalPoint - aimRay.origin).magnitude;
                finalPoint += finalPointDistance * (cameraRigController.currentCameraState.rotation * Vector3.forward);
                aimRay.direction = (finalPoint - aimRay.origin).normalized;
            }
        }
    }

    public class SkillsmasProjectileProximityDetonator : MonoBehaviour
    {
        public TeamFilter myTeamFilter;
        public ProjectileExplosion projectileExplosion;
        public GameObject objectToDestroy;

        public void OnTriggerEnter(Collider collider)
        {
            if (NetworkServer.active)
            {
                if (collider)
                {
                    var collidedHurtBox = collider.GetComponent<HurtBox>();
                    if (collidedHurtBox)
                    {
                        var healthComponent = collidedHurtBox.healthComponent;
                        if (healthComponent)
                        {
                            var teamComponent = healthComponent.GetComponent<TeamComponent>();
                            if (teamComponent && teamComponent.teamIndex == myTeamFilter.teamIndex) return;

                            if (projectileExplosion) projectileExplosion.SetAlive(false);
                            if (objectToDestroy) Destroy(objectToDestroy);
                        }
                    }
                }
            }
        }
    }

	public class SkillsmasProjectileEnhanceZone : MonoBehaviour
	{
		public TeamFilter teamFilter;
		public List<Rigidbody> ignoredProjectiles;

        public List<DamageType> enhancementDamageTypes = new List<DamageType>();
        
        public void Start()
		{
			ignoredProjectiles = new List<Rigidbody>();
		}

		public void OnTriggerEnter(Collider other)
		{
			var teamFilter = other.GetComponent<TeamFilter>();
			var rigidbody = other.GetComponent<Rigidbody>();
			if (rigidbody && teamFilter.teamIndex == this.teamFilter.teamIndex && !ignoredProjectiles.Contains(rigidbody))
			{
                ignoredProjectiles.Add(rigidbody);
                if (enhancementDamageTypes.Count > 0)
                {
                    var projectileDamage = other.GetComponent<ProjectileDamage>();
                    var projectileOverlapAttack = other.GetComponent<ProjectileOverlapAttack>();
                    var projectileDotZone = other.GetComponent<ProjectileDotZone>();
                    foreach (var enhancementDamageType in enhancementDamageTypes)
                    {
                        if (projectileDamage)
                            projectileDamage.damageType |= enhancementDamageType;
                        if (projectileOverlapAttack)
                            projectileOverlapAttack.attack.damageType |= enhancementDamageType;
                        if (projectileDotZone)
                            projectileDotZone.attack.damageType |= enhancementDamageType;
                    }
                }
            }
		}
	}

    public class SkillsmasProjectileHealOwnerOnDamageInflicted : MonoBehaviour, IOnDamageInflictedServerReceiver
    {
        public ProjectileController projectileController;
        public float flatHealing;
        public float fractionalHealing;
        public float healingFromDamageDealt;
        public int maxInstancesOfHealing = -1;
        public int instancesOfHealing = 0;

        public void Awake()
        {
            projectileController = GetComponent<ProjectileController>();
        }

        public void OnDamageInflictedServer(DamageReport damageReport)
        {
            if (maxInstancesOfHealing != -1 && instancesOfHealing >= maxInstancesOfHealing)
                return;

            if (projectileController.owner)
            {
                var ownerHealthComponent = projectileController.owner.GetComponent<HealthComponent>();
                if (ownerHealthComponent)
                {
                    var healOrb = new HealOrb
                    {
                        origin = transform.position,
                        target = ownerHealthComponent.body.mainHurtBox,
                        healValue = flatHealing + ownerHealthComponent.fullHealth * fractionalHealing + healingFromDamageDealt * damageReport.damageDealt,
                        overrideDuration = 0.3f
                    };
                    OrbManager.instance.AddOrb(healOrb);

                    instancesOfHealing++;
                }
            }
        }
    }

    public class SkillsmasProjectileSoundAdder : MonoBehaviour
    {
        public ProjectileOverlapAttack projectileOverlapAttack;
        public NetworkSoundEventDef impactSoundEventDef;

        public void Start()
        {
            if (projectileOverlapAttack) projectileOverlapAttack.attack.impactSound = impactSoundEventDef.index;
        }
    }

    public class SkillsmasAlignToRigidbodyVelocity : MonoBehaviour
    {
        public Rigidbody rigidbody;
        public float minimumVelocitySqrMagnitude = 1f;
        public ProjectileStickOnImpact projectileStickOnImpact;

        public void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            projectileStickOnImpact = GetComponent<ProjectileStickOnImpact>();
        }

        public void LateUpdate()
        {
            if (rigidbody && rigidbody.velocity.sqrMagnitude >= minimumVelocitySqrMagnitude &&
                (!projectileStickOnImpact || !projectileStickOnImpact.stuck))
                transform.forward = rigidbody.velocity.normalized;
        }
    }

    public class PrepCustomWall : EntityStates.BaseState
    {
        public float baseDuration = 0.5f;
        public GameObject areaIndicatorPrefab;
        public GameObject muzzleflashEffect;
        public GameObject goodCrosshairPrefab;
        public GameObject badCrosshairPrefab;
        public string prepWallSoundString = "Play_mage_shift_start";
        public string fireSoundString = "Play_mage_shift_stop";
        public float maxDistance = 600f;
        public float maxSlopeAngle = 70f;

        public float duration;
        public bool goodPlacement;
        public GameObject areaIndicatorInstance;
        public RoR2.UI.CrosshairUtils.OverrideRequest crosshairOverrideRequest;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;

            characterBody.SetAimTimer(duration + 2f);
            PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", duration);
            Util.PlaySound(prepWallSoundString, gameObject);
            areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
            UpdateAreaIndicator();
        }

        private void UpdateAreaIndicator()
        {
            var wasGoodPlacement = goodPlacement;
            goodPlacement = false;
            areaIndicatorInstance.SetActive(true);
            if (areaIndicatorInstance)
            {
                var aimRay = GetAimRay();
                if (Physics.Raycast(CameraRigController.ModifyAimRayIfApplicable(aimRay, gameObject, out var extraRayDistance), out var raycastHit, maxDistance + extraRayDistance, LayerIndex.world.mask))
                {
                    areaIndicatorInstance.transform.position = raycastHit.point;
                    areaIndicatorInstance.transform.up = raycastHit.normal;
                    areaIndicatorInstance.transform.forward = -aimRay.direction;
                    goodPlacement = Vector3.Angle(Vector3.up, raycastHit.normal) < maxSlopeAngle;
                }
                if (wasGoodPlacement != goodPlacement || crosshairOverrideRequest == null)
                {
                    if (crosshairOverrideRequest != null) crosshairOverrideRequest.Dispose();
                    var newCrosshairPrefab = goodPlacement ? goodCrosshairPrefab : badCrosshairPrefab;
                    crosshairOverrideRequest = RoR2.UI.CrosshairUtils.RequestOverrideForBody(characterBody, newCrosshairPrefab, RoR2.UI.CrosshairUtils.OverridePriority.Skill);
                }
            }
            areaIndicatorInstance.SetActive(goodPlacement);
        }

        public override void Update()
        {
            base.Update();
            UpdateAreaIndicator();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && fixedAge >= duration && !inputBank.skill3.down)
            {
                outer.SetNextStateToMain();
            }
        }

        public virtual void CreateWall(Vector3 position, Quaternion rotation)
        {

        }

        public override void OnExit()
        {
            if (!outer.destroying)
            {
                if (goodPlacement)
                {
                    PlayAnimation("Gesture, Additive", "FireWall");
                    Util.PlaySound(fireSoundString, gameObject);
                    if (areaIndicatorInstance)
                    {
                        if (isAuthority)
                        {
                            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleLeft", true);
                            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, gameObject, "MuzzleRight", true);
                        }
                        var forward = areaIndicatorInstance.transform.forward;
                        forward.y = 0f;
                        forward.Normalize();
                        var vector = Vector3.Cross(Vector3.up, forward);
                        CreateWall(areaIndicatorInstance.transform.position, Util.QuaternionSafeLookRotation(vector));

                        if (SkillsmasPlugin.artificerExtendedEnabled)
                            SoftDependencies.ArtificerExtendedSupport.TriggerAltPassiveSkillCast(outer.gameObject);
                    }
                }
                else
                {
                    skillLocator.utility.AddOneStock();
                    PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.2f);
                }
            }

            Destroy(areaIndicatorInstance.gameObject);
            if (crosshairOverrideRequest != null) crosshairOverrideRequest.Dispose();
            base.OnExit();
        }

        public override EntityStates.InterruptPriority GetMinimumInterruptPriority()
        {
            return EntityStates.InterruptPriority.Pain;
        }
    }
}