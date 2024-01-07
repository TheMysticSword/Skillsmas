using BepInEx;
using MysticsRisky2Utils;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace Skillsmas
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(DamageAPI.PluginGUID)]
    [BepInDependency(DotAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(MysticsRisky2UtilsPlugin.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class SkillsmasPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.themysticsword.skillsmas";
        public const string PluginName = "Skillsmas";
        public const string PluginVersion = "1.1.1";

        public static System.Reflection.Assembly executingAssembly;
        internal static System.Type declaringType;
        internal static PluginInfo pluginInfo;
        internal static BepInEx.Logging.ManualLogSource logger;
        internal static BepInEx.Configuration.ConfigFile config;

        internal static ConfigOptions.ConfigurableValue<bool> ignoreBalanceConfig;

        private static AssetBundle _assetBundle;
        public static AssetBundle AssetBundle
        {
            get
            {
                if (_assetBundle == null)
                    _assetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pluginInfo.Location), "skillsmasassetbundle"));
                return _assetBundle;
            }
        }

        public void Awake()
        {
            pluginInfo = Info;
            logger = Logger;
            config = Config;
            executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            declaringType = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;

            ignoreBalanceConfig = ConfigOptions.ConfigurableValue.CreateBool(
                PluginGUID,
                PluginName,
                config,
                "General",
                "Ignore Balance Changes",
                true,
                "If true, most balance-related number values won't be changed by configs, and will use recommended default values."
            );

            if (MysticsRisky2Utils.SoftDependencies.SoftDependencyManager.RiskOfOptionsDependency.enabled)
            {
                Sprite iconSprite = null;
                var iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "icon.png");
                if (System.IO.File.Exists(iconPath))
                {
                    var iconTexture = new Texture2D(2, 2);
                    iconTexture.LoadImage(System.IO.File.ReadAllBytes(iconPath));
                    iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0, 0), 100);
                }
                MysticsRisky2Utils.SoftDependencies.SoftDependencyManager.RiskOfOptionsDependency.RegisterModInfo(PluginGUID, PluginName, "Adds new skills. Has nothing to do with Christmas. Sorry.", iconSprite);
            }

            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<BaseGenericLoadable>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseItem>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseEquipment>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseBuff>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseInteractable>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseCharacterBody>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<MysticsRisky2Utils.BaseAssetTypes.BaseCharacterMaster>(executingAssembly);
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper.PluginAwakeLoad<Skills.BaseSkill>(executingAssembly);

            ContentManager.collectContentPackProviders += (addContentPackProvider) =>
            {
                addContentPackProvider(new SkillsmasContent());
            };
        }
    }

    public class SkillsmasContent : IContentPackProvider
    {
        public string identifier
        {
            get
            {
                return SkillsmasPlugin.PluginName;
            }
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            contentPack.identifier = identifier;
            MysticsRisky2Utils.ContentManagement.ContentLoadHelper contentLoadHelper = new MysticsRisky2Utils.ContentManagement.ContentLoadHelper();

            // Add content loading dispatchers to the content load helper
            System.Action[] loadDispatchers = new System.Action[]
            {
                () => contentLoadHelper.DispatchLoad<object>(SkillsmasPlugin.executingAssembly, typeof(BaseGenericLoadable), null),
                () => contentLoadHelper.DispatchLoad<ItemDef>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseItem), x => contentPack.itemDefs.Add(x)),
                () => contentLoadHelper.DispatchLoad<EquipmentDef>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseEquipment), x => contentPack.equipmentDefs.Add(x)),
                () => contentLoadHelper.DispatchLoad<BuffDef>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseBuff), x => contentPack.buffDefs.Add(x)),
                () => contentLoadHelper.DispatchLoad<GameObject>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseInteractable), null),
                () => contentLoadHelper.DispatchLoad<GameObject>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseCharacterBody), x => contentPack.bodyPrefabs.Add(x)),
                () => contentLoadHelper.DispatchLoad<GameObject>(SkillsmasPlugin.executingAssembly, typeof(MysticsRisky2Utils.BaseAssetTypes.BaseCharacterMaster), x => contentPack.masterPrefabs.Add(x)),
                () => contentLoadHelper.DispatchLoad<RoR2.Skills.SkillDef>(SkillsmasPlugin.executingAssembly, typeof(Skills.BaseSkill), x => contentPack.skillDefs.Add(x))
            };
            int num = 0;
            for (int i = 0; i < loadDispatchers.Length; i = num)
            {
                loadDispatchers[i]();
                args.ReportProgress(Util.Remap((float)(i + 1), 0f, (float)loadDispatchers.Length, 0f, 0.05f));
                yield return null;
                num = i + 1;
            }

            // Start loading content. Longest part of the loading process, so we will dedicate most of the progress bar to it
            while (contentLoadHelper.coroutine.MoveNext())
            {
                args.ReportProgress(Util.Remap(contentLoadHelper.progress.value, 0f, 1f, 0.05f, 0.9f));
                yield return contentLoadHelper.coroutine.Current;
            }

            // Populate static content pack fields and add various prefabs and scriptable objects generated during the content loading part to the content pack
            loadDispatchers = new System.Action[]
            {
                () => ContentLoadHelper.PopulateTypeFields<ItemDef>(typeof(Items), contentPack.itemDefs),
                () => ContentLoadHelper.PopulateTypeFields<BuffDef>(typeof(Buffs), contentPack.buffDefs),
                () => contentPack.bodyPrefabs.Add(Resources.bodyPrefabs.ToArray()),
                () => contentPack.masterPrefabs.Add(Resources.masterPrefabs.ToArray()),
                () => contentPack.projectilePrefabs.Add(Resources.projectilePrefabs.ToArray()),
                () => contentPack.gameModePrefabs.Add(Resources.gameModePrefabs.ToArray()),
                () => contentPack.networkedObjectPrefabs.Add(Resources.networkedObjectPrefabs.ToArray()),
                () => contentPack.effectDefs.Add(Resources.effectPrefabs.ConvertAll(x => new EffectDef(x)).ToArray()),
                () => contentPack.networkSoundEventDefs.Add(Resources.networkSoundEventDefs.ToArray()),
                () => contentPack.unlockableDefs.Add(Resources.unlockableDefs.ToArray()),
                () => contentPack.entityStateTypes.Add(Resources.entityStateTypes.ToArray()),
                () => contentPack.skillDefs.Add(Resources.skillDefs.ToArray()),
                () => contentPack.skillFamilies.Add(Resources.skillFamilies.ToArray()),
                () => contentPack.sceneDefs.Add(Resources.sceneDefs.ToArray()),
                () => contentPack.gameEndingDefs.Add(Resources.gameEndingDefs.ToArray())
            };
            for (int i = 0; i < loadDispatchers.Length; i = num)
            {
                loadDispatchers[i]();
                args.ReportProgress(Util.Remap((float)(i + 1), 0f, (float)loadDispatchers.Length, 0.9f, 0.95f));
                yield return null;
                num = i + 1;
            }

            // Call "AfterContentPackLoaded" methods
            loadDispatchers = new System.Action[]
            {
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<BaseGenericLoadable>(SkillsmasPlugin.executingAssembly),
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<MysticsRisky2Utils.BaseAssetTypes.BaseItem>(SkillsmasPlugin.executingAssembly),
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<MysticsRisky2Utils.BaseAssetTypes.BaseEquipment>(SkillsmasPlugin.executingAssembly),
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<MysticsRisky2Utils.BaseAssetTypes.BaseBuff>(SkillsmasPlugin.executingAssembly),
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<MysticsRisky2Utils.BaseAssetTypes.BaseInteractable>(SkillsmasPlugin.executingAssembly),
                () => MysticsRisky2Utils.ContentManagement.ContentLoadHelper.InvokeAfterContentPackLoaded<Skills.BaseSkill>(SkillsmasPlugin.executingAssembly)
            };
            for (int i = 0; i < loadDispatchers.Length; i = num)
            {
                loadDispatchers[i]();
                args.ReportProgress(Util.Remap((float)(i + 1), 0f, (float)loadDispatchers.Length, 0.95f, 0.99f));
                yield return null;
                num = i + 1;
            }

            loadDispatchers = null;
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            void OrderCharacterSkills(SkillFamily skillFamily, params string[] skillNames)
            {
                var defaultSkillVariant = skillFamily.variants[skillFamily.defaultVariantIndex];
                var variantsList = skillFamily.variants.ToList();
                var orderedSkillVariants = new List<SkillFamily.Variant>();
                foreach (var skillName in skillNames)
                {
                    var skillVariant = variantsList.FirstOrDefault(x => x.skillDef.skillName == skillName);
                    if (!skillVariant.Equals(default(SkillFamily.Variant)))
                    {
                        orderedSkillVariants.Add(skillVariant);
                        variantsList.Remove(skillVariant);
                    }
                }
                int i = 0;
                foreach (var variant in orderedSkillVariants)
                {
                    variantsList.Insert(i, variant);
                    i++;
                }

                skillFamily.variants = variantsList.ToArray();
                skillFamily.defaultVariantIndex = (uint)variantsList.IndexOf(defaultSkillVariant);
            }

            var actions = new System.Action[]
            {
                () => OrderCharacterSkills(Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyPrimaryFamily.asset").WaitForCompletion(), "FireFirebolt", "FireLightningBolt", "Skillsmas_CryoBolt", "Skillsmas_RockBolt"),
                () => OrderCharacterSkills(Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySecondaryFamily.asset").WaitForCompletion(), "Skillsmas_FlameBomb", "NovaBomb", "IceBomb", "Skillsmas_RockBomb"),
                () => OrderCharacterSkills(Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyUtilityFamily.asset").WaitForCompletion(), "Skillsmas_FireWall", "Skillsmas_LightningPillar", "Wall", "Skillsmas_RockPlatform"),
                () => OrderCharacterSkills(Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySpecialFamily.asset").WaitForCompletion(), "Flamethrower", "FlyUp", "Skillsmas_FrostBarrier", "Skillsmas_RockMeteor")
            };
            int num;
            for (var i = 0; i < actions.Length; i = num)
            {
                actions[i]();
                args.ReportProgress(Util.Remap(i + 1, 0f, actions.Length, 0f, 0.99f));
                yield return null;
                num = i + 1;
            }

            args.ReportProgress(1f);
            yield break;
        }

        private ContentPack contentPack = new ContentPack();

        public static class Resources
        {
            public static List<GameObject> bodyPrefabs = new List<GameObject>();
            public static List<GameObject> masterPrefabs = new List<GameObject>();
            public static List<GameObject> projectilePrefabs = new List<GameObject>();
            public static List<GameObject> effectPrefabs = new List<GameObject>();
            public static List<GameObject> gameModePrefabs = new List<GameObject>();
            public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();
            public static List<NetworkSoundEventDef> networkSoundEventDefs = new List<NetworkSoundEventDef>();
            public static List<UnlockableDef> unlockableDefs = new List<UnlockableDef>();
            public static List<System.Type> entityStateTypes = new List<System.Type>();
            public static List<RoR2.Skills.SkillDef> skillDefs = new List<RoR2.Skills.SkillDef>();
            public static List<RoR2.Skills.SkillFamily> skillFamilies = new List<RoR2.Skills.SkillFamily>();
            public static List<SceneDef> sceneDefs = new List<SceneDef>();
            public static List<GameEndingDef> gameEndingDefs = new List<GameEndingDef>();
        }

        public static class Items
        {
            public static ItemDef Skillsmas_TemporaryMicrobots;
        }

        public static class Buffs
        {
            public static BuffDef Skillsmas_StopBarrierDecay;
            public static BuffDef Skillsmas_ChainKillActive;
            public static BuffDef Skillsmas_EnemyMark;
            public static BuffDef Skillsmas_MarkedEnemyHit;
            public static BuffDef Skillsmas_MarkedEnemyKill;
            public static BuffDef Skillsmas_ChainKillBonusDamage;
            public static BuffDef Skillsmas_ToolbotUpdated;
        }
    }
}
