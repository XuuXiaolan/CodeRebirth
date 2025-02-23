using CodeRebirth.src.Util;

namespace CodeRebirth.src.Patches;
public static class GameNetworkManagerPatch
{
    public static void Init()
    {
        On.GameNetworkManager.SaveItemsInShip += GameNetworkManager_SaveItemsInShip;
    }

    private static void GameNetworkManager_SaveItemsInShip(On.GameNetworkManager.orig_SaveItemsInShip orig, GameNetworkManager self)
    {
        orig(self);
        CodeRebirthUtils.SaveCodeRebirthData();
    }
}