using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemyOnlyTriggers : MonoBehaviour
{
    public EnemyAI mainScript = null!;

    private void OnTriggerEnter(Collider other)
    {
        Transform? parent = CRUtilities.TryFindRoot(other.transform);
        if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy) && !enemy.isEnemyDead)
        {
            if (enemy == mainScript) return;
            mainScript.OnCollideWithEnemy(other, enemy);
        }
    }
}