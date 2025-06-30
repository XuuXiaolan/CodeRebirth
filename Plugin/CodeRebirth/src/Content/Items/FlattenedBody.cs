using System.Collections;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class FlattenedBody : GrabbableObject
{
    internal NetworkVariable<PlayerControllerReference> _flattenedBodyName = new NetworkVariable<PlayerControllerReference>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(WaitUntilNameSet());
    }

    private IEnumerator WaitUntilNameSet()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.2f));
        if (_flattenedBodyName.Value == null)
            yield break;

        ScanNodeProperties? scannode = this.GetComponentInChildren<ScanNodeProperties>();
        if (scannode == null)
            yield break;

        PlayerControllerB player = _flattenedBodyName.Value;
        scannode.headerText = $"{scannode.headerText} Of {player.playerUsername}";
    }
}