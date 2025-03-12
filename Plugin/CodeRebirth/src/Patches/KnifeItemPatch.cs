namespace CodeRebirth.src.Patches;
public static class KnifeItemPatch
{
    public static void Init()
    {
        On.KnifeItem.HitKnife += KnifeItem_HitKnife;
    }

    private static void KnifeItem_HitKnife(On.KnifeItem.orig_HitKnife orig, KnifeItem self, bool cancel)
    {
        orig(self, cancel);
        if (Plugin.ModConfig.ConfigDebugMode.Value)
        {
            self.playerHeldBy.DamagePlayer(10, true, true, CauseOfDeath.Bludgeoning, 0, false, default);
        }
    }
}