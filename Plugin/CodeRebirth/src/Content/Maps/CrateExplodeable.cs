using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CrateExplodeable : MonoBehaviour, IExplodeable
{
    [field: SerializeField]
    public ItemCrate ItemCrate { get; private set; }

    public void OnExplosion(int force, Vector3 explosionPosition, float distanceToExplosion)
    {
        if (ItemCrate.burned)
        {
            return;
        }

        ItemCrate.DoBurningServerRpc();
    }
}