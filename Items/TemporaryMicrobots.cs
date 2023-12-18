using RoR2;
using R2API.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MysticsRisky2Utils;
using MysticsRisky2Utils.BaseAssetTypes;
using R2API;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using RoR2.Items;

namespace Skillsmas.Items
{
    public class TemporaryMicrobots : BaseItem
    {
        public override void OnLoad()
        {
            base.OnLoad();
            itemDef.name = "Skillsmas_TemporaryMicrobots";
            SetItemTierWhenAvailable(ItemTier.NoTier);
            itemDef.tags = new ItemTag[]
            {
                ItemTag.Utility,
                ItemTag.WorldUnique,
                ItemTag.AIBlacklist,
                ItemTag.BrotherBlacklist
            };
            itemDef.hidden = true;
            itemDef.canRemove = false;

            itemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CaptainDefenseMatrix/texCaptainDefenseMatrixIcon.png").WaitForCompletion();
            
            On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.GetItemStack += DefenseMatrixOn_GetItemStack;
        }

        private int DefenseMatrixOn_GetItemStack(On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.orig_GetItemStack orig, EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn self)
        {
            var result = orig(self);
            if (self.attachedBody && self.attachedBody.inventory)
            {
                result += self.attachedBody.inventory.GetItemCount(itemDef);
            }
            return result;
        }

        public class TemporaryMicrobotsBodyBehavior : BaseItemBodyBehavior
        {
            [ItemDefAssociation(useOnServer = true, useOnClient = false)]
            public static ItemDef GetItemDef()
            {
                return SkillsmasContent.Items.Skillsmas_TemporaryMicrobots;
            }

            public GameObject attachmentGameObject;
            public NetworkedBodyAttachment attachment;
            public float fixedAge = 0;

            public void FixedUpdate()
            {
                attachmentActive = body.healthComponent.alive;
                
                fixedAge += Time.fixedDeltaTime;
                if (fixedAge >= Skills.Captain.LendMicrobots.duration)
                {
                    fixedAge -= Skills.Captain.LendMicrobots.duration;
                    body.inventory.RemoveItem(GetItemDef());
                }
            }

            public void OnDisable()
            {
                attachmentActive = false;
            }

            private bool attachmentActive
            {
                get
                {
                    return attachment != null;
                }
                set
                {
                    if (value != attachmentActive)
                    {
                        if (value)
                        {
                            attachmentGameObject = Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/CaptainDefenseMatrixItemBodyAttachment"));
                            attachment = attachmentGameObject.GetComponent<NetworkedBodyAttachment>();
                            attachment.AttachToGameObjectAndSpawn(body.gameObject, null);
                        }
                        else
                        {
                            Destroy(attachmentGameObject);
                            attachmentGameObject = null;
                            attachment = null;
                        }
                    }
                }
            }
        }
    }
}
