using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts.ConfigManager;
using CodeRebirth.src.Util.AssetLoading;

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        questTimer = 120f;
        if (EnemyHandler.Instance.DuckSong == null)
        {
            Plugin.Logger.LogError($"How the fuck did you even get this error");
            return;
        }
        else
        {
            List<CRDynamicConfig> configDefinitions = EnemyHandler.Instance.DuckSong.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName(enemyType.enemyName)!.ConfigEntries;
            CRDynamicConfig? configSetting = configDefinitions.GetCRDynamicConfigWithSetting("Duck", "Quest Timer");
            // string key = $"{configSetting.settingName} | {configSetting.settingDesc}".CleanStringForConfig();
            if (configSetting != null)
            {
                questTimer = CRConfigManager.GetGeneralConfigEntry<float>(configSetting.settingName, configSetting.settingDesc).Value;
                Plugin.ExtendedLogging($"found da config option.");
            }
        }
        Plugin.ExtendedLogging($"{enemyType.enemyName} has a quest timer of {questTimer} seconds.");
    }
}