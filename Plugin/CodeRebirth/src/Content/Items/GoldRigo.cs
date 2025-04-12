using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class GoldRigo : GrabbableObject
{
    public SmallRigoManager smallRigoManager = null!;

    public override void Start()
    {
        base.Start();
        // When activating the SmallRigo's, warp their agent somewhere nearby.
        // When player drops the GoldRigo, the SmallRigo's grab it and take it to where it was spawned, and they rest there until deactivated.
        // Depending on what phase the game is in and what they're doing, either their agent is disabled temporarily, or they're hidden.
    }

    public override void EquipItem()
    {
        base.EquipItem();
        if (!IsServer)
            return;

        GameObject newGameObject = Instantiate(this.gameObject);
        newGameObject.AddComponent<FallingObjectBehaviour>();
        Destroy(newGameObject.GetComponent<GoldRigo>());
        newGameObject.transform.localScale = new Vector3(10f, 10f, 10f);
        newGameObject.transform.position = new Vector3(1000, 1000, 1000);
        CodeRebirthUtils.Instance.CreateFallingObject<FallingObjectBehaviour>(newGameObject, playerHeldBy.transform.position + Vector3.up * 250f, playerHeldBy.transform.position, 10f);
    }
}