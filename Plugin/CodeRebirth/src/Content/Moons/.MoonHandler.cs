using Dusk;

namespace CodeRebirth.src.Content.Moons;
[ContentOrder(1)]
public class MoonHandler : ContentHandler<MoonHandler>
{
    public class OxydeAssets(DuskMod mod, string filePath) : AssetBundleLoader<OxydeAssets>(mod, filePath)
    {
    }

    public OxydeAssets? Oxyde = null;

    public MoonHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("oxydeassets", out Oxyde);
    }
}