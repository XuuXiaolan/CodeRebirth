using CodeRebirth.src.Util;

namespace CodeRebirth.src.Patches;
public static class GameNetworkManagerPatch
{
    public static void Init()
    {
        On.GameNetworkManager.SaveItemsInShip += GameNetworkManager_SaveItemsInShip;
        On.GameNetworkManager.ResetSavedGameValues += GameNetworkManager_ResetSavedGameValues;
    }

    private static void GameNetworkManager_SaveItemsInShip(On.GameNetworkManager.orig_SaveItemsInShip orig, GameNetworkManager self)
    {
        orig(self);
        CodeRebirthUtils.Instance.SaveCodeRebirthData();
    }

    private static void GameNetworkManager_ResetSavedGameValues(On.GameNetworkManager.orig_ResetSavedGameValues orig, GameNetworkManager self)
    {
        orig(self);
        ES3Settings settings;
        if (CodeRebirthUtils.Instance != null)
        {
            settings = CodeRebirthUtils.Instance.SaveSettings;
        }
        else
        {
            settings = new ES3Settings($"CR{GameNetworkManager.Instance.currentSaveFileName}", ES3.EncryptionType.None);
        }
        CodeRebirthUtils.ResetCodeRebirthData(settings);
    }
}