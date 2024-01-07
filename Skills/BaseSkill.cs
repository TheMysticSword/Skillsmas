using RoR2;
using UnityEngine;
using RoR2.Skills;
using MysticsRisky2Utils;

namespace Skillsmas.Skills
{
    public abstract class BaseSkill : MysticsRisky2Utils.ContentManagement.BaseLoadableAsset
    {
        public SkillDef skillDef;
        public SkillFamily.Variant skillFamilyVariant;

        public virtual System.Type GetSkillDefType()
        {
            return typeof(SkillDef);
        }
        
        public override void Load()
        {
            skillDef = (SkillDef)ScriptableObject.CreateInstance(GetSkillDefType());
            skillFamilyVariant = new SkillFamily.Variant { skillDef = skillDef };
            OnLoad();
            ((ScriptableObject)skillDef).name = skillDef.skillName;
            asset = skillDef;
        }

        public void SetUpValuesAndOptions(string skillName, float baseRechargeInterval, int baseMaxStock, int rechargeStock, int requiredStock, int stockToConsume, bool cancelSprintingOnActivation, bool forceSprintDuringState, bool canceledFromSprinting, bool isCombatSkill, bool mustKeyPress)
        {
            ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cooldown",
                baseRechargeInterval,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.baseRechargeInterval = newValue
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Max Stock",
                baseMaxStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.baseMaxStock = newValue
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Recharge Stock",
                rechargeStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.rechargeStock = newValue
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Required Stock",
                requiredStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.requiredStock = newValue
            );
            ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Stock To Consume",
                stockToConsume,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.stockToConsume = newValue
            );
            ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cancel Sprint",
                cancelSprintingOnActivation,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.cancelSprintingOnActivation = newValue
            );
            ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Force Sprint",
                forceSprintDuringState,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.forceSprintDuringState = newValue
            );
            ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cancelled From Sprinting",
                canceledFromSprinting,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.canceledFromSprinting = newValue
            );
            ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Is Combat Skill",
                isCombatSkill,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.isCombatSkill = newValue
            );
            ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Must Key Press",
                mustKeyPress,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.mustKeyPress = newValue
            );
        }
    }
}