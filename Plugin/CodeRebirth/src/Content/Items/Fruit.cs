using Unity.Netcode;

namespace CodeRebirth.src.Content.Items;
public class Fruit : GrabbableObject
{
    [ClientRpc]
    public void SetFruitValueClientRpc(int value)
    {
        SetScrapValue(value);
    }
}