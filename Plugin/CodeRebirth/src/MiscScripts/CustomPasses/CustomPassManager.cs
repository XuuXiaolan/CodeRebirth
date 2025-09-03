using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util;
using Dawn.Utils;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.src.MiscScripts.CustomPasses;
public class CustomPassManager
{
    public enum CustomPassType
    {
        SeeThroughEnemies,
        SeeThroughItems,
        SeeThroughPlayers,
        SeeThroughHazards,
    }

    private Dictionary<CustomPassType, CustomPass> _customPasses = new Dictionary<CustomPassType, CustomPass>();

    private static CustomPassManager? _instance;
    public static CustomPassManager Instance
    {
        get
        {
            _instance ??= new CustomPassManager();

            return _instance;
        }
    }

    private CustomPassVolume? _volume;
    public CustomPassVolume Volume
    {
        get
        {
            if (_volume == null)
            {
                _volume = GameNetworkManager.Instance.localPlayerController.gameplayCamera.gameObject.AddComponent<CustomPassVolume>();
                ConfigureVolume();
            }

            return _volume;
        }
    }

    private void ConfigureVolume()
    {
        if (_volume == null) return;

        _volume.targetCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        _volume.injectionPoint = CustomPassInjectionPoint.BeforeTransparent;
        _volume.isGlobal = true;
    }

    private void InitCustomPass(CustomPassType type)
    {
        if (HasCustomPass(type)) return;

        CustomPass? customPass = null;
        switch (type)
        {
            case CustomPassType.SeeThroughEnemies:
                {
                    var seeThrough = new SeeThroughCustomPass();
                    seeThrough.clearFlags = UnityEngine.Rendering.ClearFlag.None;
                    seeThrough.seeThroughLayer = MoreLayerMasks.EnemiesMask;
                    Color darkRed = Color.red;
                    Color lightRed = new(204 / 255f, 0 / 255f, 0 / 255f, 0.8f);
                    seeThrough.ConfigureMaterial(darkRed, lightRed, 0.04f);
                    customPass = seeThrough;
                    break;
                }
            case CustomPassType.SeeThroughItems:
                {
                    var seeThrough = new SeeThroughCustomPass();
                    seeThrough.clearFlags = UnityEngine.Rendering.ClearFlag.None;
                    seeThrough.seeThroughLayer = MoreLayerMasks.PropsMask;
                    Color darkGreen = new(46 / 255f, 111 / 255f, 64 / 255f, 0.9f);
                    Color lightGreen = new(168 / 255f, 220 / 255f, 171 / 255f, 0.5f);
                    seeThrough.ConfigureMaterial(darkGreen, lightGreen, 0.04f);
                    customPass = seeThrough;
                    break;
                }
            case CustomPassType.SeeThroughPlayers:
                {
                    var seeThrough = new SeeThroughCustomPass();
                    seeThrough.clearFlags = UnityEngine.Rendering.ClearFlag.None;
                    seeThrough.seeThroughLayer = MoreLayerMasks.PlayersAndRagdollMask;
                    Color darkBlue = new(0 / 255f, 51 / 255f, 102 / 255f, 0.9f);
                    Color lightBlue = new(0 / 255f, 76 / 255f, 153 / 255f, 0.6f);
                    seeThrough.ConfigureMaterial(darkBlue, lightBlue, 0.04f);
                    customPass = seeThrough;
                    break;
                }
            case CustomPassType.SeeThroughHazards:
                {
                    var seeThrough = new SeeThroughCustomPass();
                    seeThrough.clearFlags = UnityEngine.Rendering.ClearFlag.None;
                    seeThrough.seeThroughLayer = MoreLayerMasks.HazardMask;
                    Color darkRed = Color.red;
                    Color lightRed = new(204 / 255f, 0 / 255f, 0 / 255f, 0.8f);
                    seeThrough.ConfigureMaterial(darkRed, lightRed, 0.04f);
                    customPass = seeThrough;
                    break;
                }
            default: break;
        }

        if (customPass != null)
        {
            _customPasses.Add(type, customPass);
            Volume.customPasses.Add(customPass);
        }
    }

    public void RemoveCustomPass(CustomPassType type)
    {
        Volume.customPasses.Remove(_customPasses[type]);
        _customPasses.Remove(type);
    }

    public bool HasCustomPass(CustomPassType type)
    {
        return _customPasses.ContainsKey(type);
    }

    public CustomPass? CustomPassOfType(CustomPassType type)
    {
        return _customPasses.GetValueOrDefault(type);
    }

    public bool IsCustomPassEnabled(CustomPassType type)
    {
        return _customPasses.ContainsKey(type) ? _customPasses[type].enabled : false;
    }

    public CustomPass? EnableCustomPass(CustomPassType type, bool enable = true)
    {
        if (!enable && !HasCustomPass(type)) return null;

        InitCustomPass(type); // ensure it exists
        _customPasses[type].enabled = enable;

        return _customPasses[type];
    }

    public void CleanUp()
    {
        for (int i = _customPasses.Count - 1; i >= 0; i--)
            RemoveCustomPass(_customPasses.ElementAt(i).Key);

        UnityEngine.Object.DestroyImmediate(_volume);
    }
}