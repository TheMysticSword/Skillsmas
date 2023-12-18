using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace Skillsmas.Buffs
{
    public class ToolbotUpdated : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_ToolbotUpdated";
            buffDef.buffColor = new Color32(134, 255, 79, 255);
            buffDef.iconSprite = SkillsmasPlugin.AssetBundle.LoadAsset<Sprite>("Assets/Mods/Skillsmas/Skills/MUL-T/RobotUpdate/texRobotUpdateBuff.png");
            buffDef.canStack = false;

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(buffDef))
            {
                args.attackSpeedMultAdd += Skills.Toolbot.RobotUpdate.attackSpeed / 100f;
            }
        }
    }
}
