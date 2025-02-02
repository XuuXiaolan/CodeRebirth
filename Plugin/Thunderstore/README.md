# CodeRebirth

Code Rebirth is a general content mod expanding on all parts of the game.

If you're interested in helping with the development of the mod, feel free to reach out to [@xuxiaolan](https://discord.com/channels/1168655651455639582/1241786100201160784) on Discord!

![GoodJob](https://i.postimg.cc/9Mr5sSZj/image.png)

## Current Additions

- 160+ Plant types!!!
- 3 Custom Model replacement (This needs you to have ModelReplacementAPI and MoreSuits installed!!).
- 1 [̷͈̇̂ͅṘ̸̮̯E̶̺͊͛́D̸̨̉̌̃Ą̴̭͛C̵̨̪͑̈́̚Ṭ̵̝̙͋͂͊Ê̵̞̣͜͠D̷̝̟͛̈]̶̫͋̐͠
- 4 Inside Enemy.
- 2 Outside Enemy.
- 5 (+4 Decor) Helpful Creatures.
- 1 Ship Upgrade.
- 1 Shrimp Dispenser.
- 2 Ambient Enemies.
- 2 Weather with 7 different events total.
- 2 Shop Items.
- 11 Scrap with unique effects.
- 6 Custom Outside Objects.
- 6 Custom Inside Objects.

## Hazard Config

- Small tidbit on how the hazard config works for the inside hazards such as LaserTurret, FlashTurret, TeslaShock, etc.
- Config follows this structure: `MoonName - X1,Y1 ; X2,Y2 ; X3,Y3 | MoonName2 - Etc....`.
- The separators are:
  - `-` for MoonName and Coordinates
  - `,` for x and y value of a coordinate.
  - `;` for separating coordinates.
  - `|` for separating entries.
- This follows how vanilla spawns hazards on moons accurately, where vanilla generates a number between 0 and 1 and assigns it to the `X-axis`.
- Using that X-axis value, it picks the corresponding Y-value, rounds it to an integer and spawns that amount of hazards.
- It's done as a curve so that, depending on luck, you can have days where you spawn almost no hazards and on some days you have `Microwave Hell`, similar to some moons' `Turret Hell` rare occurance.
- Tool for visualising and creating curves easily <https://cosmobrain0.github.io/graph-generation/>.

## Preview (Spoilers ahead!)

<details>
  <summary><strong>Weathers</strong></summary>

### Windy

![WS](https://i.postimg.cc/c4W1tk0s/image.png)

> Disastrous weather where the player is pulled and ripped apart by different types of tornados.
> Decreases outdoor and daytime power by 3 each and increases indoor power by 6. 

### Meteor Shower

![M](https://i.postimg.cc/RFJzM5yL/image-removebg-preview-1.png)
![MS](https://i.postimg.cc/Nf2FR2r4/image.png)

> World-ending weather where the world will slowly crumble as time goes on, but with the potential for rare crystals to spawn.
> Decreases outdoor and daytime power by 3 each and increases indoor power by 6.

</details>

<details>
  <summary><strong>Inside Enemies</strong></summary>

### Duck Song

![DS](https://i.postimg.cc/1zw6FNrm/image.png)
![DB1](https://i.postimg.cc/kGpDznvY/image.png)
![DB2](https://i.postimg.cc/YqThxC9h/image.png)
![DB3](https://i.postimg.cc/LXhp7jQR/image.png)

> The one and only duck from the hit DUCK song.
> and he waddled away, waddle waddle waddle... till the very next day bam bam bam bum ba ra ra bam.

### Jimothy

![Jim](https://i.postimg.cc/GmNZG9Gk/image.png)
![JimTrap](https://i.postimg.cc/NMtCkXMr/image.png)

> He's literally programmed to think he's doing a good job.
> I'd keep an eye out on where he's going...

### Scrap-E

![SE](https://i.postimg.cc/vmr3Tmz8/image.png)

> A different take on the hoarding bug mechanic, will not be very happy if he sees you littering.
> The green one is bald.

### Lord Of The Manor

![ML](https://i.postimg.cc/x81wLZjZ/image.png)
![VP](https://i.postimg.cc/0NCXZC41/image.png)

> Once betrayed, he haunts the mansion looking for the one who backstabbed him.
> On player contact, extracts the player's blood and spawns a puppet following the player anywhere.
> If the puppet is damaged by any source, turret, landmine, other enemies, players, etc, the player would also be damaged.
> Keep your puppet safe.

</details>

<details>
  <summary><strong>Melanie</strong></summary>

![MelanieMelicious](https://i.postimg.cc/xd3PhrJ0/grinning-face.png)

> Fear

</details>

<details>
  <summary><strong>Outside Enemies</strong></summary>

### Redwood Titan

![RT](https://i.postimg.cc/FHXjYh5p/image-removebg.png)

### Carnivorous Plant

![CarnPlant](https://i.postimg.cc/d0xDgKFr/image.png)

</details>

<details>
  <summary><strong>Helpful Ship Upgrades</strong></summary>

### Shrimp Dispenser

![SD](https://i.postimg.cc/SNnzQNLB/image.png)

> Dispenses Shrimp that deals 3 damage to enemies and 60 damage to players.
> One time use unless you dispense another.
> Dropping the Shrimp despawns it.
> Inspired by the shrimp from lockdown protocol, it's a lovely game.

### Cruiser Gal

![CrG](https://i.postimg.cc/764ZkKBt/image.png)

> Friendly gal that holds unlimited scrap and follows you around!
> Can lead you into entrances both inside and outside.
> Has a special little tune included.

### Terminal Gal

![TeG](https://i.postimg.cc/kGgd0zJ2/image.png)

> Friendly gal that has a few special abilities!
> Emergency teleport with a long cooldown.
> Immediate recharging of any held item.
> Unlock any door or safe!

### Shockwave Gal

![ShG1](https://i.postimg.cc/2S37p0YR/SG1.png)
![ShG2](https://i.postimg.cc/TwvVDGDf/SG2.png)

> Friendly gal that carries scrap and kills enemies!
> Rechargable via time after orbit!

### Seamine Gal

![SeG](https://i.postimg.cc/nLK92rFv/image.webp)

> Friendly gal that gives players the ability to detect surrounding hazards and enemies.
> Reliable big sister for shockwave gal and can kill "unkillable" enemies.
> Rechargable via time after orbit or with a key on her belt!

### 999 Gal

![LIZ](https://i.postimg.cc/nzS1XSXT/image.png)

> Friendly gal that heals players that interact with her.
> Can also revive players nearby.
> Recharges on orbit or on quota depending on config.
> Very configurable.

</details>

<details>
  <summary><strong>Ambient Enemies</strong></summary>

### Cutiefly

![CF](https://i.postimg.cc/zvmYv21Z/image-207-removebg-preview.png)

> Flies around occasionally resting on the ground. (harmless)

### Snailcat

![SC](https://i.postimg.cc/qMzFFhzh/imawadge-removebg-preview.png)

> Roams the land slowly (harmless)

</details>

<details>
  <summary><strong>Shop Items</strong></summary>

### Hoverboard

![HB](https://i.postimg.cc/wj6mw7Nc/hoverboard.png)

> Shop Item that allows you to drift around the world, should be faster than walking speed and allows a boost using sprint.

### Wallet

![W](https://i.postimg.cc/wMBrg32r/imwadadage-removebg-preview.png)

> Shop Item to get some extra cash for the quota can pick up coins.

</details>

<details>
  <summary><strong>Scraps</strong></summary>

### Guitar

![GU](https://i.postimg.cc/5025L276/Guitar-Icon.png)

> From hit game "Zort", this instrument can be harmonised with the 3 other instruments added for beautiful music.

### Recorder

![RE](https://i.postimg.cc/DwStd7Np/Recorder-Icon.png)

> From hit game "Zort", this instrument can be harmonised with the 3 other instruments added for beautiful music.

### Violin

![VI](https://i.postimg.cc/wT7Wn0k7/Violin-Icon.png)

> From hit game "Zort", this instrument can be harmonised with the 3 other instruments added for beautiful music.

### Accordion

![AC](https://i.postimg.cc/4d0ccbHS/Accordion-Icon.png)

> From hit game "Zort", this instrument can be harmonised with the 3 other instruments added for beautiful music.

### Snow Globe

![SG](https://i.postimg.cc/NfBS0qgy/snowglobe-icon.png)

> Cracked, rare and unique. This Snow Globe is found deep inside of abandoned moons, made for children but loved by all. (Includes custom animations and sounds)

### Meteorite (Sapphire)

![MS](https://i.postimg.cc/gJff3RxD/image.png)

### Meteorite (Emerald)

![ME](https://i.postimg.cc/8PsDsz8n/image.png)

### Meteorite (Ruby)

![MR](https://i.postimg.cc/prXbTzmp/image.png)

> Valuable rare Scrap found from the remaining debris of some Meteors.
> Yes I'm aware the ruby looks ass.

### Epic Axe

![EA](https://i.postimg.cc/wxWPFcTY/imwadaage-removebg-preview.png)

> Cool glowy Axe!
> Can crit and deal 2x damage.

### Nature's Mace

![NM](https://i.postimg.cc/zvKF6H00/image.png)

> Mace that uses the power of nature to strike your enemies.
> Heals enemies and players alike (players to 80 hp max, enemies infinitely).
> Can crit and deal 2x damage.

### Spiky Mace

![SM](https://i.postimg.cc/5tr5tSrs/image.png)

> Looks like it would hurt a lot...
> Deals 2 damage by default, very powerful!
> Can crit and deal 2x damage.

### Icy Hammer

![IH](https://i.postimg.cc/G2NsQgQD/image.png)

> With the power of ice, enemies may be slowed down temporarily...
> Can crit and deal 2x damage.

### Pointy Needle

![PN](https://i.postimg.cc/6QfKxn8B/image.png)

> Obtained by defeating the Lord Of The Manor.
> Might have more to it later on.

### Puppet

![PS](https://i.postimg.cc/YSC7kjYg/image.png)

> Obtained by defeating the Manor Lord that puppetted the player.
> It's you, but better!

### Coin

![C](https://i.postimg.cc/cC5bHZ5L/imagwadae-removebg-preview.png)

> Scrap to get some extra cash for the quota, Coin doesn't affect normal-level scrap spawn rates and is not included in the pool normally.
> Rumours say this ancient currency can be used to trade with [INFORMATION NOT AVAILABLE].

</details>

<details>
  <summary><strong>Miscellaneous</strong></summary>

### Bug Zapper

![BZ](https://i.postimg.cc/GpGRtvjj/image.png)

> Designation : Bug Zapper  
> Objective : Pest Control  
>
> These giant electric zappers, capable of delivering fatal electric shocks, were instrumental in the protection of valuable assets. After detecting a threat, the giant tesla coil would charge up before delivering a strong shock, deterring or killing any attackers.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : Zap metal carrier, Zap bug, Zap, Zap, Zap.

### Laser Turret

![LT](https://i.postimg.cc/1t3v2Q4N/image.png)

> Designation : Laser Assisted Soil Excavation Rig (L.A.S.E.R.)  
> Objective : Mine and Extract minerals
>
> The L.A.S.E.R. is the back bone of mining operations, this experimental tech uses a massive carved ruby that focuses light into a single point creating a laser capable of melting solid rock. Energy efficient and powerful, this device is instrumental in the quick extraction of ores.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : Spin, Mine, Spin, Mine,Spin, Mine.

### Industrial Fan

![IF](https://i.postimg.cc/htGbKrcH/image.png)

> Designation : Industrial Fan  
> Objective : Aeration  
>
> These giant industrial fans were used for aeration during mining operations, keeping dust off equipment and keeping crewmates cooled down. The fan's automated system would control fan speed by detecting the amount of dust, gas and other various aerosols.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : Fan Speed - Max, Maximum aeration mode - 360 degree coverage  

### Functional Microwave

![FM](https://i.postimg.cc/x84jMnNG/image.png)

> Designation : Experimental Microwave  
> Objective : Microwave rock
>
> These modified microwave ovens were used alongside L.A.S.E.R. devices to help in the mining operation. Using a modified power supply, the microwave shoots high microwave radiation at rocky surfaces to weaken and fracture surfaces. Once a surface is weakened by an automated microwave, the mining crew can start extraction using manual tools and L.A.S.E.R devices.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure  
> [Log] Crewmate found with content of 6% various minerals  
> [Update] Weaken minerals from crewmate  
> [Update] Objective updated : Microwave crewmate

### Flash Turret

![FT](https://i.postimg.cc/FH9mzY6t/image.png)

> Designation : WunderFoto Pro Flash Camera  
> Objective : Survey and Photograph
>
> The WunderFoto Pro Flash Cameras were deployed early on into mining operations to survey the local terrain for ores and photograph local wildlife for research purposes. These state of the art cameras are controlled by the latest company software with a reliable AI that will photograph and send data directly to the ship.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : photograph crew, photograph crew, photograph crew, photograph crew.

### Bear Trap

![BT](https://i.postimg.cc/xdF738T4/image.png)

> Designation : Bear Trap  
> Objective : Wildlife control  
>
> These old mechanical bear traps, rusty but reliable, were used as a defensive measure against the local hostile wildlife trying to interrupt mining operations.  
>
> [Final Recorded Equipment Transmission]  
> N/A

### Air Control Unit

![ACU](https://i.postimg.cc/HxLZnQKR/image.png)

> Designation : Air Control Unit  
> Objective : Shoot down threats  
>
> An old heavy anti air canon repurposed for shooting down airborne threats. These were the last defense measure against hostile wildlife during mining operations. The AC unit uses a powerful pneumatic system that compresses surrounding air to launch heavy air seeking projectiles.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : Clear skies

### Item Crate

![ICW](https://i.postimg.cc/3Jz8Lfy1/image.png)
![ICWM](https://i.postimg.cc/T2nKcWSF/image.png)
![ICM](https://i.postimg.cc/g0xR1608/image.png)
![ICMM](https://i.postimg.cc/K8fLDgKx/image.png)

> Wooden: Spawns outside and is openable instantly with a key, or at a slow speed manually to get a random piece of scrap!
> Wooden (Mimic): 20% Chance to replace a normal wooden crate with a mimic'd one...
> Metal: Similar except you keep bashing it! gives you shop items.
> Metal (Mimic): Will trap you and digest you slowly...

### Diverse Flora

![F](https://i.postimg.cc/8C8k191j/image.png)

### Infectious Biomes

![IBCo](https://i.postimg.cc/G380FxFx/image.png)
![IBH](https://i.postimg.cc/wjRJfCfv/image.png)
![IBCr](https://i.postimg.cc/jq3xFLJx/image.png)

</details>

## Credits - CodeRebirth Team

**Coding:**

- Xu, Bongo, TestAccount666, and WhiteSpike

**Modeling/Designs:**

- [Rodrigo](https://www.youtube.com/watch?v=AxE4TltnvjI), v0xx, S1ckboy, Solid, Codding Cat, IntegrityChaos, and Bobilka

**Sounds:**

- Oof Bubble, Moroxide

**Ideas/Lore:**

- [Rodrigo](https://www.youtube.com/watch?v=AxE4TltnvjI)

**Concept Art:**

- Flameburnt

**Misc/Organisation:**

- Smxrez

**Testing:**

- A Glitched NPC, Rodrigo, Lizzie, Slayer.

## Credits - Zort

The 4 Instrument models and music from the game "Zort".

## Credits - Misc Models

"Baby Tomato Plant Scan (Low Poly)" (<https://skfb.ly/oHvNF>) by Marcos Silva is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
"Tomato Plant" (<https://skfb.ly/69J7v>) by zvanstone is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
"Wooden chair" (<https://skfb.ly/6WoYF>) by Kirhl is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
