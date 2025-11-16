

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public void Awake()
    {
        bool isGlobalAudio = EnemyHandler.Instance.DuckSong.GetConfig<bool>("Duck | Global Spawn Audio").Value;
        creatureUltraVoice.spatialBlend = isGlobalAudio ? 0f : 1f;
        creatureUltraVoice.Play();
    }

    public override void Start()
    {
        base.Start();
        questTimer = EnemyHandler.Instance.DuckSong.GetConfig<float>("Duck | Quest Timer").Value;
        questRepeatChance = EnemyHandler.Instance.DuckSong.GetConfig<int>("Duck | Lemonade Quest Chance").Value;
    }
}