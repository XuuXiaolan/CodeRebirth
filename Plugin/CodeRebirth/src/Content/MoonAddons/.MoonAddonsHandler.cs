using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;

namespace CodeRebirth.src.Content.MoonAddons;
public class MoonAddonsHandler : ContentHandler<MoonAddonsHandler>
{
    public class MoonUnlockerAssets(string bundleName) : AssetBundleLoader<MoonUnlockerAssets>(bundleName)
    {
    }

    public MoonUnlockerAssets? MoonUnlocker { get; private set; } = null;

    public MoonAddonsHandler()
    {
        MoonUnlocker = LoadAndRegisterAssets<MoonUnlockerAssets>("moonunlockerassets");
    }
}