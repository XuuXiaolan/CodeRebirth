using System;

namespace CodeRebirth.src.MiscScripts.ConfigManager;

public static class ConfigMisc
{
    public static void CreateDynamicGeneralConfig(CRDynamicConfig configDefinition, string configName)
    {
        switch (configDefinition.DynamicConfigType)
        {
            case CRDynamicConfigType.String:
                CRConfigManager.CreateGeneralConfig(
                    Plugin.configFile,
                    configName,
                    configDefinition.settingName,
                    configDefinition.settingDesc,
                    configDefinition.defaultString,
                    configDefinition.Description
                );
                break;
            case CRDynamicConfigType.Int:
                CRConfigManager.CreateGeneralConfig(
                    Plugin.configFile,
                    configName,
                    configDefinition.settingName,
                    configDefinition.settingDesc,
                    configDefinition.defaultInt,
                    configDefinition.Description
                );
                break;
            case CRDynamicConfigType.Float:
                CRConfigManager.CreateGeneralConfig(
                    Plugin.configFile,
                    configName,
                    configDefinition.settingName,
                    configDefinition.settingDesc,
                    configDefinition.defaultFloat,
                    configDefinition.Description
                );
                break;
            case CRDynamicConfigType.Bool:
                CRConfigManager.CreateGeneralConfig(
                    Plugin.configFile,
                    configName,
                    configDefinition.settingName,
                    configDefinition.settingDesc,
                    configDefinition.defaultBool,
                    configDefinition.Description
                );
                break;
            default:
                throw new NotImplementedException("Dynamic config type not implemented.");
        }
    }    
}
