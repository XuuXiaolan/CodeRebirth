using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;

namespace CodeRebirth.src.Content.MoonAddons;
public class MoonAddonsHandler : ContentHandler<MoonAddonsHandler>
{
    public class MoonUnlockerAssets(string bundleName) : AssetBundleLoader<MoonUnlockerAssets>(bundleName)
    {
    }

    public class OxydeLoreAssets(string bundleName) : AssetBundleLoader<OxydeLoreAssets>(bundleName)
    {
    }

    public MoonUnlockerAssets? MoonUnlocker { get; private set; } = null;
    public OxydeLoreAssets? OxydeLore { get; private set; } = null;

    public MoonAddonsHandler()
    {
        OxydeLore = LoadAndRegisterAssets<OxydeLoreAssets>("oxydeloreassets");

        MoonUnlocker = LoadAndRegisterAssets<MoonUnlockerAssets>("moonunlockerassets");
    }
}