# Code:Rebirth
Overhaul mod for Lethal Company
## Working-On List:
### Enemies

### Moons

### Scrap/Items
#### Snow Globe
- **Items:** Snow Globe + some variant insides
- **Description:** Cute looking Snow Globe that you can shake and plays some cute dropship music.

#### Wallet
- **Items:** Wallet + some variant colours
- **Description:** Stores coins.

#### Coins
- **Items:** Coins
- **Description:** Gets stored by a wallet, its value transferred.

#### Meteorite Shard
- **Items:** Gems and Crystals
- **Description:** Dropped from Meteors during Meteor Shower weather at a low low chance, glows and looks pwetty. 

### Weathers
#### Meteor Shower
- **Availability:** All worlds
- **Effects:** Randomly, Meteors will spawn way up in the sky and fall down, causing a small-ish crater with a small 5% chance to spawning a meteorite shard for scrap.

### Misc

## TODO List:

### Enemies
#### Concept: CatSnail
##### Description
##### Behaviour
##### Vulnerability and Defense
##### Spawn Behaviour
##### Additional Characteristics

#### Concept: The Nesting Bug - Advanced Looter
##### Description
The Nesting Bug is a formidable new foe resembling a fusion of a dragonfly and a centipede, characterized by its large size and unsettling appearance. It primarily inhabits forested and temperate moons, where it adopts behaviors akin to those of the tulip snake and loot bug.

##### Behavior
- **Stealing Mechanism:** The Nesting Bug actively steals scrap and other movable items from the environment, including unique objects like beehives, to fortify its nest. It exhibits a high level of persistence and cleverness in its thievery.
- **Aggression Trigger:** When struck by a shovel, the Nesting Bug switches from thievery to aggression, targeting the player. It captures and lifts the player only to drop them from significant heights, potentially causing severe damage or disorientation.

##### Vulnerability and Defense
- **Weakness:** Despite its menacing behavior, the Nesting Bug can be killed with a few hits from a shovel. However, it becomes highly aggressive after the first hit.
- **Predation:** While formidable in its thievery, the Nesting Bug is vulnerable to attacks from other predators like baboon hawks, dogs, and Old Birds. It tends to avoid direct combat with these creatures due to its fragility.

##### Spawn Behavior
- **Location:** Spawns outside on moons with forested or temperate climates.
- **Nesting:** Has a designated nesting site on the map where all stolen items are accumulated.

##### Additional Characteristics
- The presence of the Nesting Bug introduces a new dynamic to resource management and player strategy, particularly in how players secure their belongings and navigate the moon's terrain.

#### Concept: The Guardsman - Nutcracker Variant
##### Description
The Guardsman is a formidable enemy inspired by medieval knights, donning a suit of armor and wielding a longsword. This enemy stands motionless in an "at arms" position, with the sword planted in the ground, and remains perfectly still until it detects movement.

##### Activation Mechanism
Upon spotting movement, the Guardsman emits a charging horn sound and aggressively pursues the nearest moving object, swinging its longsword in sweeping motions reminiscent of a jousting knight.

##### Attack Behavior
- **Engagement:** Charges towards detected movement, swinging its sword.
- **Reset:** If it loses sight of its target, it returns to its original position, replanting its sword in the ground.

##### Vulnerability and Defense
- **Counter Strategy:** Best attacked from behind with a shovel to pop its head off, which deactivates it. The initial shovel strike provokes a forward charge.
- **Loot:** Upon defeat, its helmet can be used for protection against various environmental hazards, though it is heavy, requires two hands, and significantly obscures vision. The sword can be collected as a weapon, offering stronger attacks than a kitchen knife but with a slower setup and the risk of sweeping strikes affecting unintended targets.

##### Spawn Behavior
- **Location:** Typically spawns at a moderate distance from facility entrances, often facing away, allowing players a chance to strategize their approach.
- **Non-Discriminatory:** Attacks any moving entity, including other outside enemies like manticoil birds and tulip snakes.

##### Additional Characteristics
- The Guardsman adds a layer of tactical challenge to forested moons or those with large structures, like Titan or Artifice.

#### Concept: The Nuggets - Semi-Mobile Turrets
##### Description
The Nuggets are compact, semi-mobile turrets designed to blend seamlessly into their environment. They activate and emerge from the ground when detecting significant weight, opening fire with precise, long-range attacks.

##### Activation Mechanism
Activated by over 90 pounds of weight stationary above them, orienting towards vibration source to engage.

##### Fire Mode and Behavior
- **Firing Capabilities:** Narrow spread, long-range, slow damage rate.
- **Post-Fire Behavior:** If target not eliminated, either turn 180° and fire again or emerge fully and roam for new targets.

##### Secondary Activation
- Engages new targets found, then retracts to recharge. If no target, becomes dormant at new location.

##### Vulnerability and Defense
- **Durability:** Destroyable with two shovel hits; immune to knives and shotguns.
- **Defensive Reaction:** 360° firing spin if not destroyed by the first shovel hit.

##### Additional Characteristics
- Emits R2D2-esque sounds, adding a distinctive audio signature.

##### Strategic Implications
- More of a strategic nuisance than a threat, requiring mindful player movement.

