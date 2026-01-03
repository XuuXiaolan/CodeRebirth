# v1.5.0

- Added compat with Lategame Upgrades and FasterItemDropship on Oxyde.
- Added compat with Lategame Upgrades and Oxyde's sell percentage.
- Fixed mistress plushie.
- Gave Crane a bigger de-activation range from 20 to 40 to 60 to 90.
- Crane insta falls down on de-activation and stays fallen until it re-activates.
- Cactus budling rolling sound now doesn't play desynced if it was previously rooted before rolling.
- Mistress now targets players more randomly than before, but still implements some priorities.
- Crane configured so that it doesn't spawn on moons with less than 30 AI nodes.
- Crane given an object width of 15 units so it will not spawn within some distance to stuff like entrances or the ship itself.
- Beartraps were given a very minimal ScanNode.
- Sarah and Toby can no longer be configured and force enabled when oxyde is enabled.
- Some nonsensical configs turned off like inside merchant, mimic crates, etc.
- Inside bear traps disabled by default.
- Gave tags to all CR content.
- Fixed magician interacting with cruiser while held onto player.
- Gal unlockables now shuffle if you already built that unlockable.
- Merchant no longer spawns unlockables that you already built.
- Fixed client and server side issues with Magician.
- Fixed magician particle destroying itself after one use.
- Fixed magician not being able to kill outside.
- Made it so you can move further from mistress before getting beheaded and the blackout effect be slower.
- Fan pushing is more linear with distance.
- Puppet movement is more local to the player so kicking the puppets away should feel a tiny bit more responsive for the local client's puppet.
- Trashcans auto position themselves to dead end tiles.
- Nerfed driftwood scream from 5 damage to 2 damage, nerfed distance to getting damaged by it to 10 units.
- Made non-important unlockables decors.
- Made a config for instrument volume.
- ManorLord puppet speeds up if you're further from it.
- Increased redwood threshold for chasing animation.
- Added visual tells and audio tells for the mimic crates.
- Improved echo effect of the walkie yellie.
- Fixed driftwood not grabbing clients properly.
- Fixed hoverboard not working for clients (DawnLib update that was needed due to unity having a bug in it).
- Made explosions caused by coderebirth do stuff like burning forest giant, etc.
- Peacekeepers don't damage eachother anymore.
- Fixed any issues with mistress head not following the player while being held or any weird visual clarity.
- Cleaned up mistress' AI a bit more to make it a bit less jank.
- Monarch hp adjusted from 10 to 8.
- Fixed all plushy scrap values from being backwards, 32 -> 64 into 64 -> 32 (it forced them all to be the same price each time, 32).
- Removed Xui long voiceline, sad day for me :(.
- Xui value adjusted from 0 to be same as kingrigo.
- Expanded on cactus budling weights a bit.
- Adjusted Cacti weights to add desert.
- Made cacti spawning on you from cactus budling slower emerging when you're inside the facility.
- Adjusted driftwood's spawn curve to be higher the later the day.
- Adjusted driftwood and removed chest bang after losing aggro of the player or enemy.
- Added a limit for monarch to spawn more cutieflies, no more cutieflies past 15.
- After around 7pm, cutieflies fly up to the sky until they eventually despawn 30 seconds later.

## v1.4.2

- Fixed wallet and halloween skins not disappearing after november 10th.

## v1.4.1

- Duck configs.
- Boomtrap nerfs.
- Monarch curve fix.
- Crane collisions turn off while taking off and leaving ship
- Increased crane despawn range.
- Apparently fan is still bugged so I added a distance check for it working on you.
- Next update will have some crane stuff related to where they can spawn and despawning based on how big the map size is atleast.

## v1.4.0

- Fixed a myriad of bugs.

## v1.3.1

- Fixed Redwood cuffs.
- Fixed items not spawning when killing enemies etc.

## v1.3.0

- Halloween update
- (Almost) all enemies have a halloween replacement
- Coins got up to 9 different skins along with a wallet reskin.
- PeaceKeeper AI was also tinkered for the new update along with its new skin.
- SnailCat became greedy.
- Oxyde got a halloween overhaul too.

## v1.2.5

- Cleaned up forgotten things in the shipyard.
- Fixed enemy max values and power levels.

## v1.2.4

- Fixed an exception caused by disabling oxyde and night shift.

## v1.2.3

- Fixed an issue with the gal builder.
- Fixed what node gets loaded when gals are denied being bought.

## v1.2.2

- Gave all the plushies small weights on moons and sometimes interiors.

## v1.2.1

- I couldn't escape a patch huh, removed LateGameUpgrades as a soft dependency because it caused a cyclic dependency loop with a combination of like 6 other mods.

## v1.2.0

- Other than bug fixes, this is one of the final content updates this mod will likely receive, I hope you've enjoyed the CodeRebirth experience provided to you by the main developers, Rodrigo and Xu Xiaolan.
- Fixed SnailCat names being desynced apparently, host now controls all the names.
- Added all the plushies.
- Added Achievements.
- Fixed some ACUnit and Gunslinger Greg targetting issues.
- Rewrote how some stuff targets entities so driftwood etc shouldnt see any more false positives because of other mods being stupid.
- Fixed crane not killing clients sometimes.
- Fixed Mistress visual issues on dying to something else while she's locking you in place.
- Added MoreCounterplay Compatibility when seamine gal kills the coilhead.
- Added LethalMoonUnlocks Compatibility to Oxyde.
- Fixed MarrowSplitter recharging from hitting non enemy and/or non player.
- Made the crate drops more randomised on a moon.
- Fixed issues with fans pulling through walls.
- Oxyde crane drops ship more slowly to try and mitigate player fall damage.
- Greg and ACUnit should target the players less maybe.
- Greg shouldn't target players on the ship edges on Oxyde anymore.
- Added Configs for cacti toughness against non-scrap and a volume config for terminal gal's special song.
- Made crane deactivate timer a configurable range timer between two numbers.
- Fixed guardsman projectiles not despawning + performance increase with that stuff.
- Monarch ground attack can now be dodged by dodging midair.
- Monarch won't go for air attacks while the target player is inside the ship.
- Fixed a bunch of enemies being able to enter and leave the interior when they're not supposed to.
- Tornado met bruce and got a slight visual revamp.
- Auto routing to company on a full party wipe of oxyde.
- Fixed Incompatibility with GI's door stuff with keys.
- Fixed RadMech destroy sound not being playable from Greg.
- Fixed issue with brrreaker leaving player stuck if they die while stuck on a wall.
- Regenerated all configs for new config system :p
- Made SnailCats non-conductive.

## v1.1.11

- Fixed Scrap-E's material and model being invisible lol.

## v1.1.10

- Rereleased just incase of something.

## v1.1.9

- Fixed a desync where peacekeeper might not hurt clients.
- Switched to the new pathfinding system.
- Fixed edge case enemies not being killed by crane.
- Fixed Driftwood not killing small enemies and stuttering around them.
- Redid crate config, let me know of any issues.
- Fixed a potential issue with flora invalid config.
- Switched to a new pathfinding thingiemajig, let me know if any problems arise (or you crash, but hopefully you don't).

## v1.1.8

- Fixed issues with enemies getting stuck in pipes on oxyde.
- Fixed Driftwood issues with targetting other enemies and stutter moving and letting go of the player randomly.
- Potentially fixed issues with scrap-e getting stuck in places.
- Added extra text to the oxyde tablet.
- Addressed issues with redwood titan attacking new ForestGiantAI enemies that are bigger, because why? idk.
- Fixed Crane inconsistencies with killing players.
- Fixed Crane slight delay between clients and host resulting in clients dying on the hosts end.
- Finally fixed the sapsucker omelette holding.
- Potentially fixed an issue with oxyde enemies all bugging out randomly on first quota only???

## v1.1.7

- Guardsman no longer targets earth leviathan.
- Fixed dropping rotation of Brrreaker.
- Guardsman blacklist not working for cutiefly fixed?.
- MeteorShower meteors hitting the top meteor fixed.
- Extended the monarch's bite hitbox a bit up.
- Gave the monarch beam more outward range.
- Disable collision on enemy deaths like monarch, peacekeeper, driftwood, redwood, etc.
- Fixed cruiser not dealing damage to cacti.
- Cacti go back to the ground after 30 to 45 seconds (if spawned in).
- Fixed a despawning issue with cacti.
- Fixed hoverboard.
- Got rid of GhostGirl from oxyde.
- Buffed monarch laser up a bit.
- Made training tape slightly shorter.

## v1.1.6

- Some sort of incompatibility with fairai and cutieflys/monarchs so watch out.
- Added some null checks tocutiefly.

## v1.1.5

- Fixed the mod, blame rodrigo.

## v1.1.4

- Renamed wallet again cuz name sucked.
- Fixed issues with Sapsucker Omelette.

## v1.1.3

- Renamed wallet to avoid problems with bestiary.

## v1.1.2

- Added a new item to the microwave charred list.
- Added a greg spawn weight config.
- Adjusted a bunch of enemy weights on dine to be a lot less due to vanilla weights being less.
- Fixed wallet not working with merchant due to rename.
- Made it so that other weathers can take over other than nightshift for oxyde (requires a config option to be specifically turned off and other weathers set to oxyde to spawn).
- Fixed some stuff in relating to vehicle spawning.
  - Thanks to Scandal for the help in deciphering some cruiser code!

## v1.1.1

- Renamed Wallet to something else for compatibility reasons.
- Fixed host with oxyde crane animation not working properly.
- Fixed Oxyde config in terms of not unlocking.
- Fixed spawn weight configs on DawnLib's end.
- Fixed snailcat not being able to be brought into orbit.
- Fixed Xui desync issues in latejoin between players.
- Edited some scannodes and some Oxyde layout stuff.

## v1.1.0

- Misc changes to mistress.
  - Uncrouch crouched player.
  - Mess with head rotations.
  - Drop entire inventory on grab.
  - Make head players unable to pick up anything.
  - Rewrite some of the LOS stuff.
- Probably fixed SnailCat name desync between players. (might've made it worse, not sure, i realised too late the desync comes from late joiners lol)

## v1.0.2

- Fixed the 4 main gals' colliders not properly disabling with the disable interact.
- Added config for snailcat names editing.
- Bigger range for crane to deactivate and added smaller range for crane to despawn.
- Config added for Oxyde to always be enabled.
- Fixed Oxyde name to not have the dash in it.
- Maybe fixed some snailcat hitting errors.

## v1.0.1

- Fixed duck not aggroing on another player.
- Fixed Driftwood not moving because it would target itself.
- Fixed some scannode inconsistencies with vanilla.
- Decreased Crane hazard defaults.
- Decreased other hazards defaults.
- Made tornado a bit weaker, it's a bit overwhelming rn even if you arent super close.
- Fixed a rare monarch error.
- Crane deactivates fully if its too close to the ship on spawn.
- Maybe fixed flash turrets not working, someone reported they dont work, they work in dev version so uh maybe its fixed now.

## v1.0.0

- Lots of stuff lol.
- Dozens of new items with special effects added.
- Multiple enemies reworked and rewritten.
- I forced a lot of your configs to reset to true for the first time to experience the new content.
- Multiple new enemies added.
- Fixed redwood titan.
- Got rid of model replacement suits per demand.

<details>
  <summary> Spoilers!!!</summary>

- Moons
  - Oxyde

- Enemies
  - Cactus Budling.
  - Guardsman.
  - Mistress.
  - Monarch.
  - Redid cutiefly.
  - Nancy.
  - Peacekeeper.
  - Rabbit Magician.
  - Snailcat redid.

- Items
  - Marrow Splitter.
  - Credit Pad 100 500 and 1000.
  - Fog Horn.
  - Guardsman Phone.
  - Infinikey.
  - Lifeform Analyser.
  - Mole Digger.
  - Nitroglycerin Crate.
  - Oxidizer.
  - Rail Slugger.
  - Remote Detonator.
  - Rocky.
  - Ship Upgrade Unlocker.
  - Timestop watch.
  - Walkie Yellie.
  - Reverted wallet to only be the held version.
  - Melanie's drawing.
  - 6 new meteorite crystals.
  - Talking head.
  - Haemoglobin Tablet.
  - Oxyde Tablet.
  - Mountaineer.
  - 32 Lore documents.
  - Ceasefire.
  - Swatter.
  - Tomahop.
  - Turbulence.
  - Xui and KingRigo plushies.

- Hazards
  - Autonomous Crane.
  - Gunslinger greg.
  - Merchant.
  - Naturally spawning cactus.
  - Oxyde's crashing ship.

- Weathers
  - NightShift (Oxyde Only)

- Store Unlockables.
  - Piggy bank.
  
- Misc
  - New lore accurate manor lord death animation/effect.

</details>

<details>
  <summary>Older Versions</summary>

## v0.15.3

- Disabled the broken extra weathers, had them added by accident.
- Also see that, this was a thing I did by accident that would crash your game, and fixing it took a couple hours because I have a lot to work with, and I'm also running on a fever right now, I'm not gonna say what mod, but I'm not just silently crashing your game randomly and not fixing it immediately.

## v0.15.2

- Another one, fix for seamine gal and shockwave gal bloowing up and causing errors.
- Manor Lord also reflects damage proportional to how much damage you deal to it (so you're dead if you shoot it while its in its reflect state).
- Extra checks in manor lord to make sure he doesnt bug out.

## v0.15.1

- Well this was bound to happen.
- Fixed bear trap spawning to stop... spawning on walls???

## v0.15.0

- Added BoomTrap
- Improved readme yet again.
- Optimised the following additions a bit.
  - Janitor.
  - ACUnit.
  - Biomes.
  - Hazards.
  - Seamine Gal.
  - Shockwave Gal.
  - Terminal Gal.
  - Explosions in CR.
  - Spawning of redwood titan and seed through CR weapons.
  - Anything that plays a sound a dog or other enemies can hears.
  - A lot more lol.
- Fixed cruiser gal carrying you causing you to be spun out of existence, that was fun.
- Separated Windy into 3 weathers, the last two arent available currently.
  - Tornado.
  - Hurricane.
  - Firestorm.
- Gave cruiser gal a new song.
- Optimised a lot of collision detection in a lot of things.
- This is gonna be the last update for a while before the 1.0.0 release which will contain more content than this mod currently has, look forward to it.

## v0.14.3

- Added poster boy to the top of the readme.
- He thinks he's doing a good job.
- He shall open the gates when you are ready.
- Prepare.

## v0.14.2

- Updated README, it SHOULD have everything currently in here plus a bit more...
- Gave CruiserGal collisions.
- She also wont run over players and send them under the map anymore, probably, lol.
- Added a small eject while you're riding cruiser gal.
- Manor Lord no longer damages the puppet via collisions.
- Puppet can now only take damage once every 1 second rather than 0.5 seconds.

## v0.14.1

- Reverted a crate attempt fix cuz my friend has a luck skill issue.
- Fixed cruiser gal sounds and client bugs.
- Fixed IndustrialFan error spam.

## v0.14.0

- Added the last gal, cruiser gal.

## v0.13.7

- You can now ride jimothy inside or outside safely without problems.
- Fixed lag-spike when a coderebirth enemy spawns.
- Fixed Janitor breaking randomly-atleast for clients and in cases where it wouldn't grab objects but did for host.
- Fixed Jimothy sounds.
- Fixed Jimothy holding items incompat with cullfactory.
- Fixed problem with jimothy holding IndustrialFans, probably?

## v0.13.6

- Made performance of jimothy a bit better, and gonna do similar stuff with other coderebirth things later.
- Fixed AirControlUnit not being able to fire, at all, lol.

## v0.13.5

- Made it so my weathers dont spawn on Galetry.

## v0.13.4

- Fixed aircontrolunit despawn fix, and removed some code so that would make orbiting a tiny bit faster.

## v0.13.3

- Bear trap despawn fix, i think anyway.

## v0.13.2

- smol Pathfinding hotfix.

## v0.13.1

- I do what mel says.

## v0.13.0

- Added second member of cleanup crew, jimothy the transporter.
- Damage fix for janitor when you hit him.
- Extra fix for dropping stuff infront of janitor while he's grabbing an item.
- Added cleaning drone gal.
- Improved pathfinding completely in all CodeRebirth entities, this makes them able to fluidly use all fire exits, entrances, anything they need to reach you no matter where you are, should preform better than usual too (added a dependency to help since it'd be too much to include it into coderebirth itself).
- Hazards despawn if they spawn on top of doors.
- New weight defaults for manor lord, redwood titan and janitor.
- Did some fixing with tesla shock.
- Redid the tornado visuals entirely.
- Rewrote a lot of the code for tornados and meteor shower, let me know if anything breaks.
- Removed a meh performance thingy when you leave a moon for the crates and biomes.
- Crates opened by terminal gal should be sync'd with everyone.
- Fixed abuse of puppeteer's on hit animation.
- Probably fixed laser turret desync, let me know if i have not.
- Fixed bug with SCP 999 gal reviving body-less dead bodies and reviving more people than it should be able to via group revives.
- Fixed global duck song not playing.
- Can now store all the furniture like the gals etc.
- Changed layer of spike trap to be the maphazards layer (thanks zeekerss).

## v0.12.3

- Fixed shockwave gal spammin error.
- Balanced cleanup crew faster.
- Fixed misc bugs with him when hitting him mid animation.

## v0.12.2

- Made cleanup turn faster.
- Made him only be able to grab things infront of him.
- Fixed meteor shower sounds not working.

## v0.12.1

- Some stabilisation fixes to the cleanup crew.
- picks random trash can.
- Gave it higher priority.
- Misc fixes to janitor.

## v0.12.0

- Added cleanup crew.
- Cleaned up some assetbundles, if you notice loss of quality let me know and I'll restore some stuff.
- Fixed a bug with manor lord spawning enemies that its not supposed to know.
- Optimised navmesh code a bit.
- Fixed laser turret going through walls (woo fuck interiors that fucked it up, if it fucks up still then I'm gonna be sending you straight to the interior creators).
- Fixed tesla shock to actually trigger more often.

## v0.11.0

- Added Lord of the manor.

## v0.10.4

- Readme update! woo
- Forgot to include zort assets into the release build.
- Added a punishment from the duck abuse.
- Fixed bug with hoverboard weight not disappearing when dismounting with held.

## v0.10.3

- Added the 4 instruments from zort as scrap, harmonize together with your friends.

## v0.10.2

- Improved detection from terminal gal for scrap and items.
- Added warning sounds to suspicious thing.
- Fixed collisions with terminal gal and gave her a new facial expression for it.
- Added T!tan from rebalanced moons for weight of 0 for ACUnit.
- Put the suspicious thing into the enemy layer.

## v0.10.1

- Decreased chance of rare idle song from terminal gal.
- Accidently included unreleased assets into build.

## v0.10.0

- Added terminal gal.
- Added bear trap gal.
- Added ACUnit gal.
- Fixed explosions happening outside for clients through microwave.
- Added something suspicious...

## v0.9.7

- Fixed meteor shower craters not disappearing for clients.
- Fixed blue shrimp bug with dropping and grabbing in midair.
- Added pikmin to shockwave and seamine gal blacklist config.
- Added voicelines to bald man.
- Fixed some UI issues with duck.
- Gave value to grape and lemonade pitcher.

## v0.9.6

- Fixed inside bear traps.
- Change defaults of hazards to be more vanilla friendly and more like how zeekerss does turrets and landmines etc.
- Gave duck song enemy more configs.
- Lowered repeat quest chance to 15% from 99%.
- Added a thing for someone.

## v0.9.5

- Apparently despawning of crates was broken, no idea how, but reverted to how they despawned in previous versions.
- SCP 999 gal had a bug when somebody died and lost their corpse, this has been fixed.
- SCP 999 gal was also not interactable for quite a while because of a false/true accident.

## v0.9.4

- Added configs to the new thing I added.
- Added configs for acu for strength and knockback power.

## v0.9.3

- Fixed bug with seamine gal targetting inside enemies.
- Made craters in meteor shower disappear after 20 seconds to help performance.
- Bam bam bam ba rum da dum......
- Helped performance a bit when loading into orbit and despawning crates etc.
- Made crates not spawn the same items if more than one is broken in a moon.
- Fixed 999 not working with company config.
- Added sound to cruiser tires popping from bear trap.

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

</details>
