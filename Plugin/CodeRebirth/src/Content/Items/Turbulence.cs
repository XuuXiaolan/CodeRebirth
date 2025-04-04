using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Turbulence : CRWeapon
{
    private bool stuckToGround = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnHitSuccess.AddListener(OnHitSuccessEvent);
        OnEnemyHit.AddListener(OnEnemyHitEvent);
    }

    public void OnEnemyHitEvent(EnemyAI enemyAI)
    {
        OnEnemyHitServerRpc(new NetworkBehaviourReference(enemyAI));
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnEnemyHitServerRpc(NetworkBehaviourReference enemyAI)
    {
        OnEnemyHitClientRpc(enemyAI);
    }

    [ClientRpc]
    public void OnEnemyHitClientRpc(NetworkBehaviourReference enemyAI)
    {
        if (enemyAI.TryGet(out EnemyAI enemyAIScript))
        {
            if (enemyAIScript.enemyHP <= 0 || enemyAIScript.isEnemyDead)
            {
                enemyAIScript.transform.localScale = new Vector3(enemyAIScript.transform.localScale.x, enemyAIScript.transform.localScale.y * 0.1f, enemyAIScript.transform.localScale.z);
            }
        }
    }

    public void OnHitSuccessEvent()
    {
        OnHitSuccessServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnHitSuccessServerRpc()
    {
        OnHitSuccessClientRpc();
    }

    [ClientRpc]
    public void OnHitSuccessClientRpc()
    {
        stuckToGround = true;
        float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);

        if (distance <= 15)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
        if (playerHeldBy != GameNetworkManager.Instance.localPlayerController)
        {
            return;
        }
        playerHeldBy.externalForceAutoFade += (-playerHeldBy.gameplayCamera.transform.forward) * 30f * (playerHeldBy.isCrouching ? 0.25f : 1f);
        StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
    }

    public override void FallWithCurve()
    {
        if (stuckToGround) return;
        base.FallWithCurve();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        grabbable = true;
        stuckToGround = false;
    }
}