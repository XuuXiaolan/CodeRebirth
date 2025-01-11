using HarmonyLib;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(NetworkBehaviour))]
static class NetworkBehaviourPatch
{
    [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkSpawn)), HarmonyPostfix]
    static void OnNetworkSpawnPatch(NetworkBehaviour __instance)
    {
        if (__instance.NetworkObject.IsSpawned && __instance.NetworkObject.gameObject.layer != 21) return;

        
        // todo: add to a list
    }
}