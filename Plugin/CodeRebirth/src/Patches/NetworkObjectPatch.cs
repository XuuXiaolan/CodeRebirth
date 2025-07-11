using CodeRebirth.src.Content.Enemies;
using HarmonyLib;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(NetworkBehaviour))]
static class NetworkBehaviourPatch
{
    [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkSpawn)), HarmonyPostfix]
    static void OnNetworkSpawnPatch(NetworkBehaviour __instance)
    {
        if (!__instance.NetworkObject.IsSpawned || __instance.NetworkObject.gameObject.layer != 21 || __instance.NetworkObject.CompareTag("DoNotSet"))
            return;

        Transporter.objectsToTransport.Add(__instance.NetworkObject.gameObject);
    }

    [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkDespawn)), HarmonyPostfix]
    static void OnNetworkDespawnPatch(NetworkBehaviour __instance)
    {
        if (__instance.NetworkObject.gameObject.layer != 21 || __instance.NetworkObject.CompareTag("DoNotSet"))
            return;

        Transporter.objectsToTransport.Remove(__instance.NetworkObject.gameObject);
    }
}