using Dusk;

namespace CodeRebirth.src.Content.Skins;
public class SkinHandler : ContentHandler<SkinHandler>
{
    public class CRHalloweenAssets(DuskMod mod, string filePath) : AssetBundleLoader<CRHalloweenAssets>(mod, filePath)
    {
    }

    public CRHalloweenAssets? CRHalloween = null;

    public SkinHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("halloweencrassets", out CRHalloween);
    }
}