using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class SpikeCollider : MonoBehaviour
{
    public FloorSpikeTrap mainScript = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerControllerB player) || player != GameNetworkManager.Instance.localPlayerController) return;
        mainScript.DamagePlayer(player, transform);
    }
}