namespace CodeRebirth.EnemyStuff;
public class ScrapMasterAI : QuestMasterAI
{
    public override void Start() { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();
        if (!IsHost) return;
        creatureVoice.volume = 0.5f;
    }

    protected override void DoCompleteQuest(QuestCompletion reason) {
        base.DoCompleteQuest(reason);
        creatureVoice.volume = 0.25f;
    }
}