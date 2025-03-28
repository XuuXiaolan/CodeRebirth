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
        string path = BepInEx.Paths.PluginPath;
        foreach (string file in Directory.GetFiles(path, "*.crskinbundle", SearchOption.AllDirectories))
        {
            var skinBundle = AssetBundle.LoadFromFile(file);
            CRSkinDefinitions[] CRSkinDefinitions = skinBundle.LoadAllAssets<CRSkinDefinitions>();
            foreach (var skinDefinition in CRSkinDefinitions)
            {
                foreach (var skin in skinDefinition.BaseSkins)
                {
                    SkinContentHandler.Instance.LoadAndRegisterSkin(skin, skinDefinition.authorName);
                }
            }
        }
	}
}