## Version 2.6.0
- Updated the MoreCompany compatibility patch to spawn cosmetics on the local player. The compatibility feature now requires MoreCompany version 1.11.0 and above.

## Version 2.5.2
- Fixed an issue that would cause the outdoor ambient lighting to flicker when the body cam frame rate was limited.

## Version 2.5.1
- Repackaged 2.5.0 to remove some leftover code causing issues on rainy moons.

## Version 2.5.0
- Added a ReservedItemSlotCore compatibility feature to hide/show holstered items based on perspective.
- Sky effects are changed based on the camera perspective. When a body cam target is looking down a long hallway in the interior, the end of the hallway will now be dark.
- Made body cams be unaffected by the game's gamma setting and the tonemapping pass. This should help with visibility in dark areas, as well as prevent the screen from getting excessively bright when gamma is above normal.
- Added a patch to fix a vanilla bug that prevents players converted by a masked enemy from being targeted by the map.
- Fixed the overlay text for buying the antenna being visible upon joining a game with the body cam unlocked and placed.
- Fixed an issue that caused the PiP command to be unusable after restarting a game.
- Made the MoreCompany compatibility mode continue to function if the patch to spawn cosmetics for the local player fails.
- Prevented compatibility features from stopping mod initialization.

## Version 2.4.5
- Fixed an error that would occur when loading a save with the PiP body cam disabled.

## Version 2.4.4
- Fixed the body cam staying visible in the terminal when going to orbit.

## Version 2.4.3
- Made the initialization at the start of the game much less likely to be prevented by errors from other mods.

