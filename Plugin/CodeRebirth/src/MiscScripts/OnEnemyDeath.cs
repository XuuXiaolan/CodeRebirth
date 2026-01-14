using Dawn.Utils;
using Dusk;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class OnEnemyDeath : MonoBehaviour
{
    [SerializeField]
    private EnemyType _enemyType;

    [SerializeField]
    private UnityEvent _onDeath;

    [SerializeReference]
    private DuskAchievementReference _achievementReference;

    public void Awake()
    {
        On.EnemyAI.KillEnemy += CheckWhoKilledEnemy;
    }

    private void CheckWhoKilledEnemy(On.EnemyAI.orig_KillEnemy orig, EnemyAI self, bool destroy)
    {
        orig(self, destroy);

        if (self.enemyType != _enemyType)
            return;

        DawnEnemyAdditionalData additionalEnemyData = DawnEnemyAdditionalData.CreateOrGet(self);
        if (!self.isEnemyDead)
            return;

        _onDeath.Invoke();

        if (!additionalEnemyData.KilledByPlayer || additionalEnemyData.PlayerThatLastHit == null || !additionalEnemyData.PlayerThatLastHit.IsLocalPlayer())
            return;

        _achievementReference.Resolve().TryCompleteAchievement();
    }

    public void OnDestroy()
    {
        On.EnemyAI.KillEnemy -= CheckWhoKilledEnemy;
    }
}