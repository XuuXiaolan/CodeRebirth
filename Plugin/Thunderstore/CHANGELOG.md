# v0.9.3

- Fixed bug with seamine gal targetting inside enemies.
- Made craters in meteor shower disappear after 20 seconds to help performance.
- Bam bam bam ba rum da dum......
- Helped performance a bit when loading into orbit and despawning crates etc.
- Updated Readme.
- Made crates not spawn the same items if more than one is broken in a moon.
- Fixed 999 not working with company config.

## v0.9.2

- Fixed bug with gals not being activatable on company moon with navmeshcompany mod.
- Added a new unlockable dispenser, costs only 150, something from lockdown protocol, one of my favourite games recently.
- Messed with seamine gal range when outside for attacking.
- Changed interact message for scp 999 gal.
- Fixed scannode for hoverboard, bell crab and scp 999 gal.

## v0.9.1

- Rebuilt mod for latest game version.

## v0.9.0

- Added 15% chance event for wooden crate's to explode and damage the player.
- Gave bellcrab gal a dancing animation.
- Fixed 999 Gal not being interactable by players other than host.
- Fixed microwave scrap being on the bottom.
- Fixed microwave explosion not showing on client's end.
- Fixed microwave scrap always being grabbable despite microwave closed.
- Fixed "Vanilla" and "Modded" for hazard curves.
- Fixed microwave scannode not having a box collider.
- Gave 999 Gal and BellCrab Gal a ScanNode.
- Added config for bear traps, they can now pop the cruiser's tires, fuck you cruiser drivers, burn and die.

## v0.8.16

- Fixed lethal hands damage with crate.
- Forgot something.

## v0.8.15

- Fixed hazard stuff fully, assuming LethalLib updated before i put out this update, which i hope it did.
- Added new gal, SCP 999 Gal, with keyword LIZ-ZIE, this one is a very configurable healer type gal, have fun.
- Fixed dance animation for seamine gal.

## v0.8.14

- Forgot to check if company moon didnt have an interior.

## v0.8.13

- Forgot to include bellcrabgal assets.

## v0.8.12

