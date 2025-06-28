using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class FlattenedBody : GrabbableObject
{
    internal NetworkVariable<string> _flattenedBodyName = new NetworkVariable<string>(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(WaitUntilNameSet());
    }

    private IEnumerator WaitUntilNameSet()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.2f));
        if (_flattenedBodyName.Value == string.Empty)
            yield break;

        ScanNodeProperties? scannode = this.GetComponentInChildren<ScanNodeProperties>();
        if (scannode == null)
            yield break;

        scannode.headerText = $"{scannode.headerText} Of {_flattenedBodyName.Value}";
    }
}