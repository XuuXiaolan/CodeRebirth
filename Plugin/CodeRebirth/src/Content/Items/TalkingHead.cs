using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TalkingHead : GrabbableObject
{
    [HideInInspector] public PlayerControllerB? player = null;

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (player == null) return;
        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;
        if (StartOfRound.Instance.shipIsLeaving)
        {
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
    }
}