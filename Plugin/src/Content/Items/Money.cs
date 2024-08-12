using System;
using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;

namespace CodeRebirth.src.Content.Items;
public class Money : GrabbableObject {
    public override void Start() {
        base.Start();
        int baseValue = Math.Clamp(Plugin.ModConfig.ConfigAverageCoinValue.Value, 10, 1000);
        if(!IsHost) return;
        
        // This isn't the best solution but :3
        NetworkObject.OnSpawn(() => {
            int value = UnityEngine.Random.Range(baseValue - 10, baseValue + 10);
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