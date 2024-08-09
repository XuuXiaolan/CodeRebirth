using System.Collections;
using CodeRebirth.EnemyStuff;
using CodeRebirth.Util.Spawning;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class GoldenEgg : GrabbableObject {
    public bool mommyAlive = false;
    public bool spawnedOne = false;
    public float motherlyFactor = 1f;
    public override void Start() {
        base.Start();
        grabbableToEnemies = false;
        if (IsHost) {
            StartCoroutine(RandomChanceOfHatching());
        }
    }

    private IEnumerator RandomChanceOfHatching() {
        while (mommyAlive && !spawnedOne && !this.isInShipRoom) {
            yield return new WaitForSeconds(5f);
            if (UnityEngine.Random.Range(1f, 100f) <= 2.5f * motherlyFactor) {
                // make it a cracked egg, same egg but just animate it to be cracked
                var newGoose = RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 5f), this.transform.rotation.y, -1, EnemyHandler.Instance.PjonkGoose.PjonkGooseEnemyType);
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
        netObj.gameObject.transform.localScale *= 0.75f * this.transform.localScale.x;
        this.transform.localScale = new Vector3(0.75f, 1f, 0.75f) * this.transform.localScale.x;
        this.originalScale = this.transform.localScale;
        spawnedOne = true;
    }

    public override void Update()
    {
        base.Update();
        if (this.isHeld && this.playerHeldBy != null && this.playerHeldBy.playerSteamId == 76561198217661947) {
            motherlyFactor = 4f;
        } else {
            motherlyFactor = 1f;
        }
    }
}