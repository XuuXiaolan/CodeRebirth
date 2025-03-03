using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class SellingSallyBell : MonoBehaviour
{
    public InteractTrigger bellTrigger = null!;

    public void Start()
    {
        bellTrigger.onInteract.AddListener(OnBellInteract);
    }

    private void OnBellInteract(PlayerControllerB player)
    {
        if (SellingSally.Instance == null) return;
        if (player != GameNetworkManager.Instance.localPlayerController) return;

        SellingSally.Instance.RingBellServerRpc();
    }
}