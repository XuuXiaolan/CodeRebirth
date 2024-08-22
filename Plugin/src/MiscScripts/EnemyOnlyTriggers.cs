using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemyOnlyTriggers : MonoBehaviour
{
    public EnemyAI mainScript = null!;

    private void OnTriggerEnter(Collider other)
    {

        Transform? parent = TryFindRoot(other.transform);
        if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy) && !enemy.isEnemyDead)
        {
            if (enemy == mainScript) return;
            mainScript.OnCollideWithEnemy(other, enemy);
        }
    }

    public static Transform? TryFindRoot(Transform child)
    {
        if (child.GetComponent<NetworkObject>() != null)
        {
            return child;
        }
        return child.GetComponentInParent<NetworkObject>()?.transform;
    }
}