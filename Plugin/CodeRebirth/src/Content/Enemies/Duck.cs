using System.Collections.Generic;
using CodeRebirthLib.ConfigManagement;

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        questTimer = 120f;
        List<CRDynamicConfig> configDefinitions = EnemyHandler.Instance.DuckSong!.Content.GetCREnemyDefinitionWithEnemyName(enemyType.enemyName)!.ConfigEntries;
        CRDynamicConfig? configSetting = configDefinitions.GetCRDynamicConfigWithSetting("Duck", "Quest Timer");
        if (configSetting != null)
        {
            questTimer = CRConfigManager.GetGeneralConfigEntry<float>(configSetting.settingName, configSetting.settingDesc).Value;
        }
    }
}