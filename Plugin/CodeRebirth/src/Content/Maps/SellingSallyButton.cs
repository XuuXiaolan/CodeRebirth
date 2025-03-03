using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class SellingSallyButton : MonoBehaviour
{
    public InteractTrigger ButtonTrigger = null!;

    public void Start()
    {
        ButtonTrigger.onInteract.AddListener(OnButtonInteract);
    }

    private void OnButtonInteract(PlayerControllerB player)
    {
        if (SellingSally.Instance == null) return;
        if (player != GameNetworkManager.Instance.localPlayerController) return;

        SellingSally.Instance.PressButtonServerRpc();
    }
}