# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Fixed

### Changed

### Deprecated

### Removed

## [1.2.2] - 16/04/2024

### Changed

- Wrapped RegisterModDataAttributes in a try-catch block

## [1.2.1] - 09/04/2024

### Fixed

- Fixed unsafe Attribute finding

### Changed

- If no saved value exists, use the current value when loading ModDataContainer fields/properties

## [1.2.0] - 29/03/2024

### Added

- `ResetWhen` enum and behaviour to handle it
- Storing of original value of fields & properties tagged with `ModDataAttribute`, used for resetting

### Changed

- If a field or property that is tracked by the ModData system is not found in a save while loading, instead of keeping
  the current value, it will now be reset to the original value

## [1.1.0] - 23/03/2024

### Added

- Implemented `OnRegister` LoadWhen enum value and behaviour to handle it.

## [1.0.1] - 23/03/2024

### Fixed

- Fix NuGet publish not working

## [1.0.0] - 23/03/2024

### Added

- Events to replace the need for LethalEventsLib
  - OnSaveData
  - OnLoadData
  - OnDeleteData
  - OnMainMenu
- ModDataConfiguration, as a more future-proof way of configuring the ModData system
- Empty ModDataAttribute constructor that allows for property initialisation
- Ability to trigger a manual "event"(-esque) save/load for data in your mod (essentially triggering what a save/load
  event would do, but without it actually triggering, and without impacting other mods' data)

### Fixed

- Take into account whether or not the client is the lobby host when saving/loading CurrentSave data

### Changed

- Renamed GetIModDataKey to GetModDataKey
- SaveWhen and LoadWhen are now flags
- Some better / more verbose logging for exceptions

### Removed

- Dependency on LethalEventsLib

## [0.0.3] - 01/03/2024

### Added

- Enabled GenerateDocumentationFile
- Documentation across the board for everything that was missing it

### Fixed

### Changed

- Switched to netstandard2.1
- Renamed ModDataHandler.GetModDataKey to ModDataHandler.ToES3KeyString
- Make ModDataHandler.ToES3KeyString an extension method

### Removed

## [0.0.2] - 15/02/2024

### Added

- The API now supports properties across the board
- ModDataAttributes can now be used on non-static fields/properties, provided the class using them is instantiated, and
  registered with the ModDataHandler via `ModDataHandler.RegisterInstance(object instance, string keySuffix = "")`
- De-registration methods for ModData. Can be manually called via `ModDataHandler.DeRegisterInstance(object instance)`
- Warning when a ModDataAttribute is used on a non-static field/property, but the class is not registered with the
  ModDataHandler, and a manual save/load is attempted using the IModDataKey

### Fixed

- Fixed a bug where fields/properties in a ModDataContainer flagged with the ModDataIgnoreAttribute with no IgnoreFlags
  would not be ignored
- Fixed private fields/properties not being accessible by the API

### Changed

- Split up ModDataHandler into ModDataHandler, ModDataAttributeCollector, and SaveLoadHandler
    - ModDataHandler
        - Now only handles the registration of ModDataAttributes, and event hooking & handling
    - ModDataAttributeCollector
        - Now handles the collection of (static field/property) ModDataAttributes, calling the ModDataHandler to
          register them
    - SaveLoadHandler
        - Now handles the actual saving and loading of data
- The API now uses an IModDataKey interface for a single dictionary, rather than having separate field & property
  dictionaries. It also has a ModDataValue as the value type, rather than the ModDataAttribute. This allows me to
  store the relevant information in a unified way (e.g. instance can be null for static fields/properties, or an
  instance for non-static fields/properties)
- Use current value for field or property instead of default type value when loading non-existing data (this should
  prevent issues with default values being replaced with the default type value)

## [0.0.1] - 04/02/2024

### Added

- Initial project setup
    - README
    - CHANGELOG
    - .gitignore
- ModDataAttribute
- ModDataContainer abstract class
- ModDataHandler system
    - SaveData (3 signatures)
    - LoadData (3 signatures)
    - Finding & registration of ModData attributes
    - Hooked into LethalEventsLib's hooks for saving, autosaving, loading, and file deletion
    - Guid / assembly fetching for manual saving/loading
- ModDataHelper
- Enums
    - LoadWhen
    - SaveWhen
    - SaveLocation