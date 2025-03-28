# Changelog

## 1.4.6
- Added a GetSkin API call.

## 1.4.5
- Added back in patchers that got removed getting this project back on github. Should fix some audio issues.

## 1.4.4
- Made some API methods static so modders could actually get at it. Whoops.

## 1.4.3
- Added Maneater key and events (kinda. he didn't need any)
- Added a few API calls to make it easier for modders to interact with skins
    - ReassignSkin will let modders manually assign a skin to a spawned enemy. Chaos.
    - RemoveSkinner has been reworked to actually call the Remove method of the assigned skinner
    - GetSkinId will let modders keep tabs on what skin is assigned to what enemy
    - GetEnemyId will let modders get the SkinRegistry's enemy type of an enemy with a skin assigned to it. This is necessary if you need to know if a skin will be compatible at runtime.
- Removed a handful of deprecated methods no one was using. No one was using them, right?

## 1.4.2
- Added keys and event handlers (just audio events) for circuit bees, manicoils, and roaming locusts.
- Added event for Mask Hornet spawn
- Fixed overwites not working
- Changed "Add Moon..." text in config menu to "Add Moon/Tag..."

## 1.4.1
- Changed tag handling to average all applicable tags instead of picking one applicable tag at random
- Fixed LLL tags to pull from the correct field

## 1.4.0
- Removed support for 1.2.0 bepinconfig profiles (if you used 1.3 at all it should've already carried over)
- Added tag-based skin spawn configurations. Now before going to the default map config, if a config with an applicable tag exists, it will choose that config instead. Tags from LLL moons are automagically registered.

## 1.3.8
- Fixed a small bug in the deserialization of config messages sent when syncing
- Made skin configuration menu viewable and readonly on synced clients
- Moved sync messaging patch to player class
- Removed pointless bookkeeping and supporting pathes that were probably causing issues with LLL
- Added a couple more log statements (as a treat)
- Removed a bunch of unused using statements
- Got rid of some dead code in the sync profile method

## 1.3.7
- Actually fixed skin sync messages not getting sent at the correct times (crowd boos)

## 1.3.6
- Fixed issue where GUI was not updating when a default config was being applied

## 1.3.5
- Fixed issue where skins with a default config were inactive by default
- Sync messages are now deployed when config settings are changed

## 1.3.4
- Percentages are back by purpular request. These now Display the actual spawn percentages rather than the percent of their available weight. A little more useful.
- Fixed issue where profiles weren't appearing in the dropdown after being created
- Fixed issue with default skin profiles failing to apply
- Fixed issue where new skins were always having their default frequency set to 1 even if their default config said otherwise

## 1.3.3
- Added note on how to handle LLL soft dependency causing errors in the editor

## 1.3.2
- Actually removed the debug statements (crowd boos) 

## 1.3.1
- Fixed old birds messing with the config synchronization
- Fixed stale active skins hanging around in config profiles

## 1.3.0

- Added profile storage. Profiles can now be stored locally and loaded in the same way default moon and skin configs are.
- Added client-host syncing
- Tweaked random number generation
- Added separate frequencies for indoor outdoor spawning
- LLL moons are now automagically registered
- Profiles are no longer stored in bepin config file
- Added events and vanilla entries for Tulip Snake, Kidnapper Fox, and Barber enemies

## 1.2.0

- Added default configurations for Moons
- Added default configurations for Skins
- Added controls in the gui for reapplying default configurations
- Removed the 0 - 100 frequency counts in the ui
- Added ids for v50 enemies and moons
- fixed skin icons not displaying
- Added several new enemy events primarily to allow for more comprehensive modded sounds.

## 1.1.0

- Added Events for ghost girl
- Added Changelog

## 1.0.1

- Added images to ReadMe

## 1.0.0

- Initial release