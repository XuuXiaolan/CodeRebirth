

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        questTimer = EnemyHandler.Instance.DuckSong.GetConfig<float>("Duck | Quest Timer").Value;
    }
}