using CodeRebirth.Misc;
using CodeRebirth.src;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class Money : GrabbableObject {
    public override void Start() {
        base.Start();
        
        if(!IsHost) return;
        
        // This isn't the best solution but :3
        NetworkObject.OnSpawn(() => {
            int value = Random.Range(itemProperties.minValue, itemProperties.maxValue);
            SetScrapValue(value);
            SetMoneyValueClientRPC(value);
        });
    }

    [ClientRpc]
    void SetMoneyValueClientRPC(int value) {
        if(IsHost) return;
        SetScrapValue(value);
    }
}