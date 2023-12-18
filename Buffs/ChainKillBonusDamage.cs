using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace Skillsmas.Buffs
{
    public class ChainKillBonusDamage : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_ChainKillBonusDamage";
            buffDef.buffColor = new Color32(255, 255, 0, 255);
            buffDef.iconSprite = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/Skills/Bandit/ChainKillBuff/texEnemyMark.png");
            buffDef.canStack = true;

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            var buffCount = sender.GetBuffCount(buffDef);
            if (buffCount > 0)
            {
                args.damageMultAdd += buffCount * Skills.Bandit.ChainKillBuff.bonusPerMarkKill / 100f;
            }
        }
    }
}
