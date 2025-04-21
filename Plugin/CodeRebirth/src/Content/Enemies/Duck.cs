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
        List<CRDynamicConfig> configDefinitions = EnemyHandler.Instance.DuckSong!.EnemyDefinitions.GetCREnemyDefinitionWithEnemyName(enemyType.enemyName)!.ConfigEntries;
        CRDynamicConfig? configSetting = configDefinitions.GetCRDynamicConfigWithSetting("Duck", "Quest Timer");
        if (configSetting != null)
        {
            questTimer = CRConfigManager.GetGeneralConfigEntry<float>(configSetting.settingName, configSetting.settingDesc).Value;
        }
    }
}