using System.Collections.Generic;

namespace CodeRebirth.src.Content.Enemies;
public class Monarch : CodeRebirthEnemyAI
{
    private System.Random monarchRandom = new();
    public static List<Monarch> Monarchs = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Monarchs.Add(this);
        monarchRandom = new(StartOfRound.Instance.randomMapSeed + 69 + Monarchs.Count);

        int randomNumberToSpawn = monarchRandom.Next(2, 5);
        if (!IsServer) return;
        for (int i = 0; i < randomNumberToSpawn; i++)
        {
            RoundManager.Instance.SpawnEnemyGameObject(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 100, default), -1, -1, EnemyHandler.Instance.Monarch.CutieflyEnemyType);
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        Monarchs.Remove(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (Monarchs.Contains(this)) Monarchs.Remove(this);
    }
}