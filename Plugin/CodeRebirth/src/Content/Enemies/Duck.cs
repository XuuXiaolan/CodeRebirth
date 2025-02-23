namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        questTimer = Plugin.ModConfig.ConfigDuckSongTimer.Value;
    }
}