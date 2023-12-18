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
using JetBrains.Annotations;

namespace Skillsmas.Skills.Captain
{
    public class ReuseableBeacon : BaseSkill
    {
        public override System.Type GetSkillDefType()
        {
            return typeof(SkillsmasReuseableBeaconSkillDef);
        }

        public class SkillsmasReuseableBeaconSkillDef : CaptainOrbitalSkillDef
        {
            public override BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                return new InstanceData();
            }

            public class InstanceData : BaseSkillInstanceData
            {
                public int beaconsDropped = 0;
            }
        }

        public override void OnLoad()
        {
            skillDef.skillName = "Skillsmas_ReuseableBeacon";
            skillDef.skillNameToken = "CAPTAIN_SKILLSMAS_REUSEABLEBEACON_NAME";
            skillDef.skillDescriptionToken = "CAPTAIN_SKILLSMAS_REUSEABLEBEACON_DESCRIPTION";
            skillDef.icon = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/SkillIcons/ReuseableBeacon.jpg");
            skillDef.activationStateMachineName = "Skillswap";
            skillDef.activationState = new EntityStates.SerializableEntityStateType(typeof(SetupSupplyDropReuseable));
            skillDef.interruptPriority = EntityStates.InterruptPriority.Skill;
            SetUpValuesAndOptions(
                "Captain: Reuseable Supply Beacon",
                baseRechargeInterval: 120f,
                baseMaxStock: 1,
                rechargeStock: 1,
                requiredStock: 1,
                stockToConsume: 0,
                cancelSprintingOnActivation: true,
                forceSprintDuringState: false,
                canceledFromSprinting: true,
                isCombatSkill: true,
                mustKeyPress: true
            );
            skillDef.resetCooldownTimerOnUse = false;
            skillDef.fullRestockOnAssign = true;
            skillDef.dontAllowPastMaxStocks = false;
            skillDef.beginSkillCooldownOnSkillEnd = true;

            var originalSkillDef = Addressables.LoadAssetAsync<CaptainSupplyDropSkillDef>("RoR2/Base/Captain/PrepSupplyDrop.asset").WaitForCompletion();
            var customSkillDef = skillDef as SkillsmasReuseableBeaconSkillDef;
            customSkillDef.disabledIcon = originalSkillDef.disabledIcon;
            customSkillDef.disabledNameToken = originalSkillDef.disabledNameToken;
            customSkillDef.disabledDescriptionToken = originalSkillDef.disabledDescriptionToken;
            
            var skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Captain/CaptainSpecialSkillFamily.asset").WaitForCompletion();
            HG.ArrayUtils.ArrayAppend(ref skillFamily.variants, in skillFamilyVariant);

            On.EntityStates.Captain.Weapon.CallSupplyDropBase.OnEnter += CallSupplyDropBase_OnEnter;
        }

        private void CallSupplyDropBase_OnEnter(On.EntityStates.Captain.Weapon.CallSupplyDropBase.orig_OnEnter orig, EntityStates.Captain.Weapon.CallSupplyDropBase self)
        {
            orig(self);
            if (self.isAuthority)
            {
                if (self.skillLocator.special && self.skillLocator.special.skillDef.skillName == skillDef.skillName)
                {
                    if (self.placementInfo.ok)
                    {
                        self.activatorSkillSlot.stock = self.activatorSkillSlot.maxStock;
                        self.skillLocator.special.DeductStock(1);
                        ((SkillsmasReuseableBeaconSkillDef.InstanceData)self.skillLocator.special.skillInstanceData).beaconsDropped++;
                    }
                }
            }
        }

        public class SetupSupplyDropReuseable : EntityStates.Captain.Weapon.SetupSupplyDrop
        {
            public override void OnEnter()
            {
                this.LoadConfiguration(typeof(EntityStates.Captain.Weapon.SetupSupplyDrop));
                base.OnEnter();
                var beaconsDropped = ((SkillsmasReuseableBeaconSkillDef.InstanceData)skillLocator.special.skillInstanceData).beaconsDropped;
                skillLocator.primary = skillLocator.FindSkill((beaconsDropped % 2 == 0) ? "SupplyDrop1" : "SupplyDrop2");
                skillLocator.secondary = originalSecondarySkill;
                if (crosshairOverrideRequest != null) crosshairOverrideRequest.Dispose();
            }
        }
    }
}