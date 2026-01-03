MonkeyInjectionLibrary
============
[![GitHub Release](https://img.shields.io/github/v/release/mattymatty97/LTC_MonkeyInjectionLibrary?display_name=release&logo=github&logoColor=white)](https://github.com/mattymatty97/LTC_MonkeyInjectionLibrary/releases/latest)
[![GitHub Pre-Release](https://img.shields.io/github/v/release/mattymatty97/LTC_MonkeyInjectionLibrary?include_prereleases&display_name=release&logo=github&logoColor=white&label=preview)](https://github.com/mattymatty97/LTC_MonkeyInjectionLibrary/releases)  
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/mattymatty/MonkeyInjectionLibrary?style=flat&logo=thunderstore&logoColor=white&label=thunderstore)](https://thunderstore.io/c/lethal-company/p/mattymatty/MonkeyInjectionLibrary/)

A library mod that lets other mods add properties and methods to existing base-game classes at runtime, without modifying game files.

This mod does nothing on its own – it exists to support other mods.

---

## Overview

This library provides an **interface-based monkey-patching system** for Unity games.

Instead of creating your own preloader, mod authors define **interfaces** that describe the methods/properties they want to add.
At runtime, the library injects those interfaces into the target classes using metadata provided by custom attributes.

This approach keeps patches:

* Declarative
* Easier to reason about
* Less fragile across game updates

---

## How It Works

1. Mod authors define one or more interfaces.
2. Attributes are applied to describe:
    * Which Class to inject into
    * How errors should be handled
3. The library scans loaded assemblies marked for injection.
4. Interfaces are injected into the target Unity classes at runtime.

---

## Required Attributes

### `RequiresInjections`

Marks an assembly as containing injectable interfaces.

**This attribute is required** for the library to detect your mod.

**Usage**

* Must be placed at the **assembly level**
* Should be included in **every file that defines injection interfaces**

Without this attribute, your mod will be ignored entirely.

---

### `InjectInterface`

Specifies the base-game class that an interface should be injected into.

**Usage**

* Applied to an interface
* Accepts a `Type` reference or class name of a base-game class

An interface may target **multiple classes** by applying multiple `InjectInterface` attributes.

---


## Error Handling

Injection errors can be handled using the `HandleErrors` attribute.

If no attribute is specified, the default behavior is `Terminate`.

### `HandleErrors`

Defines how the library reacts when an error occurs during injection.

**Usage locations**

* Assembly
* Interface
* Individual property or method

More specific scopes override more general ones.

---

### `ErrorHandlingStrategy`

Available error handling strategies:

* **Terminate** (Default)  
  Immediately stops the injection process when an error occurs.

* **Ignore**  
  Silently ignores errors and continues execution.

* **LogWarning**  
  Logs the error as a warning and continues execution.

* **LogError**  
  Logs the error as an error and continues execution.

---

## Attribute Precedence

When multiple `HandleErrors` attributes are present, the following order applies (highest priority last):

0. Default
1. Assembly
2. Interface
3. Method / Property

The most specific setting always wins.

---

## Intended Use

This library is intended for:

* Advanced mods that need controlled runtime modification of Unity classes

It is **not** intended for direct use by end users.

---

## Notes

* This is a library mod and has no visible effect on gameplay by itself
* Other mods may depend on it
* Removing it may break mods that rely on interface injection


## Example Interface
```C#
using InjectionLibrary;
using InjectionLibrary.Attributes;

[assembly: RequiresInjections]
[assembly: HandleErrors(ErrorHandlingStrategy.Terminate)]

namespace ExampleMod.Interfaces;

[HandleErrors(ErrorHandlingStrategy.LogError)]
[InjectInterface(nameof(StartOfRound))]
public interface ICrashingInterface
{
    void Awake();
    
    [HandleErrors(ErrorHandlingStrategy.LogWarning)]
    void Start();
    
    [HandleErrors(ErrorHandlingStrategy.Ignore)]
    void Update();
}
```
