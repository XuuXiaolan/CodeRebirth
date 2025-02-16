# CodeRebirth

Code Rebirth is a big general content mod expanding on all parts of the game. This mod is highly configurable and each feature can be disabled separately.

If you're interested in helping with the development of the mod, feel free to reach out to [@xuxiaolan](https://discord.com/channels/1168655651455639582/1241786100201160784) on the Lethal Modding Discord!

![GoodJob](https://i.postimg.cc/9Mr5sSZj/image.png)

-Jimothy, Employee of the year

## Current Additions

- 9 Unique Hazards
- 4 Inside enemies
- 2 Outside Enemies
- 2 Ambient Enemies
- 2 Custom Weathers with variations
- Equipment Crates and Item Safes
- 6 Helpful Ship Upgrades
- 1 [̷͈̇̂ͅṘ̸̮̯E̶̺͊͛́D̸̨̉̌̃Ą̴̭͛C̵̨̪͑̈́̚Ṭ̵̝̙͋͂͊Ê̵̞̣͜͠D̷̝̟͛̈]̶̫͋̐͠
- 160+ Plant types!!!
- 4 Ship Decorations
- 2 Shop Items
- 11 Scrap with unique effects
- 3 Custom Model replacement (This needs you to have ModelReplacementAPI and MoreSuits installed!!)

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

</details>

<details>
  <summary><strong>Hazards</strong></summary>

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

![ACU](https://i.postimg.cc/jS9Rj24y/image.png)

> Designation : Air Control Unit  
> Objective : Shoot down threats  
>
> An old heavy anti air canon repurposed for shooting down airborne threats. These were the last defense measure against hostile wildlife during mining operations. The AC unit uses a powerful pneumatic system that compresses surrounding air to launch heavy air seeking projectiles.  
> [Final Recorded Equipment Transmission]  
> [ERROR] Software critical failure - Objective updated : Clear skies

### Item Crate

![ICW](https://i.postimg.cc/3Jz8Lfy1/image.png)
![SW](https://i.postimg.cc/k4NV8KT0/image.png)
![ICWM](https://i.postimg.cc/T2nKcWSF/image.png)
![ICMM](https://i.postimg.cc/K8fLDgKx/image.png)

> Safe: Spawns outside and is unlockable with a key, manually open it with the dial to get a random pieces of scrap!
> Metal (Mimic): Will trap you and digest you slowly...
> Wooden: Similar except you keep bashing it! gives you shop items.
> Wooden (Mimic): 20% Chance to replace a normal wooden crate with a mimic'd one...
</details>

<details>
  <summary><strong>Inside Enemies</strong></summary>

### Jimothy (Transporter)

![Jim](https://i.postimg.cc/mD7ZxNL2/image.png)

> Carries around hazards and crates, inside and outside, and relocates them.
> Due to his cheap circuits frying, he think he's doing a good job.
> I'd keep an eye out on where he's going...

### Scrap-E (Janitor)

![SE](https://i.postimg.cc/rm7NwbLB/image.png)

> A different take on the hoarding bug mechanic, will not be very happy if he sees you littering.
> The green one is bald.

### Puppeteer (Manor Lord)

![ML](https://i.postimg.cc/FsPQfn1J/image.png)
![VP](https://i.postimg.cc/0NCXZC41/image.png)

> Once betrayed, he haunts the mansion looking for the one who backstabbed him.
> On player contact, stabs the player with his pin and spawns a voodoo puppet following the player anywhere.
> If the puppet is damaged by any source, turret, landmine, other enemies, players, etc, the player would also be damaged.
> Keep your puppet safe.

### Duck Song

![DS](https://i.postimg.cc/1zw6FNrm/image.png)
![DB1](https://i.postimg.cc/kGpDznvY/image.png)
![DB2](https://i.postimg.cc/YqThxC9h/image.png)
![DB3](https://i.postimg.cc/LXhp7jQR/image.png)

> Gives a quest to find grapes to a player. Won't butcher you in any way whatsoever...
> The one and only duck from the hit DUCK song.
> and he waddled away, waddle waddle waddle... till the very next day bam bam bam bum ba ra ra bam.
</details>

<details>
  <summary><strong>Outside Enemies</strong></summary>

### Redwood Titan

![RT](https://i.postimg.cc/FHXjYh5p/image-removebg.png)

> Stomps around outside, crushing anything in its way
> Staying too close may prompt aggressive behavior.

### Carnivorous Plant

![CarnPlant](https://i.postimg.cc/d0xDgKFr/image.png)

</details>

<details>
  <summary><strong>Weathers</strong></summary>

### Windy

![WS](https://i.postimg.cc/c4W1tk0s/image.png)

> Disastrous weather where the player is pulled and thrown by different types of tornados.
> Decreases outdoor and daytime power by 3 each and increases indoor power by 6. 

### Meteor Shower

![MS](https://i.postimg.cc/Nf2FR2r4/image.png)

> World-ending weather where the world will slowly crumble as time goes on, but with the potential for rare crystals to spawn.
> Decreases outdoor and daytime power by 3 each and increases indoor power by 6.

</details>

<details>
  <summary><strong>Helpful Ship Upgrades</strong></summary>

### Shockwave Gal (SWRD-1)

![ShG1](https://i.postimg.cc/0y6MVyXk/image.png)

> Strong and Reliable, this robotic assistant can carry items back to the ship and kill enemies

### Seamine Gal (SEA-M1)

![SeG](https://i.postimg.cc/dt3jKvNX/image.png)

> A mix of Mechanical and biological components, gives players the ability to detect surrounding hazards and enemies through its sonar ping.
> Combat based robot, Attacks and kills enemies, its blast is strong enough to kill "unkillable" enemies.
> Attack charges recharge in orbit or when a key is used on her belt!

### Terminal Gal (DAISY)

![TeG](https://i.postimg.cc/5tyVBXsg/image.png)

> Utility based robot that has a few special abilities!
> Emergency teleport right back to the ship with a long cooldown.
> Immediate recharging of any held item.
> Unlock any door or safe!

### Cruiser Gal (MISS CRUISER)

![CrG](https://i.postimg.cc/8c8rvQmp/image.png)

> Utility based robot that holds unlimited scrap and follows you around!
> Can lead you into entrances both inside and outside.
> Has a special little tune included.

### 999 Gal (LIZ-ZIE)

![LIZ](https://i.postimg.cc/nzS1XSXT/image.png)

> Friendly Gelatinous smile dressed as a nurse that heals players that interact with her.
> Can also revive players nearby.
> Recharges on orbit or on quota depending on config.
> Highly configurable.

### Shrimp Dispenser

![SD](https://i.postimg.cc/SNnzQNLB/image.png)

> Dispenses Shrimp that deals 3 damage to enemies and 60 damage to players.
> One time use unless you dispense another.
> Dropping the Shrimp despawns it.
> Inspired by the shrimp from lockdown protocol, it's a lovely game.
</details>
<details>
  <summary><strong>Ship Decorations</strong></summary>

![SB](https://i.postimg.cc/mZJGVMzg/image.png)

> AIRCONTROL, BEARTRAP, HERMIT and CLEANER.
> Animated Ship decorations, no practical use.
> Zedfox not included.
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

> Cracked, rare and unique. This Snow Globe is found deep inside of abandoned moons.

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
  <summary><strong>Moon Changes</strong></summary>

### Diverse Flora

![F](https://i.postimg.cc/8C8k191j/image.png)

### Infectious Biomes

![IBCo](https://i.postimg.cc/G380FxFx/image.png)
![IBH](https://i.postimg.cc/wjRJfCfv/image.png)
![IBCr](https://i.postimg.cc/jq3xFLJx/image.png)

</details>
<details>
  <summary><strong>Ambient Enemies</strong></summary>

### Cutiefly

![CF](https://i.postimg.cc/zvmYv21Z/image-207-removebg-preview.png)

> Flies around occasionally resting on the ground. (harmless?????)
> DO NOT APPROACH DO NOT APPROACH [REDACTED].
> ON DEATH IT LIVES YET AGAIN, REBORN A NEW.

### Snailcat

![SC](https://i.postimg.cc/qMzFFhzh/imawadge-removebg-preview.png)

> Roams the land slowly (harmless)

</details>
<details>
  <summary><strong>Melanie</strong></summary>

![MelanieMelicious](https://i.postimg.cc/xd3PhrJ0/grinning-face.png)

> Fear the low value 2 handers

</details>

## Credits

**Moon Work:**

- Xu Xiaolan, [QWERTYrodrigo](https://www.youtube.com/watch?v=kHLM5DtR7Vc), SolidStone, S1ckboy, MrSaltedBeef, Siphon, NutDaddy

**Coding:**

- Xu Xiaolan, Bongo, TestAccount666, and WhiteSpike

**Modeling/Designs:**

- [QWERTYrodrigo](https://www.youtube.com/watch?v=AxE4TltnvjI), v0xx, S1ckboy, SolidStone, Codding Cat, IntegrityChaos, and Bobilka

**Sounds:**

- Oof Bubble, Moroxide

**Ideas/Lore:**

- [QWERTYrodrigo](https://www.youtube.com/watch?v=AxE4TltnvjI)

**Concept Art:**

- Flameburnt

**Misc/Organisation:**

- [Smxrez](https://www.youtube.com/shorts/6Mo9MJFu89M)

**Testing:**

- A Glitched NPC, QWERTYrodrigo, Lizzie, Slayer6409.

## Credits - Zort

The 4 Instrument models, music and zort playermodel are from the game "Zort" by Londer Software.

## Credits - Misc Models

"Baby Tomato Plant Scan (Low Poly)" (<https://skfb.ly/oHvNF>) by Marcos Silva is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
"Tomato Plant" (<https://skfb.ly/69J7v>) by zvanstone is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
"Wooden chair" (<https://skfb.ly/6WoYF>) by Kirhl is licensed under Creative Commons Attribution (<http://creativecommons.org/licenses/by/4.0/>).
