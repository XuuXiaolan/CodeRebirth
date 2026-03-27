using Dusk;

namespace CodeRebirth.src.Content.DevTools;

public class DevToolHandler : ContentHandler<DevToolHandler>
{
    public class DevToolsAssets(DuskMod mod, string filePath) : AssetBundleLoader<DevToolsAssets>(mod, filePath)
    {
    }

    public DevToolsAssets? DevTools = null;

    public DevToolHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("devtoolsassets", out DevTools);
    }
}