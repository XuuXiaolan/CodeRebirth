using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemySpawner : MonoBehaviour
{
    public Transform[] enemySpawnTransforms = [];

    public EnemyType? ChooseRandomWeightedEnemyType()
    {
        var validEnemies = StartOfRound.Instance.currentLevel.OutsideEnemies.Where(x => x.rarity > 0).ToList();

        float cumulativeWeight = 0;
        var cumulativeList = new List<(EnemyType, float)>(validEnemies.Count);
        for (int i = 0; i < validEnemies.Count; i++)
        {
            cumulativeWeight += validEnemies[i].rarity;
            cumulativeList.Add((validEnemies[i].enemyType, cumulativeWeight));
        }

        // Get a random value in the range [0, cumulativeWeight).
        float randomValue = UnityEngine.Random.Range(0, cumulativeWeight);
        EnemyType? selectedEnemy = null;

        foreach (var (enemy, cumWeight) in cumulativeList)
        {
            if (randomValue < cumWeight)
            {
                selectedEnemy = enemy;
                break;
            }
        }

        if (selectedEnemy == null)
        {
            Plugin.ExtendedLogging($"Could not find a valid enemy to spawn!");
            return null;
        }
        return selectedEnemy;
    }
}