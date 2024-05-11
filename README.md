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
#### 204-Yield
#### Population: Slaughtered.
#### Conditions: High winds, arid, and with constantly shifting sand dunes.
#### Fauna: Ecosystem supports territorial behavior, and is primarily subterranean.
#### History: 204-Yield distantly orbits the red giant star Rightside. 204-Yield itself used to harbor advanced civilization, but it is hypothesized that roaming anomalous creatures devoured most of the population, the remaining individuals fleeing the planet permanently.
![YieldMoon](./ImageStorage/YieldMoon.png)
#### 58-Heart
##### Population: Haunted.
##### Conditions: Humid. Cool, with a thick atmosphere. Craggy, tectonically-uplifted terrain.
##### Fauna: Badly infested with aggressive wildlife.
##### History: This planet appears to have been used for research, and it lies in a close orbit around a smoldering brown dwarf. Its surface is littered with failed experiments, attempting to create life, always failing in some way or another.
![HeartMoon](./ImageStorage/HeartMoon.png)
#### 2229-Postage
##### Population: Declining.
##### Conditions: Humid. Warm, with a slowly-escaping atmosphere. Jagged, river-cut terrain.
##### Fauna: Home to many types of territorial wildlife.
##### History: Formerly used for recreational purposes, most likely a getaway location for particularly rich individuals, 2229-Postage orbits known gas planet Step Up. Its inhabitants have begun to show signs of radiation poisoning due to the system's brilliant white-blue sun, leading to a major decline in population in the last century.
![PostageMoon](./ImageStorage/PostageMoon.png)
#### 848-Warning
##### Population: Desiccated.
##### Conditions: Somewhat dry. Cool, with a thin atmosphere. Craggy, sand-swept terrain.
##### Fauna: Home to a few types of peaceful wildlife.
##### History: Originally used to research its binary suns, 848-Warning quickly became a popular tourist destination for its proximity to the famous Icicle Nebula, leading to disruption of the planet's climate. Its population faced a resource crisis and swiftly perished as the planet's oceans dried up.

#### 2-Hestia
##### Population: Raptured.
##### Conditions: Very humid. Warm, with a thick, steamy atmosphere. Smooth, rolling terrain.
##### Fauna: Overrun with somewhat manageable wildlife.
##### History: This planet continues to actively be used for weapons testing. It regularly undergoes barrages of Old Bird drops and missile bombardments, making recovery of scrap incredibly difficult, but incredibly profitable. Bombardments tend to come every few hours, largely at random, only signified by the blaring sirens. This is one of the few moons designated with the risk level of X.

#### 132-Concept
##### Population: Prowling.
##### Conditions: Humid. Hot, with consistent boiling rainfall. Weathered-down plains make up most of its terrain.
##### Fauna: No life survives outdoors.
##### History: This moon's harsh conditions are the result of military testing over the last multiple centuries. Incendiary weaponry has been utilized repeatedly across the whole moon's surface, in an attempt to vanquish any loose genetic experiments- this failed, however, as the now-transformed population of this moon simply fled indoors, into the bunker system beneath the surface.

#### 363-Scrimmage
##### Population: Fading.
##### Conditions: Dry and extremely cold, having been stripped of its atmosphere centuries ago.
##### Fauna: None.
##### History: 363-Scrimmage, once used as a site to mine explosive minerals, underwent a rapid chain reaction which created a shockwave that ripped the atmosphere clean off the surface of this moon. Its parent planet, Nighthawk, causes strong earthquakes which occasionally stir awake the automatic mining facilities' defense mechanisms, in the form of aptly-named "Nuggets", miniaturized sentry turrets scattered randomly underground that rise from the soil upon detecting heavy activity above.

#### 599-Potential
##### Population: Monstrous.
##### Conditions: Very humid and somewhat cold, constant snowfall. Terrain is flattened by its recent ice age.
##### Fauna: Home to many types of usually-peaceful wildlife.
##### History: The twin moon to 363-Scrimmage, 509-Potential was the site of major refineries for the minerals that were extracted. After exposure to volatile compounds and biohazards, a plague spread across the population, corrupting many into the monsters we see today, and killing off the rest. Though 509-Potential is home to immense riches due to its status as a mineral refinery, it is incredibly dangerous.

#### 729-Empathy
##### Population: Unmoving.
##### Conditions: Very humid. Warm, with constant gentle rainfall. Craggy terrain formed by intense river activity.
##### Fauna: Terrain supports a few types of territorial wildlife.
##### History: Once used for mining natural gas and oil, 729-Empathy's proximity to gas giant Broken Window caused immense tidal stress on its surface, disrupting mining operations and settlements. The danger was amplified even further when the moon came under attack with multiple biological conversion agents, petrifying the settlers and workers permanently.

#### 46-Concave
##### Population: Captured.
##### Conditions: No water in any form. Warm, and very dusty. Terrain is incredibly difficult to traverse, often riddled with holes, caves, and tight passages.
##### Fauna: Ecosystem centers around the planet's few apex predators.
##### History: Once used for weapons testing, when a rogue artificial intelligence awoke, it swiftly captured the settlers of the planet, subjecting them to experiments of its own. After a century and a half of operation, the AI shut down permanently, and its facility fell into disarray.

#### 766-Redemption
##### Population: Abandoned.
##### Conditions: Very dry, and somewhat cool, mostly covered in glaciers, making it quite hard to find your way into the facility.
##### Fauna: Home to a few types of aggressive wildlife.
##### History: This moon, once used as a site for subglacial organism studies, came under attack during the last Great War, and the ensuing nuclear winter plunged the moon into an ice age, wiping out all but the hardiest of its wildlife, though the ecosystem continues to recover to this day.

#### 99-Serenity
##### Population: Evacuated.
##### Conditions: Covered in a worldwide taiga forest, with rugged terrain beneath it.
##### Fauna: Thriving ecosystem encourages unique behaviors. Badly infested with wildlife.
##### History: 99-Serenity was a standard colony moon, home to over 800 million people at its peak. Orbiting the gas giant Maybe Man, it served as a center of commerce for multiple centuries, until the last Great War, when a biological weapon caused rapid artificially-induced evolution in the wildlife of the moon. Forests quickly began to propagate across every square inch of the surface, and creatures quickly overran the colonies, causing an evacuation order to be put in place. Nearly 600 million people successfully escaped the moon, with the other 200 million succumbing to the rapidly-spreading environment.

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
- **Effects:** Increases enemy spawn rate and power level both inside and outside the facility. The environment becomes dark all day with a blood-red eclipsed sun, making it more challenging than typical eclipsed conditions.

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
