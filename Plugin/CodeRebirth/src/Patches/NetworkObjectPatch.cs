using System.Linq;
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

        int priority = 5;
        if (__instance.NetworkObject.gameObject.name.EndsWith("BearTrap"))
        {
            priority = 1;
        }
        Transporter.objectsWithPriorityToTransport.Add(new GameObjectWithPriority(__instance.NetworkObject.gameObject, priority));
    }

    [HarmonyPatch(nameof(NetworkBehaviour.OnNetworkDespawn)), HarmonyPostfix]
    static void OnNetworkDespawnPatch(NetworkBehaviour __instance)
    {
        if (__instance.NetworkObject.gameObject.layer != 21 || __instance.NetworkObject.CompareTag("DoNotSet"))
            return;

        GameObjectWithPriority gameObjectWithPriority = Transporter.objectsWithPriorityToTransport.FirstOrDefault(kv => kv.gameObject == __instance.NetworkObject.gameObject);
        Transporter.objectsWithPriorityToTransport.Remove(gameObjectWithPriority);
    }
}