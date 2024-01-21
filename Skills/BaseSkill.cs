using RoR2;
using UnityEngine;
using RoR2.Skills;
using MysticsRisky2Utils;
using System.Collections.Generic;

namespace Skillsmas.Skills
{
    public abstract class BaseSkill : MysticsRisky2Utils.ContentManagement.BaseLoadableAsset
    {
        public SkillDef skillDef;
        public SkillFamily.Variant skillFamilyVariant;

        public ConfigOptions.ConfigurableValue<float> configCooldown;
        public ConfigOptions.ConfigurableValue<int> configMaxStock;
        public ConfigOptions.ConfigurableValue<int> configRechargeStock;
        public ConfigOptions.ConfigurableValue<int> configRequiredStock;
        public ConfigOptions.ConfigurableValue<int> configStockToConsume;
        public ConfigOptions.ConfigurableValue<bool> configCancelSprint;
        public ConfigOptions.ConfigurableValue<bool> configForceSprint;
        public ConfigOptions.ConfigurableValue<bool> configCancelledFromSprinting;
        public ConfigOptions.ConfigurableValue<bool> configIsCombatSkill;
        public ConfigOptions.ConfigurableValue<bool> configMustKeyPress;

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
            configCooldown = ConfigOptions.ConfigurableValue.CreateFloat(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cooldown",
                baseRechargeInterval,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.baseRechargeInterval = newValue
            );
            configMaxStock = ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Max Stock",
                baseMaxStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.baseMaxStock = newValue
            );
            configRechargeStock = ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Recharge Stock",
                rechargeStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.rechargeStock = newValue
            );
            configRequiredStock = ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Required Stock",
                requiredStock,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.requiredStock = newValue
            );
            configStockToConsume = ConfigOptions.ConfigurableValue.CreateInt(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Stock To Consume",
                stockToConsume,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.stockToConsume = newValue
            );
            configCancelSprint = ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cancel Sprint",
                cancelSprintingOnActivation,
                onChanged: (newValue) => skillDef.cancelSprintingOnActivation = newValue
            );
            configForceSprint = ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Force Sprint",
                forceSprintDuringState,
                onChanged: (newValue) => skillDef.forceSprintDuringState = newValue
            );
            configCancelledFromSprinting = ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Cancelled From Sprinting",
                canceledFromSprinting,
                onChanged: (newValue) => skillDef.canceledFromSprinting = newValue
            );
            configIsCombatSkill = ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Is Combat Skill",
                isCombatSkill,
                useDefaultValueConfigEntry: SkillsmasPlugin.ignoreBalanceConfig.bepinexConfigEntry,
                onChanged: (newValue) => skillDef.isCombatSkill = newValue
            );
            configMustKeyPress = ConfigOptions.ConfigurableValue.CreateBool(
                SkillsmasPlugin.PluginGUID,
                SkillsmasPlugin.PluginName,
                SkillsmasPlugin.config,
                skillName,
                "Must Key Press",
                mustKeyPress,
                onChanged: (newValue) => skillDef.mustKeyPress = newValue
            );
        }
    }
}