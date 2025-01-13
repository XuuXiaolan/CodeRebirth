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
        if (!__instance.NetworkObject.IsSpawned || __instance.NetworkObject.gameObject.layer != 21) return;
        Transporter.objectsToTransport.Add(__instance.NetworkObject.gameObject);
    }

    [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkDespawn)), HarmonyPostfix]
    static void OnNetworkDespawnPatch(NetworkBehaviour __instance)
    {
        if (__instance.NetworkObject.gameObject.layer != 21) return;

        if (Transporter.objectsToTransport.Contains(__instance.NetworkObject.gameObject)) Transporter.objectsToTransport.Remove(__instance.NetworkObject.gameObject);
    }
}