using CodeRebirthLib.ContentManagement.Enemies;

namespace CodeRebirth.src.Content.Enemies;
public class Duck : QuestMasterAI
{
    public override void Start()
    {
        base.Start();
        questTimer = 120f;
        if (Plugin.Mod.EnemyRegistry().TryGetFromEnemyName("Duck", out CREnemyDefinition? CREnemyDefinition))
        {
            questTimer = CREnemyDefinition.GetGeneralConfig<float>("Duck | Quest Timer").Value;
        }
    }
}