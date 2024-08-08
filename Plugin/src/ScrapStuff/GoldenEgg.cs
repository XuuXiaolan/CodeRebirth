using System.Collections;
using CodeRebirth.EnemyStuff;
using CodeRebirth.Util.Spawning;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class GoldenEgg : GrabbableObject {
    public bool mommyAlive = true;
    public bool spawnedOne = false;
    public override void Start() {
        base.Start();
        CodeRebirthUtils.goldenEggs.Add(this);
        grabbableToEnemies = false;
        if (IsHost) {
            StartCoroutine(RandomChanceOfHatching());
        }
    }

    private IEnumerator RandomChanceOfHatching() {
        while (mommyAlive && !spawnedOne) {
            yield return new WaitForSeconds(3f);
            if (UnityEngine.Random.Range(1, 100) <= 10) {
                // make it a cracked egg, same egg but just animate it to be cracked
                var newGoose = RoundManager.Instance.SpawnEnemyGameObject(this.transform.position + this.transform.forward, this.transform.rotation.y, -1, EnemyHandler.Instance.PjonkGoose.PjonkGooseEnemyType);
                SpawnedStuffServerRpc(newGoose);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnedStuffServerRpc(NetworkObjectReference go) {
        SpawnedStuffClientRpc(go);
    }

    [ClientRpc]
    public void SpawnedStuffClientRpc(NetworkObjectReference go) {
        go.TryGet(out NetworkObject netObj);
        netObj.gameObject.transform.localScale *= 0.75f;
        spawnedOne = true;
    }
}