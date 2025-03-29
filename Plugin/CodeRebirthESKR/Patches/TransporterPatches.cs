using AntlerShed.SkinRegistry;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirthESKR.Patches;
public static class TransporterPatch
{
    public static void Init()
    {
        On.EnemyAI.Start += OnEnemyAI_Start;
    }

    private static void OnEnemyAI_Start(On.EnemyAI.orig_Start orig, EnemyAI self)
    {
        orig(self);
        if (self is Transporter)
        {
            System.Random random = new(StartOfRound.Instance.randomMapSeed + RoundManager.Instance.SpawnedEnemies.Count + StartOfRound.Instance.allPlayerScripts.Length);
            Skin randomSkin = EnemySkinRegistry.PickSkinAtValue(self.enemyType.enemyName, self.isOutside ? SpawnLocation.OUTDOOR : SpawnLocation.INDOOR, random.NextFloat(0f, 1f));
            EnemySkinRegistry.ApplySkin(randomSkin, self.enemyType.enemyName, self.gameObject);
        }
    }
}