# v0.6.0

- Made biomes more dangerous.
- Added carnivorous plant.
- Added reworked redwood titan.
- Added shockwave gal.
- Added farming.
  - Added new decor, "plant pot".
  - Added wooden seed, obtained by chopping wood and getting it randomly.
  - Added tomato and golden tomato, sellable to the company.
- Added seamine tink.
- Improved meteor shower visuals.
- Improved biome particles performance.
- Updated for v64.
- Improved hoverboard a bit.
- Improved meteor shower overhead visuals a bit.
- Fixed Snailcat not being rideable.
- Fixed Cutiefly not being rideable.
- New icon for coderebirth!! thanks to [Koda](https://www.youtube.com/@Sp71ng).
- Fixed wooden crates having health similar to metal crates.

## v0.5.2

- Removed redwood from readme for now.
- Fixed potential meteor shower error.
- Fixed Coin radar not disappearing on
- Added coin min and max config.

## v0.5.1

- Fixed biomes not despawning on round leave.

## v0.5.0

- Fixed Sapphire gem colliders.
- Fixed Sapphire gem tranparency.
- Fixed nature's mace stuff not doing its thing right with healing players, probably.
- Adjusted Icy Hammer to only slow 100% on crit and only deal 1 damage.
- Fixed floating flora (hopefully).
- Fixed money radar icon not disappearing when picked up.
- Added biomes (currently do nothing).
- Improved some patches.
- Changed the weathers to have a config to not do the powerlevel shenanigans, but currently tornado and meteor shower increase inside power by 6 and decrease outside and daytime by 3.
- Flora doesn't spawn on catwalks anymore.
- Gave hoverboard some weight when in held mode.
- Gave hoverboard collisions for moving it around while not held by you or a player.
- Hoverboard battery works now when holding shift for a boost.
- Adjusted snowglobe holding animation, no more bugs wooo.
- Epic axe can chop trees now.
- Added emerald meteorites... for real now, i might've lied last time I said that.
- Accidently adjusted flora, if you see the wrong flora in the wrong biome let me know.
- Added ruby gem to meteorite.
- Updated default config.
- Updated readme images and presentation.
- Fixed multi-hit bug.
- Uploaded to fix tornados not working in v62.

## v0.4.2

- Sigh, fixed the tornado stuff for the guy who wants it to spawn naturally as an enemy (WONT WORK IF THE WEATHER IS WIMDY ALREADY).
- Fixed plant spawning not on navmesh (this is custom moons being bad and adding colliders where there shouldn't be, so wasnt even my fault smh).
- uhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh.

## v0.4.1

- Spiky Mace by default deals 2 damage now.
- Icy Hammer slows down enemies and players hit by it temporarily.
- Nature's Mace heals enemies by 1 hit point and heals players.
- Improved readme.
- Added a blacklist moon config for flora (doesn't except placeholders, only moon names).
- Made LLL and WeatherRegistry a hard dependency.
- Registered Tornado as an enemy with 0 spawnweights for that one guy.
- Added I think like 70 more flower types?

## v0.4.0

- Fixed flora not spawning on some custom moons.
- Metal crate changed to drop non-scrap equipment.
- Added 18 new flora types

## v0.3.3

- Flora can now be sorted into 3 different types.

## v0.3.2

- Github is now public and contributions/talent/reports/more ideas are appreciated.
- Configs now accept both custom and modded (custom is more recommended just incase).

## v0.3.1

- Halved the strength of the pull of the tornado by default.
- Added 20 more plants.
- Added a metal item crate.
- Fixed soft dependencies not working properly.
- Fixed flower config breakages.
- Added more flower configs.

## v0.3.0

- Add slider config for tornado strength.
- Added yeet SFX in config for tornado (false by default).
- Randomised a bit more the strength of the tornado throwing and at what point you'd be thrown.
- Added a bunch of flora.
- Decreased overall assetbundle size from 32mb~ to 18mb~.

## v0.2.6

- Lowered tornado power.
- Fixed scannodes for the new weapons.
- Lowered default config for meteor shower lol.
- Fixed tornado throwing you.

## v0.2.5

- Should fix tornado kinematic patch not working.
- Turned off some spammy logs.
- Tornado should stray from ship if spawned next to it.
- Fixed some hoverboard sound with footsteps not playing.

## v0.2.4

- Fixed tornado config properly.
- Added tornado volume in ship config.
- Added cutiefly flap sound volume config.
- Fixed Snailcat and Cutiefly flying routes.
- Improved tornado looks.
- Windy tornado can now throw the player.
- Added compatibility with Surfaced.
- Fixed hoverboard.
- Buffed hoverboard.
- Added compatibility with subtitles API (may not work for some reason).
- Added 3 new weapons.
- Added 2 new gems in the meteor shower and made them more common.
- Water tornado now drowns you.
- Made weather registry a soft compatibility (required to install but can disable).
- Hopefully Sync'd some issues with particles when near a tornado.
- Fixed tornado math pulling you when you're further away.
- Blood tornado either heals or damages you now randomly.
- Fire tornado would get weaker when you're on 20 or lower hp.
- Electric tornado gives a bigger speed boost.
- Balanced and made tornados weaker overall.
- Made tornados much faster and properly move around.
- Tornado weather now decreases total outside power level by 3 and inside by 3.
- Meteor shower weather now decreases total outside power level by 3 and inside by 3.
- Made enemies rideable.

## v0.2.2

- Fixed Snowglobe animations.

## v0.2.1

- Fixed company not loading bug.
- Fixed tornado config -ish.

## v0.2.0

- Fixed tornado not spawning, may need to recreate config.

## v0.1.9

- Fixed being unable to ride truck during tornado.
- Potentially fixed hoverboard slow issues.

## v0.1.8

- Accidently included something not supposed to be there, caused an error.
- Potentially fixed item crate error.

## v0.1.7

- Improved hoverboard collisions, I think.
- Increased range for tornado effects.
- Tornados will get a bigger update when I'm more free later I promise.
- Added config to switch wallet between an actual item to the refactored version.
- Tried to fix bug where only host could pick up the wallet in the new system.
- Updated version of the mod to play nice in latest version of the game.

## v0.1.6

- Buffed item crate by atleast 3 times.
- Added config to disable snow globe music (client side).
- Attempted to fix wallet positioning.
- Stopped you from being able to pick up multiple wallets.
- Improved tooltips for wallet.
- Improved tooltips for hoverboard.
- Nerfed Tornado power again.

## v0.1.5

- Fixed particle effects not working.
- Fixed unlimited range on tornados.
- Fixed tornado audio disappearing when entering and leaving interior.
- Reduced tornado spawning near ship.

## v0.1.4

- Improved electric, water and potentially other tornado types and gave unique mechanics and changes.
- Potentially fixed snowglobe not working for everyone's animator.
- Potentially fixed item crate desyncs and exploit that didn't require digging it up.
- Added sounds to tornados (provided by Moroxide).
- Added particle effects to players near tornados (will improve in the future).

## v0.1.3

- Fixed crater textures being weird.
- Improved tornado particles.
- Added subtypes for the tornado that don't currently do anything differently special.
- Removed dependency on custom story logs temporarily until i figure out soft dependencies.
- Added config for subtypes.

## v0.1.2

- Fixed Snow Globe not having a value.
- Allowed you to use ladders and special animations while tornado is around.
- Reverted funny visuals with meteor craters.
- Fixed item crate sound being global.
- Made blood tornado do some weird stuff with damaging and healing player.
- Potentially fixed a wallet bug.

## v0.1.1

- Fixed wallet dropping while using terminal.
- Fixed item crates not disappearing after opening.
- Updated item crate scannode.
- Added compatibility warning with piggy's variety mod (their animator overrides every mod's so it ruins the snow globe).
- Heavily nerfed how strong the windy weather is.
- Fixed hoverboard dragging itself to a specific position sometimes.
- Allowed cover to exist for windy weather
- windy and meteors can now be disabled in config

## v0.1.0

- Added new item, SnowGlobe.
- Added configs for SnowGlobe.
- Added a loooooot of configs.
- Improved Weather performance
- Added warning for CentralConfig being in the same pack.
- Added new outside object that can spawn in sometimes.
- Removed collision from snailcat.
- Improved meteor spawning location.
- Added new shop item, hoverboard.
- Added config and chance for critical hit from code rebirth weapons.
- Added tornado.
- Updated readme.
- Updated all icons.
- Updated meteorite Crystals.
- Improved how wallet works, it's a player upgrade now.

## v0.0.3

- Added moon blacklist config for the weather.
- Added Volume config for the weather.
- Lowered Coin spawnage from just 10, to 0 to 10
- Made Epic Axe bigger.
- Fixed spawnweight configs not applying.
- Fixed MeteorShower spawning on company.
- Made a google forms for issues and idea applications into the readme.
- Improved crater fire.
- Got rid of crater and replaced it with a better texture on the ground.
- Fixed issues with desync'd items.
- Improved a lot on meteors by increasing size, messing with the particle system, please report any fps drops.

## v0.0.2

- Fixed weather not turning off when heading to main menu/other reasons.
- Fixed wallet not working.
- Fixed coin tooltip not showing.

## v0.0.1

- Initial release
