using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class CactusAICollisionDetect : MonoBehaviour
{
    [SerializeField]
    private EnemyAI _mainScript = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerControllerB>() != null)
        {
            _mainScript.OnCollideWithPlayer(other);
        }
        else if (other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && enemyAICollisionDetect.mainScript != this._mainScript)
        {
            _mainScript.OnCollideWithEnemy(other, enemyAICollisionDetect.mainScript);
        }
    }
}