### Moons
#### 58-Heart
##### Population: Haunted
##### Conditions: Humid, cool, thick atmosphere, craggy terrain
##### Fauna: Aggressively infested with wildlife
##### History: Previously a research site, now littered with failed life-creation experiments. Orbits a smoldering brown dwarf.
![HeartMoon](./ImageStorage/HeartMoon.png)
#### 2229-Postage
##### Population: Declining
##### Conditions: Humid, warm, slowly-escaping atmosphere, jagged terrain
##### Fauna: Home to territorial wildlife
##### History: Once a luxury retreat, now suffering from radiation poisoning due to its sun, causing a sharp population decline.
![PostageMoon](./ImageStorage/PostageMoon.png)
#### 848-Warning
##### Population: Desiccated
##### Conditions: Somewhat dry, cool, thin atmosphere, sand-swept terrain
##### Fauna: Few peaceful wildlife species
##### History: Former research site and tourist hotspot, now devastated by climate disruption from tourism and dried oceans.

#### 2-Hestia
##### Population: Raptured
##### Conditions: Very humid, warm, thick steamy atmosphere, rolling terrain
##### Fauna: Manageable wildlife
##### History: Active weapons testing site with frequent bombardments and sirens, designated risk level X.

#### 132-Concept
##### Population: Prowling
##### Conditions: Humid, hot, constant boiling rain, weathered plains
##### Fauna: No outdoor survival
##### History: Moon transformed by military incendiary tests, with a hidden population in underground bunkers.

#### 363-Scrimmage
##### Population: Fading
##### Conditions: Dry, extremely cold, stripped atmosphere
##### Fauna: None
##### History: Former mining site for explosive minerals, now with no atmosphere due to a catastrophic chain reaction. Occasional earthquakes activate underground miniaturized turrets known as "Nuggets."

### Scrap/Items
#### Beach Items on 2229-Postage
- **Items:** Beach balls, umbrellas, beach towels
- **Description:** Typical beach-related items scattered around, reflecting the moon's past as a luxury getaway.

#### Bones and Exoskeletons on 58-Heart
- **Items:** Various bones, complete skeletons, exoskeletons of failed experiments
- **Description:** The remains of various creatures, hinting at the moon's history of biological research and experimentation.

#### Plushies in Manor Interiors
- **Items:** Generic plushies, including teddy bears and plush sharks
- **Description:** Soft toys found within manor settings, adding a touch of personal history and homeliness to these spaces.

#### Winter Gear on Snowy Moons
- **Items:** Individual mittens, hats, boots, snow shovels
- **Description:** Essential winter gear, abandoned or lost in the snowy environments of the moon.

#### Books in Manor Interiors
- **Items:** Various books
- **Description:** Books ranging from fiction to encyclopedias, found predominantly in manor interiors, suggesting a rich history and cultured past occupants.

#### Action Figures
- **Items:** Action figures of monsters like nutcrackers and jesters
- **Description:** Detailed miniatures making little noises relevant to their design, such as clacking for nutcrackers and popping for jesters. These can be found across various environments, adding a playful element to the scrap collection.

#### Humorous and Unusual Items
- **Items:** Cat ear headbands, grenades, steel folding chairs, sports medals
- **Description:** A collection of eclectic and humorous items that add a light-hearted contrast to the often grim settings of the moons.

### Weathers

### Weathers
#### Ion Storm
- **Availability:** Universal
- **Effects:** Creates a thick fog and intermittent electrical bolts, simulating a toned-down lightning storm. Disables all electronic devices outdoors. Delays delivery times by double.

#### Sandstorm
- **Availability:** Sandy Moons (e.g., Offense)
- **Effects:** Significantly slows player movement and reduces visibility. Small scrap items may be blown away if not secured.

#### Rogue Waves
- **Availability:** Watery Moons
- **Effects:** Water levels fluctuate dramatically and unpredictably. A new item, a life preserver, can be purchased to prevent drowning.

#### Blackout
- **Availability:** Universal
- **Effects:** External lights fail and internal lights flicker, turning off and on every few minutes in real life.

#### Orbital Drops
- **Availability:** Worlds like Artifice, Embrion, and those with Old-Bird presence
- **Effects:** Randomly, every 30 minutes to 6 hours, an Old Bird drops from orbit, activated upon arrival. There is a chance an existing Old Bird is awakened instead of a new one dropping.

#### Orbital Bombardment
- **Availability:** Same as Orbital Drops
- **Effects:** Sirens warn of incoming orbital barrages that last up to a minute, causing significant damage to anything outdoors. Occasionally, inactive bombs can be collected as valuable scrap.

#### Enchantment
- **Availability:** Forested Moons
- **Effects:** Certain areas are enveloped in a glowing purple mist that pacifies creatures and grants a temporary boost to players, similar to a TZP effect.

#### Heatwave
- **Availability:** Non-snowy Moons
- **Effects:** Causes heat ripples, faster stamina drain, and increased movement penalty due to heat. Reduces enemy spawn rates outdoors.

#### Fire Tornadoes
- **Availability:** Desert and Hot Moons
- **Effects:** Creates fiery tornadoes that cause damage when in close proximity, using visuals similar to those from HD2's Old-Bird-fire-like particles.

#### Blood Moon
- **Availability:** Any moon that has an entry fee above a certain threshold
- **Effects:** Increases enemy spawn rate and power level both inside and outside the facility. The environment becomes dark all day with a blood-red sun, making it more challenging than typical eclipsed conditions.

##### Impact on Gameplay
[How this weather affects player strategies, enemy behavior, or game environment.]

### Misc
#### [Miscellaneous Category]
##### Description
[General description or list of miscellaneous items, features, or game mechanics to be added.]

#### Feature: [Feature Name]
##### Functionality
[Description of what the feature does and how it integrates into the game.]

##### Importance
[Why this feature is essential or beneficial to the game.]