## Version 2.4.2
- Fixed an error that would occur when [GeneralImprovements](https://github.com/Shaosil/LethalCompanyMods-GeneralImprovements/)'s `UseBetterMonitors` option is enabled.

## Version 2.4.1
- Hid the overlay when the body cam's screen is powered off.
- Added an option to set the text displayed when the body cam is rendering normally. This is currently independent of whether a player is living or dead.
- Switched initialization to use a different hook to (hopefully) avoid broken saves causing the body cam not to function.

## Version 2.4.0
- Added an overlay that indicates the reason the body cam is not visible. The supported states are:
    - The ship upgrade is enabled, but the body cam antenna has not been bought yet.
    - The antenna is bought but stored, preventing body cams from being used.
    - The body cam's target is invalid, e.g. when the targeted player has been eaten and has no corpse.
    - The body cam is disabled due to its target being safe on the ship.
- Fixed the body cam not appearing when the GeneralImprovements option for extra monitors was enabled while the mesh it depends on is disabled.
- Disabled the body cam view when it is attached to a radar booster in a belt bag to avoid seeing the items stacked in the void.
- API changes:
    - Added the static `BodyCam.MainBodyCam` property to get the body cam displayed on the ship monitors.
    - Improved organization of the `BodyCamComponent` members, moving API to the top and adding comments.

## Version 2.3.1
- Fixed some issues that could cause breakage or crashes upon loading a corrupted save.
- Fixed an issue preventing the body cam from functioning if another mod removes the camera from the small monitor with `DisableCameraOnSmallMonitor` enabled.

## Version 2.3.0
- Reduced overhead when applying framerate limit to the body cam. There is no longer a downside to setting a framerate limit instead of letting the camera render every frame, and performance will scale as expected.
- Fixed light flickering that could occur on body cams when their framerate is limited.
- Added API functionality to track when body cams are created and destroyed.
- Added an API flag to body cams to determine whether they should be considered wirelessly connected to the ship.

## Version 2.2.4
- Prevented the vanilla first person arms from showing in body cams when using a viewmodel replacement through [ModelReplacementAPI](https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI).

## Version 2.2.3
- Prevented an error that could occur when creating the terminal PiP body cam command alongside [darmuhsTerminalStuff](https://github.com/darmuh/TerminalStuff) v3.5.0.

## Version 2.2.2
- Fixed visibility issues when viewing a dead player's perspective through a body cam.
- Prevented a NullReferenceException that could occur if the terminal tried to load a null node.

## Version 2.2.1
- Fixed an issue that prevented body cams created by API users from displaying output when attached to arbitrary objects.
- Added a feature to allow API users to hide any set of renderers they choose.

## Version 2.2.0
- Implemented switching perspectives for flashlights held by players viewed by body cams. Flashlights that are in the view player's hand will now emit light from the item instead of the helmet light that is used in third person.

## Version 2.1.2
- Fixed centipede tracking thinking a centiped was clinged to two players at once if two clients send messages that they are being clinged at the same time. This is likely the cause of the issue that was worked around in 2.1.1.

## Version 2.1.1
- Tentatively fixed NullReferenceExceptions which could occur when a centipede clings to a player's head.
- Prevented NullReferenceExceptions when running LethalPipeRemoval to destroy the door screen.

## Version 2.1.0
- Modified the HorizontalResolution option to only affect the main body cam. API users can now set the `Resolution` property to override the default resolution of 160x120.

## Version 2.0.6
- Prevented a NullReferenceException when API users ([TwoRadarMaps](https://github.com/Zaggy1024/LC_TwoRadarMaps/), [darmuhsTerminalStuff](https://github.com/darmuh/TerminalStuff)) create a body cam that is synchronized with a radar map.
- Removed the warning that `DisplayOriginalScreenWhenDisabled` will not work when GeneralImprovements's UseBetterMonitors is enabled.

## Version 2.0.5
- Added support for falling back to [GeneralImprovements](https://github.com/Shaosil/LethalCompanyMods-GeneralImprovements/)'s monitor assignments when the body cam is disabled.
- Fixed the monitor index ordering with [GeneralImprovements](https://github.com/Shaosil/LethalCompanyMods-GeneralImprovements/)'s `UseBetterMonitors` on and `AddMoreBetterMonitors` off.

## Version 2.0.4
- Fixed an error that could occur when [darmuhsTerminalStuff](https://github.com/darmuh/TerminalStuff) tried to create a body cam, caused by some debug logging that is now removed.
- Added a tip upon the first load into a game on 2.0.4 to notify users that the body cams are a ship upgrade now.

## Version 2.0.3
- Prevented error spam that would occur when a body cam switches from targeting a solo player to targeting nothing.
- Fixed a long-standing issue where the body cam would not switch back on if it was targeting a dead player with no corpse while players respawned.
- Fixed invalid targets sometimes displaying on the body cam after the 2.0.0 update.
- Only disable the main body cam based on the EnableCamera option, and exclude any body cams created by API users.

## Version 2.0.2
- Fixed the antenna prefab not spawning on clients.

## Version 2.0.1
- Switched the default value for the ship upgrade option to be true by default as it was intended to be in 2.0.0. Old values will be migrated.
- Fixed a compatibility issue with [darmuhsTerminalStuff](https://github.com/darmuh/TerminalStuff)'s minicams command.
- Prevented errors that would occur when the debugging options are enabled.

## Version 2.0.0 (requires v50)
- Added a ship upgrade to enable body cams for 200 credits which is enabled by default if LethalLib is installed.
- Added support for adjusting the position of tulip snakes and snare fleas based on the view perspective.
- Added support for vanilla cosmetics (bunny ears, bee antennae).
- The screen that the body cam occupies will now switch back to the camera that originally displayed when the body cam is invalid, when [GeneralImprovements](https://github.com/Shaosil/LethalCompanyMods-GeneralImprovements/)'s UseBetterMonitors is not enabled.
- Added an option to swap the external and internal cameras' positions when [GeneralImprovements](https://github.com/Shaosil/LethalCompanyMods-GeneralImprovements/)'s UseBetterMonitors is not enabled.
- Added an option to modify the external camera screen's emissive color.
- Fixed an issue where the radar map's night vision light would be visible on body cams when CullFactory is enabled.
- The camera that is replaced by the body cam will now be disabled to improve performance.
- Allowed API users to override a body cam's resolution and DisableCameraWhileTargetIsOnShip option.

## Version 1.3.0
- Fixed a long-standing issue where MoreCompany cosmetics being destroyed on a masked enemy would cause a freeze.
- Significantly reworked how cosmetics are updated. These changes may introduce old or new bugs, please report them on GitHub.

## Version 1.2.11
- Fixed error spam that would occur when the Advanced Company light shoes curse was lifted.

## Version 1.2.10
- Prevent errors when body cams are immediately destroyed between setting and resetting the perspective.

## Version 1.2.9
- Fixed a freeze/error spam that would occur if MoreCompany encountered an error while syncing cosmetics.

## Version 1.2.8
- Fixed the green transition animation becoming permanently frozen on the body cam screen if `UseTargetTransitionAnimation` was disabled.

## Version 1.2.7
- Fixed the green transition appearing on the player's view when switching from a dead body or mimic to that player. The transition could remain visible until the target was switched again if `DisableCameraWhileTargetIsOnShip` was enabled.
- Allowed `DisableCameraWhileTargetIsOnShip` to disable body cams based on whether other targets types (corpses, masked players, and radar boosters) are on the ship.

## Version 1.2.6
- Fixed the body cam not powering back on after the monitors are turned off.

## Version 1.2.5
- Fixed an error that would occur if the door screen was destroyed.

## Version 1.2.4
- Fixed disabling the transition animation causing the green rectangle to get stuck on the body cam screen. Note that this probably doesn't fix [issue #23](https://github.com/Zaggy1024/LC_OpenBodyCams/issues/23).

## Version 1.2.3
- Optimized some hot code which may help performance very slightly.

## Version 1.2.2
- Added two options to allow collection of useful debug information if error spam is encountered, information which should point directly to the problematic mod. More information is available in the readme.
- Prevented cosmetics from being collected from other mods if they have been destroyed. Not sure if this has been hit in the wild, but this may prevent some error spam.

## Version 1.2.1
- Fixed an error that would occur when exiting to the menu and starting a game again with the picture-in-picture body cam enabled.

## Version 1.2.0
- Added an _opt-in_ `view bodycam` command to the terminal to display a picture-in-picture view of the bodycam. [TwoRadarMaps](https://thunderstore.io/c/lethal-company/p/Zaggy1024/TwoRadarMaps/) v1.2.0 will use a separate bodycam when the feature is enabled.
- Developer features: Added an API to force a body cam to render, and allowed body cams to function without being attached to a renderer's visibility/materials.

## Version 1.1.6
- (Tentatively) fixed an error that could softlock the ship when viewing a player with a custom ragdoll. This could occur when a player was killed by Herobrine mod.

## Version 1.1.5
- Fixed error spam that would occur when changing AdvancedCompany cosmetics.
- Made the PrintCosmeticsDebugInfo option work when enabled at startup.

## Version 1.1.4
- Added an option to disable the green flash animation displayed when changing targets on the body cam.
- HDLethalCompany graphics options will now apply to the body cam.

## Version 1.1.3
- Fixed compatibility for MoreCompany versions greater than 1.8.0. Note that there appears to be an issue where upon joining a server, cosmetics may appear blocking the view of the body cam, which may be caused by an error in MoreCompany.

## Version 1.1.2
- Fixed error spam that could occur when changing models with [ModelReplacementAPI](https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI).
- Added a debug option that prints extra information about which cosmetics are being collected for each player, and the reason they are being collected. If you have issues with cosmetics/model replacements, I may ask for you to enable this to get new logs.

## Version 1.1.1
- Hopefully prevented an issue where in rare cases, masked players being viewed by the body cam would cause the screen to freeze. This was perhaps caused by other mods deleting renderers attached to the masked players after they were created.

## Version 1.1.0
- Refactored the mod to allow creation of multiple body cams through an API. This feature is not used in OpenBodyCams, but may be used in the future to provide a separate body cam for the terminal, or other use cases.
- When no valid target is selected (i.e. when players are still respawning), the screen will now display nothing, but it will remain illuminated to indicate that it is still powered.
- Fixed an issue where the screen would get frozen with the green transition visible when a radar booster is left behind on a planet.
- Note: Due to refactoring much of the code that reacts to the events in the game, I'm anticipating there may be regressions in the way the body cam tracks its target's status changes (death, masking, respawning). If the camera gets stuck displaying something unexpected, please report that (preferably through a GitHub issue) with steps to reproduce the problem.

## Version 1.0.24
- Added support for [ModelReplacementAPI](https://github.com/BunyaPineTree/LethalCompany_ModelReplacementAPI)'s third person model replacements.
- Added support for LethalVRM's third person model replacements.

## Version 1.0.23
- Fixed the external camera staying frozen when looking at the screen by the door controls.

## Version 1.0.22
- Added handling of AdvancedCompany equipment to the compatibility feature, preventing equipped items from obstructing the body cam.

## Version 1.0.21
- Fixed all cameras in the ship staying enabled when looking away from the front of the ship with DisableInternalShipCamera off. The internal camera is disabled if its screen is not visible to any camera but its own, which occurs when it is not visible to the player.

## Version 1.0.20
- Fixed parsing of the emissive color option in locales that use `,` as their decimal separator.

## Version 1.0.19
- Added an option to adjust the emissive color of the body cam screen.
- Added an option for the panning speed for cameras when attached to a radar booster. When set to 0, the camera will be stationary and face in the direction the player was looking when placing the radar booster.
- Implemented an optional fix for dropped item rotation being desynced between the player dropping the item and other players. This will affect all items in the game. Without this fix, radar boosters will not face in the direction that they were dropped by the player that had been holding them.
- Allowed the camera to be disabled when the player being viewed enters the ship to avoid rendering the items on board twice.

## Version 1.0.18
- Fixed the radar map not disabling correctly when it was not visible, or while the player is outside the ship.

## Version 1.0.17
- Fixed body cams being unable to display on the GeneralImprovements large monitors with UseBetterMonitors enabled.

## Version 1.0.16
- Fixed a reduction in performance that began in version 1.0.14 with the fix for the DisableInternalShipCamera bug.

## Version 1.0.15
- Updated GeneralImprovements compatibility to work with version 1.1.1. This updated compatibility will not work with any previous versions of GeneralImprovements.

## Version 1.0.14
- Fixed a bug that caused error spam when MoreCompany compatibility was being used.
- Forced the radar map to keep rendering when interacting with the terminal while the DisableInternalShipCamera option is enabled.

## Version 1.0.13
- Supported the GeneralImprovements UseBetterMonitors option with a config option to select the monitor number to display the body cam on.

## Version 1.0.12
- Stopped placing the body cam on the monitor by the door.
- Reduced the emissivity of the bodycam monitor.

## Version 1.0.11
- Added an option to turn the body cam screen off, which can be used in-game if performance issues are encountered.

## Version 1.0.10
- Added support for AdvancedCompany cosmetics. They should now be hidden on the body cam when viewing other players.

## Version 1.0.9
- Made the bodycam monitor turn off when the radar map is powered down by the button or a lightning strike.

## Version 1.0.8
- Added an option to adjust the brightness of the night vision light. This can be used to counteract the green tint of the screen making it difficult to see when players are in dark areas.

## Version 1.0.7
- Fixed a compatibility issue with Immersive Visor which would cause the body cam to render to the screen instead of a texture when a player respawned.
- Prevented the radar map's night vision light from showing up on the body cam.

## Version 1.0.6
- Prevented MoreCompany cosmetics from appearing and obstructing the view when joining a game with the camera viewing someone with cosmetics equipped.
- Made the body cam attach to dead bodies on clients other than the one of the player that died.

## Version 1.0.5
- Fixed an compatibility issue with MirrorDecor that caused the first person arms to be invisible in the body cam.

## Version 1.0.4
- Fixed an issue where the local player's body would be invisible in the body cam with LethalEmotesAPI or MirrorDecor installed.

## Version 1.0.3
- Fixed an error that would occur when a third person emote was started by the LethalEmotesAPI mod.
- Added a config option to disable MoreCompany cosmetic support that can be used if compatibility issues arise.

## Version 1.0.2
- Fixed error spam that would occur if the camera was attached to an enemy or dead body being destroyed after a round ends.

## Version 1.0.1
- Fixed an error that would occur when MoreCompany is not installed.

## Version 1.0.0
- Initial release.
