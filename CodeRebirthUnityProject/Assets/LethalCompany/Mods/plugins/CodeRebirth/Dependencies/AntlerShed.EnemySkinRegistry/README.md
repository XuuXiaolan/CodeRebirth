
# Enemy Skin Registry

This mod gives developers a means to register client-side enemy skins to be mutually exclusive with other registered enemy skins. It gives users the ability to control when and where those skins will appear, as well as how frequently.


## Features

### Completed
- LethalConfig-accessible GUI for end users to set configurations 
- Registration of custom Skin implementations
- Configurable distribution of skins per enemy
- Configurable distribution of skins per moon
- Automatic registration for LLL moons
- Named skin configuration profiles
- Events on vanilla enemies for making fully custom skins with fewer patches (send a request for an event type to my discord or as a feature request on github)

### Implemented but not fully tested
- Custom Enemy Registration
- Config synchronization for players that have the same mods installed

### Planned
- More Event listeners

## User Guide

###Configuration Menu

The enemy skin configuration is accessible through the Lethal Config menu.

![The skin configuration menu as it appears in the Lethal Company GUI](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/menu.PNG?raw=true)

The top bar will let you scroll through the registered enemies. 
Use the right and left arrows to navigate through them. The name of the enemy in the middle will have its configuration displayed in the rest of the ui below.

![The skin configuration menu showing an alternate enemy configuration](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/MenuSwitchEnemy.PNG?raw=true)

The panel in the lower left may be used to activate and de-activate registered skins. Click the skin's icon on the left side of each entry to toggle whether an enemy can spawn with this skin or not. Deactivation works mid round to allow for buggy skins to be removed if they are causing problems. This is the only time where adjusting the config will change any already-spawned enemy.

![The active skin section of the skin configuration menu, showing a deactivated skin](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/menuDeactivated.PNG?raw=true)

The panel in the lower right is used to configure the likelihood of a skin being selected when an enemy of a particular type spawns. Each moon can be given its own distribution of skins. The "Default" distribution is for any moon that does not have an explicit distribution. Add a skin to a distribution by clicking the "Add Skin..." dropdown on a configuration.

![The add skin dropdown being expanded](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/menuAddSkin.PNG?raw=true)

Each skin on each moon can be set to spawn more or less frequently. Setting the bar to the max will make it take up all of its fair share of the remaining spawn chance, and setting the bar to nothing will make it never spawn. So if you have three skins all set to the max value, they'll each have a 1/3 chance of spawning. But if you set two of those skins to half, then the distribution becomes 1/4, 1/4, 1/2.

Add a distribution for a moon by clicking the "Add Moon..." dropdown.

![The expanded add moon dropdown](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/menuAddMoon.PNG?raw=true)

