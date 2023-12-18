using MysticsRisky2Utils.BaseAssetTypes;
using RoR2;
using UnityEngine;
using MysticsRisky2Utils;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

namespace Skillsmas.Buffs
{
    public class EnemyMark : BaseBuff
    {
        public override void OnLoad() {
            buffDef.name = "Skillsmas_EnemyMark";
            buffDef.canStack = false;
            buffDef.isHidden = true;
            buffDef.isDebuff = true;

            SkillsmasEnemyMarkHelper.markPrefab = SkillsmasPlugin.AssetBundle.LoadAsset<GameObject>("Assets/Mods/Skillsmas/Skills/Bandit/ChainKillBuff/MarkedEnemyOverlay.prefab");

            SceneCamera.onSceneCameraPreRender += SceneCamera_onSceneCameraPreRender;
        }

        private void SceneCamera_onSceneCameraPreRender(SceneCamera sceneCamera)
        {
            if (sceneCamera.cameraRigController)
            {
                foreach (var model in InstanceTracker.GetInstancesList<CharacterModel>())
                {
                    SkillsmasEnemyMarkHelper.Get(model).UpdateForCamera(sceneCamera.cameraRigController);
                }
            }
        }

        public class SkillsmasEnemyMarkHelper : MonoBehaviour
        {
            public static GameObject markPrefab;
            public static Dictionary<CharacterModel, SkillsmasEnemyMarkHelper> cache = new Dictionary<CharacterModel, SkillsmasEnemyMarkHelper>();

            public CharacterModel characterModel;
            public GameObject markInstance;

            public static SkillsmasEnemyMarkHelper Get(CharacterModel self)
            {
                SkillsmasEnemyMarkHelper helper = null;
                if (!cache.TryGetValue(self, out helper))
                {
                    helper = self.gameObject.AddComponent<SkillsmasEnemyMarkHelper>();
                    cache.Add(self, helper);
                }
                return helper;
            }

            public void Awake()
            {
                characterModel = GetComponent<CharacterModel>();
            }

            public void OnDestroy()
            {
                if (markInstance) Destroy(markInstance);
                if (characterModel != null && cache.ContainsKey(characterModel)) cache.Remove(characterModel);
            }

            public void UpdateForCamera(CameraRigController cameraRigController)
            {
                if (characterModel.body && characterModel.body.GetVisibilityLevel(cameraRigController.targetTeamIndex) != VisibilityLevel.Invisible && characterModel.body.healthComponent && characterModel.body.healthComponent.alive && cameraRigController.targetBody)
                {
                    var iconExists = markInstance != null;
                    var shouldShowIcon = characterModel.body.HasBuff(SkillsmasContent.Buffs.Skillsmas_EnemyMark);
                    if (iconExists != shouldShowIcon)
                    {
                        if (!iconExists)
                        {
                            markInstance = Instantiate(markPrefab);
                        }
                        else
                        {
                            Destroy(markInstance);
                        }
                    }
                    else if (iconExists)
                    {
                        markInstance.transform.position = characterModel.body.corePosition;
                    }

                    return;
                }

                if (markInstance) Destroy(markInstance);
            }
        }
    }
}
