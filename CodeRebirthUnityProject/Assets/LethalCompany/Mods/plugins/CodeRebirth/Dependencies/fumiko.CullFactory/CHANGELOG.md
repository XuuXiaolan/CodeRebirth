# 1.5.0

## Added

- An API method to update a light that has moved in or out of the interior

# 1.4.3

## Fixed

- Culling issues on UT99 CIDOM Metro, which will now use the fallback portals by default

# 1.4.2

## Fixed

- Items being invisible when the player holds them while wearing a model replacement via ModelReplacementAPI

# 1.4.1

## Fixed

- Items being initially invisible until their visibility changes

# 1.4.0

## Added

- An API to allow disabling culling per-camera via a CameraCullingOptions component

## Fixed

- An issue where some items would be visible off-screen after spawning

# 1.3.15

## Fixed

- Item interactions being permanently broken on 1.3.14

# 1.3.14

## Fixed

- Items being invisible after being removed from the Capsule Hoi-Poi item in the AddonFusion mod

# 1.3.13

## Fixed

- An issue where items that are held by a non-player non-enemy could be invisible after teleporting

# 1.3.12

## Fixed

- Errors that would spam in the console if an item was destroyed while it needed to be checked for changes

# 1.3.11

## Fixed

- Items being invisible for some clients after another client teleported in/out of the interior and dropped them

# 1.3.10

## Fixed

- The apparatus and other forcibly-spawned items in interiors being invisible

# 1.3.9

## Fixed

- Items being invisible when dropped from a belt bag inside the interior

# 1.3.8

## Fixed

- An error preventing LAN players to join games

# 1.3.7

## Changed

- Reduced the size of the mod by removing the Burst assembly's pdb and all referenced packages within it except for the Unity.Burst plugin

## Fixed

- Burst assembly code being unused due to applying the patch to ignore failure to resolve Burst-compiled methods too early

# 1.3.6

## Fixed

- Breakage of LethalCompanyVR due to missing Burst-compiled methods, these errors will now be silently ignored with functionality unaffected

# 1.3.5

## Fixed

- Culling never activating alongside some unidentified mod(s) due to a BepInEx bug

# 1.3.4

## Fixed

- Non-functional null checks causing errors when loading some levels

# 1.3.3

## Fixed

- A NullReferenceException that would occur on the Mental Hospital interior due to lights lacking HDRP properties

# 1.3.2

## Fixed

- The interior disappearing in the mineshaft's entry tile when standing near the "normal" entrance position

# 1.3.1

## Fixed

- Light culling issues in the vanilla mineshaft's entry tile
- The disappearing world in Scoopy's entry tile, theoretically this should be prevented in most cases where the generation is logical

# 1.3.0

## Added

- An option (`Disable LOD culling`) to prevent LOD in interior objects from causing them to disappear visibly
- Calculation of influence of spot lights to reduce the visible geometry further

## Changed

- `Disable shadow distance fading` will now only disable shadow fading for a light using a heuristic to avoid performance degradation
- Culling is enabled by default on Grand Armory again, as performance is no longer regressed thanks to the above change

## Fixed

- Disabling distance culling after loading a level with it enabled would cause the far plane to reset to the configured interior far plane distance

# 1.2.3

## Fixed

- Items being invisible in the entry tile of the new interior

# 1.2.2

## Fixed

- Packages built into the Burst assembly will now use the versions packaged with Lethal Company to avoid tricky bugs

# 1.2.1

## Fixed

- Culling issue in the entry tile of the new interior in v60
- Crash when joining a LAN host

# 1.2.0

## Added

- A Burst-compiled assembly which will contain optimized algorithms to be used for culling
- An optimization to prevent some work from being done when culled tiles don't change

## Fixed

- A freeze that could occur on any interior, but most commonly the Grand Armory interior

# 1.1.5

## Fixed

- Freezes on the Grand Armory interior due to an issue in Unity, it will be blacklisted until further notice

# 1.1.4

## Changed

- Removed the Grand Armory interior from the blacklist, culling issues have been fixed

# 1.1.3

## Fixed

- Imperium's night vision not being visible on the player camera

# 1.1.2

## Fixed

- Added Wesley's upcoming interior to the blacklist, as issues are anticipated at release

# 1.1.1

## Fixed

- The render distance (camera far plane) being set based on where the player previously was when teleporting

# 1.1.0 (requires v50)

## Added

- Tracking of items held by enemies that can enter and leave the interior

## Fixed

- Items becoming invisible if custom moons cause errors while a player enters/leaves the interior

# 1.0.4

## Fixed

- All lights being culled when LethalCompanyVR is active (tentative fix)

# 1.0.3

## Fixed

