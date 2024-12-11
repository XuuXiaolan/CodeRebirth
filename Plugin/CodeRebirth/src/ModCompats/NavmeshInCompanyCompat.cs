namespace CodeRebirth.src.ModCompats;
public static class NavmeshInCompanyCompat
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Kittenji.NavMeshInCompany"); } }
}