using CodeRebirth.src.MiscScripts.ConfigManager;

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        if (EnemyHandler.Instance.DuckSong == null || EnemyHandler.Instance.DuckSong.AssetBundleData == null)
        {
            questTimer = 120f;
            Plugin.Logger.LogError($"How the fuck did you even get this error");
            return;
        }
        else
        {
            questTimer = CRConfigManager.GetGeneralConfigEntry<float>(EnemyHandler.Instance.DuckSong.AssetBundleData.configName, "Quest Timer").Value;
        }
        Plugin.ExtendedLogging($"{enemyType.enemyName} has a quest timer of {questTimer} seconds.");
    }
}