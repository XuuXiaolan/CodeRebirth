using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;

namespace CodeRebirth.src.Content.Items;
public class Money : GrabbableObject
{
    public override void Start()
    {
        base.Start();
        if(!IsHost) return;

        int minBaseValue = Plugin.ModConfig.ConfigMinCoinValue.Value;
        int maxBaseValue = Plugin.ModConfig.ConfigMaxCoinValue.Value;
        if (minBaseValue > maxBaseValue)
        {
            minBaseValue = maxBaseValue;
        }
        
        // This isn't the best solution but :3
        NetworkObject.OnSpawn(() => {
            int value = UnityEngine.Random.Range(minBaseValue, maxBaseValue);
            SetScrapValue(value);
            SetMoneyValueClientRpc(value);
        });
    }

    [ClientRpc]
    private void SetMoneyValueClientRpc(int value)
    {
        if(IsHost) return;
        SetScrapValue(value);
    }
}