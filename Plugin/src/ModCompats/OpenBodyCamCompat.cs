using System.Runtime.CompilerServices;
using CodeRebirth.src.Content.Unlockables;
using UnityEngine;
using OpenBodyCams;

namespace CodeRebirth.src.ModCompats;

public static class OpenBodyCamCompatibilityChecker
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Zaggy1024.OpenBodyCams"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        Plugin.ExtendedLogging("No way openbodycams is on?!");
        Plugin.OpenBodyCamsIsOn = true;
        InitializeImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void InitializeImpl()
    {
        BodyCamComponent.BeforeTargetChangedToTransform += OverrideAttachmentPoint;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static bool OverrideAttachmentPoint(MonoBehaviour bodyCam, Transform target, ref Transform attachmentPoint, ref Vector3 offset, ref Quaternion angle, ref Renderer[] renderersToHide)
    {
        if (target.TryGetComponent<ShockwaveGalAI>(out var gal))
        {
            attachmentPoint = gal.DroneHead;
            renderersToHide = gal.renderersToHideIn;
            return true;
        }
        return false;
    }
}