using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

namespace Skillsmas.Buffs
{
    public class ChainKillActive : BaseBuff
    {
        public static GameObject temporaryEffectPrefab;

        public override void OnLoad() {
            buffDef.name = "Skillsmas_ChainKillActive";
            buffDef.canStack = false;
            buffDef.isHidden = true;
            refreshable = true;

            temporaryEffectPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Bandit/ChainKillBuff/ChainKillBuffEffect.prefab");
            
            var ppObject = temporaryEffectPrefab.transform.Find("PostProcessing").gameObject;
            temporaryEffectPrefab.AddComponent<LocalCameraEffect>().effectRoot = ppObject;

            var ppFadeIn = temporaryEffectPrefab.AddComponent<PostProcessDuration>();
            ppFadeIn.enabled = false;
            ppFadeIn.ppVolume = ppObject.GetComponent<PostProcessVolume>();
            ppFadeIn.ppWeightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            ppFadeIn.maxDuration = 0.1f;
            ppFadeIn.destroyOnEnd = false;

            var ppFadeOut = ppObject.AddComponent<PostProcessDuration>();
            ppFadeOut.enabled = false;
            ppFadeOut.ppVolume = ppObject.GetComponent<PostProcessVolume>();
            ppFadeOut.ppWeightCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            ppFadeOut.maxDuration = 0.1f;
            ppFadeOut.destroyOnEnd = false;

            var tempVFX = temporaryEffectPrefab.AddComponent<CustomTempVFXManagement.MysticsRisky2UtilsTempVFX>();
            tempVFX.enterBehaviours = new[]
            {
                ppFadeIn
            };
            tempVFX.exitBehaviours = new[]
            {
                ppFadeOut
            };

            CustomTempVFXManagement.allVFX.Add(new CustomTempVFXManagement.VFXInfo
            {
                prefab = temporaryEffectPrefab,
                condition = (x) => x.HasBuff(buffDef),
                radius = CustomTempVFXManagement.DefaultRadiusCall
            });
        }
    }
}