![The distribution menu added with the new moon configuration](https://github.com/Yinigma/EnemySkinRegistry/blob/main/Images/MenuAddedMoon.PNG?raw=true)

Any default configurations modders have included with their mod will appear in the dropdown menus just above the "Save Config" section of the GUI. Select "Moon" or "Skin" from the first dropdown and your desired mod in the second dropdown, then click the "Reapply" button to apply it to the working configuration.

Finally, hit the "Save Config" button to save any changes. Changes are immediately applied to skin selection rates and will affect any enemies that spawn after. Changes will not affect enemies that have already spawned except in the case of deactivating skins.

If you want to store your configuration as a profile for quickly loading later, hit the "Store as Profile" button. Profiles can be loaded using the same dropdowns
as the default skin and default moon configs.

### Other Options in Lethal Config

- Allow Sync - When this setting is toggled on, clients can opt in to syncing with your profile when you're the host of a game

- Attempt Sync - When this setting is toggled on, if you're playing a game as a client, you will attempt to sync your profile with the host

- Indoor/Outdoor - Toggles the indoor and outdoor tabs in the skin configuration menu

- Log Level - The verbosity of the skin registry's logger - for troubleshooting purposes

## For Developers

### Getting this mod as a dependency

To add this mod to your project, I'd recommend using [ThunderKit](https://github.com/PassivePicasso/ThunderKit). But if you'd rather avoid using it, you can simply download this mod and use it like a unity package.

### Adding a Custom Skin Implementation

This mod has a [companion mod](https://thunderstore.io/c/lethal-company/p/AntlerShed/EnemySkinKit/) with default skin implementations that allow for a codeless approach. However, if that approach is too limiting for what you need to get done, then you can still create a skin mod that's compatible with this one by following these steps. For this I'm assuming you have some programming experience.

#### 1. Implement the Skin interface
Implementors of the Skin interface are required to provide a handful of metadata:
    
- Label - The name of the skin as it will show up in the configuration menu
- Id - The unique id of the skin. To help enforce uniqueness, it's recommended that ids follow the pattern "<AuthorName>.<SkinName>"
- EnemyId - The unique id string of the enemy type the Skin will be attached to.
- Icon - A Unity Texture2D to be used as the skin's icon so it can be previewed in the configuration menu

Along with this information, the Skin interface follows the abstract factory pattern to produce Skinners. Each time an enemy spawns, a Skin is picked based on the user configuration and asked to create an instance of Skinner. Each Skin has an enemy type and each Skinner is mapped to an instance of that corresponding type. 
```csharp
class MySkin : Skin
{
    public Skinner CreateSkinner()
    {
        //Return your skinner implementation from here
    }
}
```
#### 2. Implement the Skinner interface
The Skinner interface is only required to implement two methods:
```csharp
class MySkinner : Skinner
{
    void Apply(GameObject enemy)
    {
        //Perform any logic here to modify the appearance of the enemy. All of it must be client-side.
        //This is also the point where an EventHandler is registered if your skinner makes use of it. To do so, call EnemySkinRegistry.RegisterEventHandler(enemy, MyEventHandler)
    }

    void Remove(GameObject enemy)
    {
        //Restore the enemy to its vanilla appearance, undoing all of the changes done by Apply.
        //Unregister the event handler by calling RemoveEventHandler(enemy) if you registered one.
    }
}
```

For vanilla enemies, the game object passed to these methods is the one containing the EnemyAI component. Applying a skin to a modded enemy will require the author of the enemy mod to add calls to the registry from their logic, or third-party patching. This is covered elswhere in this document.

#### 3. Call RegisterSkin

Once you have implemented both of these classes, call the following from the Awake method in your plugin:
```csharp
EnemySkinRegistry.RegisterSkin(new MySkin());
```
Note that this does nothing for creating and reading from asset bundles. Resolution of dependencies on textures, models, animations, sounds, and other assets that a Skin or Skinner might have is the responsibility of the developer.

Optionally you may include a default configuration for your skin:

```csharp
MySkin mySkin = new MySkin();
EnemySkinRegistry.RegisterSkin
(
	mySkin,
	new DefaultSkinConfigData
	(
		//List of moon-id to frequency pairs
		// 0.0 means it never appears, 1.0 means it appears 			//as frequently as possible when considering other 			//skins 
		new DefaultSkinConfigEntry[]
		{
			new DefaultSkinConfigEntry
			(
				EnemySkinRegistry.OFFENSE_ID,
				1.0f
			),
			new DefaultSkinConfigEntry
			(
				EnemySkinRegistry.MARCH_ID,
				0.5f
			),
		},
		//Default frequency
		//The frequency of this skin on any unconfigured map
		0.0f,
		//Vanilla fallback frequency
		//Optional - frequency of the vanilla appearance if 			//this default config ends up making a new config 			//entry for the enemy on a moon
		0.0f
	)
);
```


### Event Handlers

Enemy Event handlers are an in-progress feature to allow developers to fully overhaul an enemy's appearance without relying on asset replacement. Enemies will notify a handler class that a certain event has occured, e.g. a bracken snaps a player's neck or a blind dog lunges, and that handler will run some logic when that happens.

To receive events from an enemy, implement its corresponding EventHandler class and then call 
```csharp
EnemySkinRegistry.RegisterEnemyEventHandler(EnemyAI enemyInstanceAIComponent, EnemyEventHandler myEventHandler);
```

in your Skinner's Apply method, then call
```csharp
EnemySkinRegistry.RemoveEnemyEventHandler(EnemyAI enemyInstanceAIComponent);
```

in your Skinner's Remove method.

Only one handler per instance may be registered at a time.

### Registering a Modded Enemy

The only thing that needs to be done to add a modded enemy to the registry is a call to RegisterEnemy
```csharp
RegisterEnemy(string enemyId, string label);
```
The enemy id is the unique Id of the modded enemy type. Again, it is recommended to follow the pattern <ModAuthorName>.<ModdedEnemyType> to ensure that this id is unique even when other mods are installed. The label is how it will appear in the GUI.

From there, in your enemy's implementation, probably in its start method or at some point when it's spawning, call:
```csharp
Skin randomSkin = EnemySkinRegistry.PickSkin(myModdedEnemyId);
EnemySkinRegistry.ApplySkin(randomSkin, myModdedEnemyId, enemyGameObject);
```

For vanilla enemies, "enemyGameObject" is the game object that contains the EnemyAI component, but you can put whatever suits your fancy as long as anyone trying to change the appearance of your enemy can reasonably get at what they need to change the appearance of your modded enemy instance. If you do go against this convention, make sure anyone trying to make a skin mod for your enemy is aware of this.

Skins also have the option to register EventHandler implementations for enemies during the call to apply. To get a reference to the EventHandler for one of your modded enemy instances, call
```csharp
EnemySkinRegistry.GetEnemyEventHandler(enemyGameObject);
```
This will be null if no handler was registered or if the skin was removed.

Here, enemyGameObject is the same game object used in the call to Apply. All EnemyEventHandlers come with a handful of common events (Spawn, Hit, Die, Stunned etc.). These common events are handled by the patcher inculded in this mod. However, you can give other modders more functionality by extending the EnemyEventHandler interface with events epecific to your enemy.
```csharp
interface MyEnemyEventHandler : EnemyEventHandler()
{
    void OnDoThing(ModdedEnemyAI enemyAI, <OtherParams>);
    ...
}
```
and then in your enemy logic:
```csharp
class MyEnemy : EnemyAI
{
    //whoa, this is some snazzy custom enemy code

    void DoThing()
    {
        ...
        (EnemySkinRegistry.GetEnemyEventHandler(this) as MyEnemyEventHandler)?.OnDoThing(this);
        ...
    }

    //golly! even more immaculate enemy code!
}
```
This way, modders can have their skins do something in response to your enemy doing "Thing."

### Registering Modded Moons

To add your modded moon to be configurable in the menu, just call
```csharp
EnemySkinRegistry.RegisterMoon(string planetName, string configLabel, DefaultMapConfigEntry[] defaultConfig = null);
```
Where "planetName" must match the field of the same name in your moon's SelectableLevel object. The field "configLabel" is how your moon will appear in the GUI. For the vanilla moons, I omitted the number, but you're free to do whatever pleases you.
"defaultConfig" is an optional field which lets a moon author supply a default skin configuration for their moon, allowing for modded moons to match skins to their theme by default.

```csharp
EnemySkinRegistry.RegisterMoon
(
	"13 Buck",
	"Buck",
	new DefaultMapConfigEntry[] 
	{
		new DefaultMapConfigEntry
		(
			//id of the enemy
			EnemySkinRegistry.THUMPER_ID,
			//vanilla frequency 
			0.0f,
			//skins
			new SkinConfigEntry[] 
			{
				new SkinConfigEntry(1.0f, "antlershed.ReindeerThumper"),
				new SkinConfigEntry(0.2f, "antlershed.RedNoseReindeerThumper"),
			}
		),
		new DefaultMapConfigEntry
		(
			EnemySkinRegistry.EYELESS_DOG_ID, 
			0.5f, 
			new SkinConfigEntry[]
			{
				new SkinConfigEntry(1.0f, "antlershed.BlindElk"),
			}
		)
	}
);
```

So on my deer-themed moon "13 Buck," the vanilla thumper would not appear. Instead, the regular reindeer thumper would spawn most of the time (10/11), and the red-nosed reindeer thumper would spawn pretty infrequently (1/11). The elk dog would spawn 2/3 of the time and the vanilla blind dog would spawn 1/3 of the time.

##To-Dos
- Add more documentation
