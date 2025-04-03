using System.IO;
using System.Runtime.CompilerServices;
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
        foreach (string file in Directory.GetFiles(path, "*.xskinbundle", SearchOption.AllDirectories))
        {
            var skinBundle = AssetBundle.LoadFromFile(file);
            CRSkinDefinitions[] CRSkinDefinitions = skinBundle.LoadAllAssets<CRSkinDefinitions>();
            foreach (var skinDefinition in CRSkinDefinitions)
            {
                foreach (var skin in skinDefinition.BaseSkins)
                {
                    Plugin.ExtendedLogging($"Loading skin: {skin.Label}");
                    Plugin.ExtendedLogging($"Author: {skinDefinition.authorName}");
                    skinContentHandler.LoadAndRegisterSkin(skin, skinDefinition.authorName, skinDefinition.config);
                }
            }
        }
	}
}