- Fixed shockwave gal textures for crismas.
- Made some of wesley's interiors in toy store scan for hazards properly like the cake and stuff.
- Made spikerooftrap on hazard layer so seamine gal scans it.
- Added bellcrab gal (she's purely decor with some luck value added).
- Fixed bugs with seamine and shockwave hug and pat bug with doing it after directly getting out of charger.
- Added config for seamine gal only owner sees scan results.

## v0.8.11

- Fixed ACUnit sound continuously playing.

## v0.8.10

- Actually added the seamine and shockwave gal christmas textures.
- Fixed rare animation bug for seamine gal.
- Increased range seamine gal needs to stop to explode at an enemy.
- Added config for sounds of hazards.

## v0.8.9

- Added christmas texture for shockwave gal and seamine gal.

## v0.8.8

- Fixed icy hammer and made it have a 75% chance to slow.
- Fixed microwave scrap not following microwave for clients (maybe).
- Fan doesn't push you through railing now :<.
- Added redwood spawning with breaking trees sometimes.
- Fixed seamine gal dance anim.
- Changed weights of all the weapons to be less and gave them a small sound when grabbing dropping etc.
- Fixed hazard spawning for non host clients in debug mode.
- Fixed item spawning value for microwave scrap.
- Fixed wooden seed spawning (maybe).
- Got rid of lineofsight check for seamine gal's explosion.
- Fixed bug with gals where they'd disappear when lobby was reloaded.

## v0.8.7

- Fixed redwood explosion.

## v0.8.6

- Fixed position desync of laser turret.

## v0.8.5

- Buffed redwood giant's kick and jump actions.
- Laser turret should perform a tiny bit better.
- Crates should shoot up the same as for host and client now.
- Completely fixed parenting and position issues with latejoin clients and other clients with the gals and their chargers.
- Improved redwood giant search routine for clients.

## v0.8.4

- Made the seamine gal scanner a LOT better, you can put down the pitch forks now.
- Tried to fix automatic issues with gal.

## v0.8.3

- Fixed very rare config issue with the hazard curve stuff with non english systems(?) and added more error logging.
- Readded config (true by default) to remove interior fog.

## v0.8.2

- Fixed gal being outside in orbit for late joining clients.
- Fixed pathing issues with seamine gal if enemy is in an unreachable position.
- Check the readme for how the hazards config works!!!

## v0.8.1

- Fixed harmless error with MRAPI and CodeRebirth when MRAPI isn't found.
- Fixed harmless error with gal's chargers when the owner dies.

## v0.8.0

- Added seamine gal.
- Added seamine gal as a player suit.
- Added configs for enabling/disabling seamine and shockwave gal player models.
- Fixed meteor shower initial volume stuff.
- Microwave has a chance to spawn scrap inside of it now, making it more deadly.
- Nerfed meteor shower fire damage a bit and reduced the particles to save on some frames.
- Lowered the default speed of the meteor shower meteor's from 50 to 30.
- Made meteor shower automatically end at 80% through the day by default and start only after the ship has nearly landed.
- Made meteor shower strike on ship by default false because I got scolded, and also improved the non-hitting of the ship to be a decent radius around the ship.
- Made meteor shower scrap amount multiplier and scrap value multiplier 1.2x by default and lowered spawn weight from 50 to 30 (and tornado from 50 to 40).
- Added a special death interaction for Pjonk, die often for the sake of us (this is a config disabled by default because most people aren't Pjonk).
- Added a config in general category for the gal ai owner being the only person able to disable her.
- Probably fixed tornado type desync.
- Maybe fixed position desync for late-join clients (clients that joined AFTER the gal was bought).
- Gave All inside hazards an animation curve in the config stuff, have fun.
- Gave industrial fan and laser turret config to not destroy body.

## v0.7.18

- Removed readjustment of camera with the shockwave gal model cuz that seems like a bad idea in hindsight.
- Also credited rodrigo with a funny video.

## v0.7.17

- Fixed bug with wooden crate where it would error/spawn normal items despite config not allowing it.
- Allowed you to be teleported away/use entrance teleports while in a bear trap to escape.
- Gave bear traps a more advanced config.
- Fixes to do with functional microwave.
- Fixes to do with laser turret.
- Fixes to do with wooden seeds.
- Added config for wooden seed spawn chance from tree.
- Shockwave gal fix with being fired, again.

## v0.7.16

- Metal crates no longer abusable.
- Fixed exploit with metal crates.
- Added safeguards to wooden crates with desynced shops.
- Maybe fixed bug where you would sometimes get flashed by flash camera for a long time.
- Fan doesn't push/pull through doors now.
- Laser turret doesnt shoot through doors too.

## v0.7.15

- Forgot to get rid of dependency.

## V0.7.14

- Added to README about the model replacement feature.
- Added a first time message saying what mods you can enable.
- Added compatibility with shockwave gal with openbodycams camera.

## v0.7.13

- Added a new thunderstore dependency, keep in mind it is OPTIONAL, you can get rid of it if you dont like it!

## v0.7.12

- Added config for safe item value multiplier, default is 1.4f value;
- Turned all flora into static shadow casters, should help with performance a bit.
- Reduced fan push force into 3 from 4 to help with not clipping through walls.
- Beartraps are hopefully more synced up.
- Redwood giant no longer lingers forcefully around the ship.
- Fixed problems with metal crate.
- I left a present for if a player enters a metal crate.
- Fixed problems with hitting wooden crate part 2 electric bogaloo.
- Fixed ACU being an explosive mess.
- Added whitelist option for wooden crates, auto generates if blacklist field is empty and whitelist is toggled on.

## v0.7.11

- Fixed problems with hitting wooden crate.
- Tried to fix problems with gal's selling features.
- Fixed problem with gal erroring when employee gets fired.

## v0.7.10

- Fixed bug with gal for clients.
- If crates are pulled up with fists, the player gets damaged a little bit.

## v0.7.9

- Fixed metal crate for clients.
- Slightly updated metal crate textures.
- Changed how I handle pathing for the gal and redwood giant, so let me know how those feel.
- Disabled Cutiefly, SnailCat and Biomes in teh config by default.

## v0.7.8

- Fixed error where mod wouldn't load (whoopsie).

## v0.7.7

- Fixed really strong industrial fans.
- Added metal safes, removed metal crates, changed how metal safes work from metal crates, same with wooden crates, changed how they work.
  - Basically swapped features of metal crate and wooden crate.

## v0.7.6

- Fixed endless growing of the plants by abusing going to company moon.
- Reverted the halloween fog changes because zeekerss made it less common.
- Added gal compatibility with openbodycams.
- Cleaned up the gal's hand triggers, they won't show unless she's activate and you're holding an item.
- Added appropriate screenshakes.
- Polished tesla shock to be a stronger hit that has a longer cooldown.
- Added a config to enable bear traps inside the interior.
- Industrial fan had frame rate issues.
- Fixed flash turret not working for clients.
- Gave ACUnit bullet a trail that explodes you less far.
- Fixed readme with bear trap and flash turret mix-up.

## v0.7.5

- Made shockwave gal resync with clients on lobby reload/late joiners.
- Fixed teslashock shocking everyone lol.

## v0.7.4

- Improving ACU as best as I can, doesn't target you if you're actively on the ground or have something blocking LoS.
- Improved BearTrap spawning.
- Decreased IndustrialFan power by 25%.

## v0.7.3

- Added a debug config to disable halloween fog for my own testing and whoever else wants.
- Reworked config names and values for hazards to be less and gave a better description for them.

## v0.7.2

- Gave ACU a spawn weight config based on moon + value.
- Gave microwave bigger collider to open doors.
- Gave all hazards lore in the readme.

## v0.7.1

- Fixed potential bug with ACUnit where it wouldnt despawn on moon unload.
- Fixed animator bug with bear trap.
- Fixed problem with industrial fan not spawning red mist.
- Fixed Tesla Shock targetting the first person through walls.
- Fixed Laser Turret not rotating for clients.
- Stopped ACU while ship is leaving.
- Potentially fixed flash camera not working on other clients.
- Nerfed ACU a tiny bit.
- Fixed bug with gal not picking up items properly or not able to be activated by clients.

## v0.7.0

- Added 7 new hazards.
  - TeslaShock.
    - Damages nearby players with metallic items, chains enemies and players alike.
  - Functional Microwave.
    - Wanders the facility... very slowly... while cooking it up.
  - Air Control Unit.
    - Screw jetpack users.
  - Laser Turret.
    - With gems stolen from Henry Stickman, some mad scientist created a big ass turret that went wild.
  - Bear Traps.
    - Usually is for bears.
  - Flash Turret.
    - Say cheese!
  - Industrial Fan.
    - Back when employees used to work overtime in the hot summers.
- Fixed bug with getting fired.
- Made it so you can drop gal's items while she's delivering them.
- Cleaned up triggers with gal so prompt wont pop up unless you fulfill conditions.
- Gave gal a radar dot so you can look at her in the terminal's radar, goes by Delilah.
  - Works with the OpenBodyCams mod ~~not really the POV is from her feet~~.
- Made it so gal doesnt jitter for clients on ship land/takeoff.
- Gal probably doesnt jitter in the elevator anymore.
- Fixed several bugs with gal killing enemies.
- Fixed gal dropping off items onto ship, they now stay there with the ship, notif doesnt always show up though, so gal has her own notif now.
- Massively improved gal selling mechanics.

## v0.6.8

- Forgot to make metal crate whitelist into a blacklist.

## v0.6.7

- Trying the 3rd time to fix the propeller sounds.
- Fixed gal staring at player causing weird rotation.
- Fixed her following enemy too much before attacking.
- Changed some texts around.

## v0.6.6

- Fixed some propeller volume stuff.
- Fixed hand interaction.
- Fixed some movement pathing issues.
- Will do a more detailed fix update when i'm more available (weekend).

## v0.6.5

- Updated the looks of the wooden crate.
- Fixed some misc bugs with shockwave gal.
  - Laser not doing as intended for clients.
  - Triggers not disappearing when you dont meet conditions to use em.
  - etc
- Fixed a bug involving automatic config with the gal.

## v0.6.4

- Added propeller volume config lol.
- Auto start on ship start config.
- Fixed the shovels spawning wooden seeds randomly.

## v0.6.3

- Fixed parenting issue with shockwave gal.
- Turn crate configs into whitelists from blacklists.
- Stopped gal from targetting enemies while you're inside and its outside or vice versa.
- Give tomatoes an inventory icon.
- Give seeds an inventory icon.
- Config for propeller volume for shockwavegal.
- Allow redwood to eat the driftwood giant.
- Give config for seed spawn weights.
- Fix meteor spam for clients.
- Better pathing when coming to ship with items.
- Fixed constant error for clients on meteor shower.
- Fixed some rare config error with tornado smoky version.

## v0.6.2

- Adjusted redwood spawn count and meteor volumes config.
- Made gal drop items on attack mode switch.
- Made item dropped register for ship.
- Allowed gal to follow player through the elevator by herself.
- Improved triggers for shockwave gal.
- Remove physics when warping or heading to elevator for shockwave gal.
- Added snare flea to shockwave gal config.
- Added bozo rodrigo to readme credits.
- Fixed sound ranges with plant monster.
- Fixed item holding rotation with shockwave gal.
- Added scannode to shockwave gal.
- Items stick onto the ship when dropped by shockwave gal (while she dropped onto the ship).
- Fixed multiplayer bug.
- Shockwave gal has a new behaviour when activated on company, selling up to quota.
- Added shockwave gal config for always being able to hold 4 items regardless of multiplayer or singleplayer (false by default).

## v0.6.1

- Gave some sounds to the carnivorous plant.
- Gave redwood kick some sounds too.
- Fixed some redwood sounds playing only on host.

## v0.6.0

- Biomes disabled by default.
- Added carnivorous plant.
- Added reworked redwood titan.
- Added shockwave gal.
- Added farming.
  - Added new decor, "plant pot".
  - Added wooden seed, obtained by chopping wood and getting it randomly.
  - Added tomato and golden tomato, sellable to the company.
- Improved meteor shower visuals drastically (thanks V0xx).
- Improved biome particles performance.
- Updated for v64.
- Improved hoverboard a bit.
- Improved meteor shower overhead visuals a bit.
- Fixed Snailcat not being rideable.
- Fixed Cutiefly not being rideable.
- New icon for coderebirth!! thanks to Koda.
- Fixed wooden crates having health similar to metal crates.
- Fixed scan nodes with wooden and metal crates.
- Fixed metal crates opening randomly by themselves.
- Allowed wallets to get smaller if their value somehow decreases.
- Made flower spawning a bit more spread-y.
- Added metal crate config for whitelist if need be.
- Added wooden crate config for whitelist if need be.
- Gave each item a "min,max" worthiness config.
- Added config for meteor shower to stop at a normalised time of day.
- Added some more item crate configs.
- Added change config for biomes.

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
