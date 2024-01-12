using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;

namespace Skillsmas.Buffs
{
    public class Harden : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_Harden";
            buffDef.buffColor = new Color32(250, 183, 46, 255);
            buffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();
            buffDef.canStack = false;

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(buffDef))
            {
                args.armorAdd += DamageTypes.Crystallize.energeticResonanceArmor;
            }
        }
    }
}
