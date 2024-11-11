# LethalModDataLib

*A library for mod data saving & loading.*

[![Build](https://img.shields.io/github/actions/workflow/status/MaxWasUnavailable/LethalModDataLib/build.yml?style=for-the-badge&logo=github&branch=master)](https://github.com/MaxWasUnavailable/LethalModDataLib/actions/workflows/build.yml)
[![Latest Version](https://img.shields.io/thunderstore/v/MaxWasUnavailable/LethalModDataLib?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/MaxWasUnavailable/LethalModDataLib)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/MaxWasUnavailable/LethalModDataLib?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/MaxWasUnavailable/LethalModDataLib)
[![NuGet Version](https://img.shields.io/nuget/v/MaxWasUnavailable.LethalModDataLib?style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/MaxWasUnavailable.LethalModDataLib)

## What is this?

This library provides a standardised way to save and load persistent data for mods. It is designed to be easy to use and
flexible, offering multiple different ways to interact with the system, depending on your needs.

Data is saved in `.moddata` files, which are stored in the same location as the vanilla save files. Instead of having a
single file, or a file per mod, the library has a file for each save file, and a file for general data - essentially
mimicking the vanilla save system. This ensures that mods do not pollute the vanilla save files. The library makes use
of ES3 to handle the actual saving and loading of the data, which should be compatible with most Unity types.

When saving and loading data through the library, keys are automatically generated based on your mod's GUID and assembly
information (depending on the approach used - see below). This ensures that your data does not conflict with other mods'
data, and that it is easy to find and debug.

### File structure

```
ZeekerssRBLX
    └── Lethal Company
        ├── LCGeneralSaveData
        ├── LCGeneralSaveData.moddata
        ├── LCSaveFile1
        ├── LCSaveFile1.moddata
        ├── LCSaveFile2
        ├── LCSaveFile2.moddata
        ├── LCSaveFile3
        └── LCSaveFile3.moddata
```

As you can see, there is a `.moddata` file for each vanilla save file, including the general save file. Mods do not
have individual `.moddata` files, and do not touch the vanilla save files.

### Supported types

See [Easy Save 3's documentation](https://docs.moodkie.com/easy-save-3/es3-guides/es3-supported-types/) for a list of
supported types. In general, most Unity types are supported, as well as custom classes and structs that are
serializable.

## Usage

There are 3 ways to use this library. They can all be used in the same project, with some caveats.

### 1. Using the `ModData` attribute

This is the easiest and most automated way to use the library. Unless you need to manually handle saving and loading,
this is the way to go. Note that this method still allows you to manually handle saving and/or loading if you need to,
so you are not limited to the automated part.

Depending on the attribute configuration, the library will take care of saving and loading data for you, in a way that
is seamless and "invisible" / does not require you to add any additional code beyond the attribute.

The `ModData` attribute can be used to mark fields & properties that should be saved and loaded through the handler's
event hooks. When applied to **static** fields or properties, the attribute will automatically register the class with
the ModDataHandler, and the data will be saved and loaded depending on the attribute's parameters. When applied to
**non-static** fields or properties, the attribute will be ignored unless you register the class' instance with the
ModDataHandler through the `RegisterInstance` method.

The ModData handler will save the **original** value of your field or property, and use this when no mod data exists
when loading a save file. This ensures you don't need to manually reset a value whenever a player would switch saves,
or when a new save is created. If you wish to reset a value when a game over happens, you can use the `ResetWhen`
parameter.

This is the attribute's constructor signature:

```csharp
public ModDataAttribute(SaveWhen saveWhen, LoadWhen loadWhen, SaveLocation saveLocation, string? baseKey = null)
```

These are options for its 4 parameters:

- `SaveWhen` (enum) - When the data should be saved
    - `Manual` - Manually handled by you, the modder
    - `OnAutoSave` - When the game is autosaved (= Whenever the ship returns to orbit)
    - `OnSave` - When the game is saved (Most frequent - also called by autosaves)
- `LoadWhen` (enum) - When the data should be loaded
    - `Manual` - Manually handled by you, the modder
    - `OnLoad` - When a save file is loaded, right after all vanilla loading is done
    - `OnRegister` - When the attribute is registered, as soon as possible
- `SaveLocation` (enum) - Where the data should be saved
    - `GeneralSave` - In a .moddata file that fulfills the same purpose as vanilla's LCGeneralSaveData file
    - `CurrentSave` - In a .moddata file that is specific to the current save file
- `ResetWhen` (enum) - When the data should be reset
    - `Manual` - Manually handled by you, the modder
    - `OnGameOver` - When a game over happens (quota not reached, ship reset)
- `BaseKey` - **Strongly recommended to leave default unless you know what you're doing** - The base key for the data.
  This is used to create the key for the field in the .moddata file. If not set, the library will sort this out. In
  general, you should not need to set this unless you are e.g. trying to access the data from another mod which is not
  enabled. Note that using the same base key for multiple fields will very likely cause unexpected behaviour. (If you
  do want to use the same key as a currently enabled mod, for some case I can't imagine, you should be using the
  `GetModDataKey` method in `ModDataHelper` to fetch its information).

> [!IMPORTANT]
>
> To manually trigger saving & loading of an attribute-marked field or property, you can use the `SaveLoadHandler`
> class' `SaveData` and `LoadData` methods, using an `IModDataKey` object. This can be fetched using the
> `GetModDataKey` method in `ModDataHelper`.

The ModData attribute can be used on fields and properties, both static and instanced ones, as well as public, private
and internal ones.

> [!TIP]
>
> Remember that non-static (instanced) fields and properties with the ModData attribute will be ignored unless you
> register the
> class' instance with the ModDataHandler through the `RegisterInstance` method. De-registering an instance is done
> through the `DeRegisterInstance` method.

> [!TIP]
>
> Example instanced usage:

```csharp
public class SomeClass
{
    [ModData(SaveWhen.OnSave, LoadWhen.OnLoad, SaveLocation.GeneralSave)]
    private int __someInt;
    
    [ModData(SaveWhen.OnAutoSave, LoadWhen.OnLoad, SaveLocation.CurrentSave)]
    public string SomeString { get; set; } = "SomeDefaultValue";
    
    [ModData(SaveWhen.Manual, LoadWhen.OnLoad, SaveLocation.GeneralSave)]
    private float __someFloat;
    
    // Some method in which we manually handle __someFloat's saving, since its attribute is set to SaveWhen.Manual
    private void SomeMethod()
    {
        // (...)
        
        SaveLoadHandler.SaveData(ModDataHelper.GetModDataKey(this, nameof(__someFloat)));
        
        // Note that we can also force a save or load of automated fields/properties:
        SaveLoadHandler.LoadData(ModDataHelper.GetModDataKey(this, nameof(SomeString)));
        
        // This might be useful to instantiate values for instances that may be null when the OnLoad event is called.
        if (string.IsNullOrEmpty(SomeString))
        {
            // (...)
        }
        
        // (...)
    }
}

// In some other class
public class SomeOtherClass
{
    private SomeClass __someClass;
    
    public SomeOtherClass()
    {
        __someClass = new SomeClass();
        
        ModDataHandler.RegisterInstance(someClass, "someInstanceName"); // Register an instance of SomeClass with the ModDataHandler
    }
}
```

> [!TIP]
>
> Example static usage:

```csharp
public class SomeClass
{
    [ModData(SaveWhen.OnSave, LoadWhen.OnLoad, SaveLocation.GeneralSave)]
    private static int __someInt;
    
    [ModData(SaveWhen.Manual, LoadWhen.OnLoad, SaveLocation.CurrentSave)]
    public static string SomeString { get; set; } = "SomeDefaultValue";
    
    public void SomeMethod()
    {
        // (...)
        
        SaveLoadHandler.SaveData(ModDataHelper.GetModDataKey(typeof(this), nameof(SomeString))); // Note the use of typeof(this) instead of this
        
        // (...)
    }
}

public class SomeOtherClass
{
    // (...)
    
    public void SomeOtherMethod()
    {
        // (...)
        
        SaveLoadHandler.SaveData(ModDataHelper.GetModDataKey(typeof(SomeClass), nameof(SomeClass.SomeString))); // Note the use of typeof(SomeClass)
        
        // (...)
    }
    
    // (...)
}
```

> [!WARNING]
>
> When using the Manual parameter for saving and/or loading, you **need** to use the methods that take an IModDataKey as
> parameter. This is because the other save/load methods will result in a different key being used (unless you go
> through the unnecessary trouble of finding out the key yourself). Not doing this will cause your data to be saved and
> loaded in a different location, unless you're handling it manually entirely - in which case you don't need the
> attribute in the first place.

### 2. Using the `ModDataContainer` abstract class

This way of using the library requires you to set up a class that inherits from `ModDataContainer`. Any fields or
properties in this class will be saved and loaded automatically, without the need for any attributes. You are
essentially creating a "container" for your mod data.

The ModDataContainer class has a number of properties and methods that you can override to customize its behavior:

- Properties:
    - `SaveLocation` - Where the data should be saved. Defaults to `SaveLocation.CurrentSave`
    - `OptionalPrefixSuffix` - A string that will be appended to the prefix for keys of fields in the container. This is
      useful in case you want to have different instances of the same container in the same save file; for example a
      container per player. Defaults to `string.Empty`
- Methods:
    - `GetPrefix` - **Strongly recommended to leave default unless you know what you're doing** - Returns the prefix for
      keys of fields in the container. Defaults to the assembly name and the class name, separated by a dot. (
      e.g. `MyMod.MyContainer`). If `OptionalPrefixSuffix` is set, it will be appended to the prefix like
      so: `MyMod.MyContainer.MyOptionalPrefixSuffix`
    - `Save` - **Strongly recommended to leave default unless you know what you're doing** - Saves the data in the
      container. Should be called by the modder when the data should be saved.
    - `Load` - **Strongly recommended to leave default unless you know what you're doing** - Loads the data in the
      container. Should be called by the modder when the data should be loaded.
    - Pre/PostSave/Load - Methods that are called before and after the saving and loading of the container's data. Can
      be used to perform additional operations, such as logging or data validation.

There is also an additional attribute that can be used to mark fields or properties as ignored by the container:

```csharp
public ModDataIgnoreAttribute(IgnoreFlags ignoreFlags = IgnoreFlags.None)
```

The `IgnoreFlags` enum has the following options:

- `None` - No flags. Completely ignore the field or property.
- `OnSave` - Ignore the field or property when saving.
- `OnLoad` - Ignore the field or property when loading.
- `IfNull` - Ignore the field or property if it is null.
- `IfDefault` - Ignore the field or property if it is the default value for its type.

> [!TIP]
>
> Example usage:

```csharp
public class SomeContainer : ModDataContainer
{
    private int __someInt;
    public string SomeString { get; set; } = "SomeDefaultValue";
    [ModDataIgnore(IgnoreFlags.IfDefault)]
    private float __someFloat;
    private List<int> __someList;
    
    // Use the constructor to set the OptionalPrefixSuffix, so we can have multiple instances of this container without them overwriting each other
    public SomeContainer(string name)
    {
        OptionalPrefixSuffix = name;
    }
    
    // Override the PostLoad method to ensure that the list is not null
    protected override void PostLoad()
    {
        if (__someList == null)
        {
            __someList = new List<int>();
        }
    }
}

// In some other class
public class SomeClass
{
    private SomeContainer __container;
    
    public SomeClass()
    {
        __container = new SomeContainer("SomeName"); // Create a new instance of the container
        __container.Load(); // Load the container's data, if any exists
    }
    
    // Some method in which we manually handle saving the container's data
    private void SomeMethod()
    {
        // (...)
        
        __container.Save(); // Save the container's data
        
        // (...)
    }
}
```

> [!WARNING]
>
> Note: You should **not** use the ModData attribute on (static) fields or properties in a class that inherits from
> ModDataContainer. This will cause the fields to be saved/loaded twice, once by the container and once by the
> attribute. Additionally, the keys for the fields will be different, which can cause inconsistencies depending on when
> the data is saved and loaded. When used on non-static fields or properties, the attribute will be ignored unless you
> register the class' instance with the ModDataHandler, which is also not recommended for the same reasons.

### 3. Using the `SaveLoadHandler` save & load methods

This is the "good old" manual way of saving and loading data. You can use the `SaveLoadHandler` class' methods to
manually handle saving and loading of data. This is useful if you need to save and load data in a way / at a time that
is not covered by the other options, or if you want to build your own handler for saving and loading.

The `SaveLoadHandler` class has a SaveData & LoadData method, with two public signatures each:

```csharp
// The recommended method to use for manual saving.
// It is recommended to leave autoAddGuid as true, since this will automatically add your mod's guid to the key; preventing conflicts with other mods.
public static bool SaveData<T>(T? data, string key, SaveLocation saveLocation = SaveLocation.CurrentSave, bool autoAddGuid = true)
    
// For usage with the SaveWhen.Manual attribute parameter. You will need to fetch the IModDataKey object for the field or property you want to save.
// This can be done using the GetModDataKey method in ModDataHelper.
// Note: This will save the data from the field/property, rather than requiring you to pass a value through the method.
public static bool SaveData(IModDataKey modDataKey)
```

```csharp
// The recommended method to use for manual loading.
// It is recommended to leave autoAddGuid as true, since this will automatically add your mod's guid to the key; preventing conflicts with other mods.
public static T? LoadData<T>(string key, T? defaultValue = default, SaveLocation saveLocation = SaveLocation.CurrentSave, bool autoAddGuid = true)
    
// For usage with the LoadWhen.Manual attribute parameter. You will need to fetch the IModDataKey object for the field or property you want to load.
// This can be done using the GetModDataKey method in ModDataHelper.
// Note: This will load the data into the field/property, rather than requiring you to assign the value returned by the method.
public static bool LoadData(IModDataKey modDataKey)
```

> [!TIP]
>
> Example usage:

```csharp
public class SomeClass
{
    private int __someInt;
    private string SomeString { get; set; };
    
    [ModData(SaveWhen.Manual, LoadWhen.Manual, SaveLocation.GeneralSave)]
    private float __someFloat;
    
    // Some method in which we manually handle saving __someInt
    private void SomeMethod()
    {
        // (...)
        
        SaveLoadHandler.SaveData(__someInt, "SomeIntKey");
        
        // (...)
    }
    
    // Some method in which we manually handle loading __someString
    private void SomeOtherMethod()
    {
        // (...)
        
        SomeString = SaveLoadHandler.LoadData<string>("SomeStringKey", "SomeDefaultValue");
        
        // (...)
    }
    
    // Some method in which we manually handle saving __someFloat
    private void YetAnotherMethod()
    {
        // (...)
        
        SaveLoadHandler.SaveData(ModDataHelper.GetModDataKey(this, nameof(__someFloat)));
        
        // (...)
    }
    
    // Some method in which we manually handle loading __someFloat
    private void AndAnotherMethod()
    {
        // (...)
        
        SaveLoadHandler.LoadData(ModDataHelper.GetModDataKey(this, nameof(__someFloat)));
        
        // (...)
    }
}
```

## Tips

- The library automatically removes the paired .moddata file when a save is deleted, so handle this accordingly in your
  mod. (e.g. by hooking into the `PostDeleteFileEvent` event from `LethalEventsLib`)
- Validate your data after loading, if you expect it to be in a certain state. If a value is missing when it is loaded,
  it will be set to the type's `default` value (0, null, etc...). This can be done in e.g. the `PostLoad` method of a
  `ModDataContainer` or in the method that loads the data. For attribute-based saving and loading, it is recommended to
  use properties, and to validate the value in the property's setter.
- Lethal Company sets its current save file to the last selected/loaded save file on game start. Keep this in mind
  if you are using the `SaveLocation.CurrentSave` parameter, and are manually handling saving and/or loading. This is
  not a concern if you are using the attribute without manual handling, if you are using the `SaveLocation.GeneralSave`
  parameter, or if you are saving/loading *after* a save file has been loaded.

## Attribution

<a href="https://www.flaticon.com/free-icon/floppy-disk_482459?term=floppy&page=1&position=1&origin=search&related_id=482459" title="save icons">
Save icons created by Those Icons - Flaticon</a>