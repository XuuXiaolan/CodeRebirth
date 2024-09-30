namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        if (!IsHost) return;
        creatureVoice.volume = 0.5f;
    }

    protected override void DoCompleteQuest(QuestCompletion reason)
    {
        base.DoCompleteQuest(reason);
        creatureVoice.volume = 0.25f;
    }
}