- Items spawned in the interior through LethalLib remaining invisible permanently, they will now never be culled

# 1.0.2

## Fixed

- Error spam that could occur after exiting to menu, loading into a save again and landing on a moon

# 1.0.1

## Fixed

- The flashlight from [Spectate Enemies](https://thunderstore.io/c/lethal-company/p/AllToasters/SpectateEnemies/) being invisible when viewing an enemy in the interior

# 1.0.0

## Added

- Visibility culling of items inside the dungeon
- Culling of all items outside the dungeon when inside the dungeon and vice versa
- An option that is enabled by default preventing shadowed lights shining through walls when viewing them from afar (i.e. the Mansion's chandeliers in the entry tile)

## Fixed

- Dynamic lights such as flashlights shining through walls
- Faraway shadowed lights on the opposite sides of a walls disappearing and reappearing when moving between tiles
- Puddles in the vanilla Factory interior sometimes disappearing visibly at certain camera angles
- Setting the `[Distance culling] Enabled` option to `false` in-game would set the far planes incorrectly

# 0.9.3

## Fixed

- Better compatibility with DunGen's automatic tile bounds

# 0.9.2

## Changed

- Use fallback portals on SleepsDungeon "School"

## Fixed

- Shadow-casting lights shining through walls when they shouldn't
- Incorrectly inferred room sizes when tile bounds are automatically calculated

# 0.9.1

## Fixed

- Static lights should no longer shine through walls

# 0.9.0

## Changed

- The default/recommended culling frequency is now 0 (Updates every game cycle)
- Light culling is now more accurate

## Fixed

- Portal occlusion culling now accurately reads objects' visibility states from the previous frame.
- Errors occurred when using CullFactory should no longer cause the game to freeze

# 0.8.7

## Fixed

- Removed network-synced object spawn tracking (This should allow door codes to function again)

# 0.8.6

## Changed

- Print warning in console when a tile component is removed unexpectedly

## Fixed

- Tile being entirely excluded from rendering when only partial objects are problematic

# 0.8.5

## Fixed

- Removed benchmarking behaviour when B is pressed

# 0.8.4

## Fixed

- Camera stops rendering when a renderer or light is removed after level generation

# 0.8.3

## Fixed

- Far plane culling not responding to changed settings during gameplay

# 0.8.2

## Fixed

- Mod not being properly initialized when installed manually

# 0.8.1

## Added

- An option to disable compatibility mode on known problematic interiors

## Fixed

- An issue with changing the fallback lists during gameplay

# 0.8.0

## Added

- A compatibility mode for incorrect portals (Enabled by default)

## Fixed

- **Scoopys Variety Mod's interiors** would sometimes appear invisible with portal occlusion culling
  - This is fixed by using the compatibility mode mentioned above
- Shadows flickering on **Scarlet Devil Mansion** with portal occlusion culling
- Depth culling sometimes causing interiors to become entirely invisible

# 0.7.2

## Added

- Options to visualize the portals and tile bounds

## Changed

- The render distance option is now opt-in with the `[Distance culling] Enabled` option

## Fixed

- The entrance room on **Scoopy's Sewer interior** would appear invisible at certain angles
- Depth culling sometimes allowing tiles to remain invisible on cameras
- Camera render distance not changing when entering or leaving on clients, or when using the ship teleporters

# 0.7.1

## Fixed

- Various issues due to not fully pulling update commits

# 0.7.0

## Added

- Portal occlusion culling (thanks to Zaggy1024!)

# 0.6.2

## Fixed

- Fallback tile being used when none is available

# 0.6.1

## Removed

- Lights no longer gets removed from volumetric lighting

# 0.6.0

## Added

- Remembers player's last valid position when out of bounds
- Culling for radar boosters

# 0.5.3

## Fixed

- Culling distance options works the other way around

# 0.5.2

## Fixed

- Culling finishes early after the first target

# 0.5.1

## Added

- Control for far plane distance when player is on the surface
- Volumetric lighting is disabled in factories

## Changed

- Far plane clips further when player is outside

## Fixed

- Culling stops working when any monitored player is outside

# 0.5.0

## Known issues

- Culling also happens when you're outside

## Changed

- Distance-based culling now modifies the camera's far plane, allowing parts of a tile to be culled
- Depth-based culling now applies to all players

# 0.4.1

## Added

- Changelog

# 0.4.0

## Changed

- Update frequency now affects all culling methods
- Moved logging option to the **general** category

## Fixed

- Tiles with null doorways halting the culling process

# 0.3.0

## Added

- All meshes will be culled when the factory is not being observed*

*Note: The local player (yourself), and all monitors are all outside the factory.

(Read the full commit history [here](https://github.com/2394425147/LC_CullFactory/commits/master/))
