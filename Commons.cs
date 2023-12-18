using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Skillsmas
{
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