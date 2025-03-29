using System.IO;
using System.Runtime.CompilerServices;
using AntlerShed.SkinRegistry;
using CodeRebirthESKR.Misc;
using UnityEngine;

namespace CodeRebirthESKR;
public static class SkinsHandler
{
	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	public static void RegisterSkins()
	{
        SkinContentHandler skinContentHandler = new SkinContentHandler();
        string path = BepInEx.Paths.PluginPath;
        foreach (string file in Directory.GetFiles(path, "*.crskinbundle", SearchOption.AllDirectories))
        {
            var skinBundle = AssetBundle.LoadFromFile(file);
            CRSkinDefinitions[] CRSkinDefinitions = skinBundle.LoadAllAssets<CRSkinDefinitions>();
            foreach (var skinDefinition in CRSkinDefinitions)
            {
                foreach (var skin in skinDefinition.BaseSkins)
                {
                    Plugin.ExtendedLogging($"Loading skin: {skin.Label}");
                    Plugin.ExtendedLogging($"Author: {skinDefinition.authorName}");
                    DefaultSkinConfigData defaultConfig = new DefaultSkinConfigData
                    (
                        //List of moon-id to frequency pairs
                        // 0.0 means it never appears, 1.0 means it appears
                        //as frequently as possible when considering other
                        //skins 
                        [
                            new DefaultSkinConfigEntry
                            (
                                EnemySkinRegistry.OFFENSE_ID,
                                1.0f
                            ),
                            new DefaultSkinConfigEntry
                            (
                                EnemySkinRegistry.MARCH_ID,
                                0.5f
                            ),
                        ],
                        //Default frequency
                        //The frequency of this skin on any unconfigured map
                        0.0f,
                        //Vanilla fallback frequency
                        //Optional - frequency of the vanilla appearance if 			//this default config ends up making a new config 			//entry for the enemy on a moon
                        0.0f
                    );
                    skinContentHandler.LoadAndRegisterSkin(skin, skinDefinition.authorName, defaultConfig);
                }
            }
        }
	}
}