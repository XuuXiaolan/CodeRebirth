# Lobby Compatibility

[![Build](https://img.shields.io/github/actions/workflow/status/MaxWasUnavailable/LobbyCompatibility/build.yml?style=for-the-badge&logo=github&branch=master)](https://github.com/MaxWasUnavailable/LobbyCompatibility/actions/workflows/build.yml)
[![Latest Version](https://img.shields.io/thunderstore/v/BMX/LobbyCompatibility?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/BMX/LobbyCompatibility)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/BMX/LobbyCompatibility?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/BMX/LobbyCompatibility)
[![NuGet Version](https://img.shields.io/nuget/v/TeamBMX.LobbyCompatibility?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/TeamBMX.LobbyCompatibility)

This mod aims to provide better vanilla/modded lobby compatibility and browsing.

## For Players

### Lobby Browser

This mod lets you know when a lobby is incompatible with your currently installed mods, and will let you know what mods
you need to update, downgrade, download, or remove to join that lobby.

You will notice an icon at the bottom left of every lobby in the lobby browser, and you can see more information (such
as whether it's incompatible, and what mods are causing it) by hovering over it.

![Hovering over the Lobby Compatibility icon](https://raw.githubusercontent.com/MaxWasUnavailable/LobbyCompatibility/master/assets/hover.png)

If you click on the icon, you can then see an in-depth view of the mod list, with a scrollbar to view all mods required
to connect to that server. Note that this only works if the server is running this mod.

![The Lobby Compatibility modal](https://raw.githubusercontent.com/MaxWasUnavailable/LobbyCompatibility/master/assets/modal.png)

If you then attempt to connect to a server - either public or private - with incompatible or missing mods, an error will
display telling you that you are missing required mods.

![Lobby connection error due to incompatible or missing mods](https://raw.githubusercontent.com/MaxWasUnavailable/LobbyCompatibility/master/assets/error.png)

### Modded Leaderboards

This mod adds a separate modded leaderboard to better compare to your friends! The intent is to help split up vanilla
and modded leaderboard listings, so modded runs (which might be easier or harder than vanilla runs) don't get mixed in
with vanilla runs.

## For Developers

To use this, you need to add a package reference to `TeamBMX.LobbyCompatibility` in your `.csproj` file. You can
use the
following code:

```xml
<ItemGroup>
    <PackageReference Include="TeamBMX.LobbyCompatibility" Version="1.*" PrivateAssets="all" />
</ItemGroup>
```

You can also use your IDE's interface to add the reference. For Visual Studio 2022, you do so by clicking on
the `Project` dropdown, and clicking `Manage NuGet Packages`. You then can search for `TeamBMX.LobbyCompatibility`
and add
it from there.

### Usage

#### Attribute

Simply add `[LobbyCompatibility(CompatibilityLevel, VersionStrictness)]` above your `Plugin` class like so:

```csharp
// ...
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("BMX.LobbyCompatibility", DependencyFlags.HardDependency)]
[LobbyCompatibility(CompatibilityLevel = CompatibilityLevel.Everyone, VersionStrictness = VersionStrictness.Minor)]
class MyPlugin : BaseUnityPlugin
{
    // ...
}
```

The enums used are as follows:

##### `CompatibilityLevel`

- `ClientOnly`
    - Mod only impacts the client.
    - Mod is not checked at all (i.e. you can join vanilla lobbies), VersionStrictness does not apply.
- `ServerOnly`
    - Mod only impacts the server, and might implicitly impact the client without the client needing to have it
      installed for it to work.
    - Mod is only required by the server. VersionStrictness only applies if the mod is installed on the client.
- `Everyone`
    - Mod impacts both the client and the server, and adds functionality that requires the mod to be installed on both.
    - Mod must be loaded on server and client. Version checking depends on the VersionStrictness.
- `ClientOptional`
    - Not every client needs to have the mod installed, but if it is installed, the server also needs to have it.
      Generally used for mods that add extra (optional) functionality to the client if the server has it installed.
    - Mod must be loaded on server. Version checking depends on the VersionStrictness.

##### `VersionStrictness`

- `None`
    - No version check is done (x.x.x)
- `Major`
    - Mod must have the same Major version (1.x.x)
- `Minor`
    - Mods must have the same Minor and Major version (1.1.x)
- `Patch`
    - Mods must have the same Patch, Minor, and Major version (1.1.1)

#### Method

Alternatively, as a way to support soft dependencies, you can use the `PluginHelper.RegisterPlugin` method with the
following signature:

```csharp
public static void RegisterPlugin(string guid, Version version, CompatibilityLevel compatibilityLevel, VersionStrictness versionStrictness)
```

> [!IMPORTANT]
>
> This method should be called in the `Awake` method of your plugin's main class, as we cache some data when fetching
> the lobby list.

#### Retrieving & Using the LobbyDiff

If you want to use the lobby diff (the "diff" of mods installed by the lobby and client), it is accessible via `LobbyCompatibility.Features.LobbyHelper.GetLobbyDiff(Lobby lobby)`, like so:

```csharp
using LobbyCompatibility.Features;
using Steamworks.Data;

/* ... */

// Just an example - no data would be returned since the lobby is non-existant.
Lobby lobby = new();
LobbyDiff lobbyDiff = LobbyHelper.GetLobbyDiff(lobby);
```

Then, if you want to check to see if the lobby has a specific mod downloaded, you can it like so:

```csharp
if (lobbyDiff.PluginDiffs.Any(diff => diff.GUID == "example.guid" && diff.ServerVersion != null))
{
    /* Code Here */
}
```

`diff.ServerVersion != null` is used to see if the lobby/server has the mod, as it is set only when the server has the corresponding mod installed.