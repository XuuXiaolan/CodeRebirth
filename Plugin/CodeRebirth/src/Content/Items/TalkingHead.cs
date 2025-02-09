using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TalkingHead : GrabbableObject
{
    public Transform playerBone = null!;
    public MeshRenderer meshRenderer = null!;
    [HideInInspector] public PlayerControllerB? player = null;

    public override void Update()
    {
        base.Update();
        if (player == null) return;
        player.transform.position = playerBone.position;
        player.transform.rotation = transform.rotation;
        if (StartOfRound.Instance.shipIsLeaving && player.IsOwner)
        {
            player.DisablePlayerModel(player.gameObject, true, false);
            player.disableMoveInput = false;
            player.disableInteract = false;
            player.thisPlayerModelLOD2.gameObject.SetActive(true);
            player.thisPlayerModelLOD1.gameObject.SetActive(true);
            
            if (GameNetworkManager.Instance.localPlayerController == player)
            {
                HUDManager.Instance.HideHUD(false);
                player.headCostumeContainer.gameObject.SetActive(true);
                player.headCostumeContainerLocal.gameObject.SetActive(true);
                StartOfRound.Instance.allowLocalPlayerDeath = true;
            }
            Plugin.ExtendedLogging($"Killing talking head.");
            player.KillPlayer(Vector3.zero, false, CauseOfDeath.Snipped, 0);
            player = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncTalkingHeadServerRpc(int playerIndex)
    {
        SyncTalkingHeadClientRpc(playerIndex);
    }

    [ClientRpc]
    private void SyncTalkingHeadClientRpc(int playerIndex)
    {
        player = StartOfRound.Instance.allPlayerScripts[playerIndex];
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            meshRenderer.enabled = false;
        }
    }
}