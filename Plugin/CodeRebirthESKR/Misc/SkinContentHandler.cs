using AntlerShed.SkinRegistry;
using CodeRebirth.src.MiscScripts.ConfigManager;
using CodeRebirthESKR.SkinRegistry;

namespace CodeRebirthESKR.Misc;
public class SkinContentHandler
{
	internal static SkinContentHandler Instance { get; private set; } = null!;
	internal SkinContentHandler()
    {
		Instance = this;
	}

    protected void LoadEnabledConfigs(string keyName)
    {
        var config = CRConfigManager.CreateEnabledEntry(Plugin.configFile, keyName, keyName, "Enabled", true, $"Whether the {keyName} skin is enabled.");
        CRConfigManager.CRConfigs[keyName] = config;
    }

    public void LoadAndRegisterSkin(BaseSkin baseSkin, string authorName)
    {
        LoadEnabledConfigs(baseSkin.Label);
        bool loadSkin = CRConfigManager.GetEnabledConfigResult(baseSkin.Label);
        if (!loadSkin) return;

		EnemySkinRegistry.RegisterSkin(baseSkin);
		EnemySkinRegistry.RegisterEnemy($"{authorName}.{baseSkin.EnemyId}", baseSkin.Label, baseSkin.spawnLocation);
    }
}