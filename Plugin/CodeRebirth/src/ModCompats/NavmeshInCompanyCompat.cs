namespace CodeRebirth.src.ModCompats;
internal static class NavmeshInCompanyCompat
{
    internal static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("dev.kittenji.NavMeshInCompany"); } }
}