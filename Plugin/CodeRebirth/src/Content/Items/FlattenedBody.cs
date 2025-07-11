using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class FlattenedBody : GrabbableObject
{
    internal PlayerControllerB? _flattenedBodyName = null;

    [SerializeField]
    private ScanNodeProperties _scanNodeProperties = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
            return;

        StartCoroutine(WaitUntilNameSet());
    }

    private IEnumerator WaitUntilNameSet()
    {
        yield return null;
        yield return null;
        if (_flattenedBodyName == null)
            yield break;

        ChangeScanNodeNameServerRpc($"{_scanNodeProperties.headerText} Of {_flattenedBodyName.playerUsername}");
    }

    [ServerRpc]
    private void ChangeScanNodeNameServerRpc(string newName)
    {
        ChangeScanNodeNameClientRpc(newName);
    }

    [ClientRpc]
    private void ChangeScanNodeNameClientRpc(string newName)
    {
        _scanNodeProperties.headerText = newName;
    }
}