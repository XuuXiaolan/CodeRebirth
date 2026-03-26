using System.Collections;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class MouseTrap : GrabbableObject
{
    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }
    [field: SerializeField]
    public AudioClip ArmingSound { get; private set; }
    [field: SerializeField]
    public AudioClip SnapSound { get; private set; }

    [field: SerializeField]
    public Animator Animator { get; private set; }

    private static readonly int ArmedHash = Animator.StringToHash("Armed"); // Bool

    private bool _armed = false;
    public void OnTriggerEnter(Collider other)
    {
        // damage enemy or player if _armed
        if (!_armed)
        {
            return;
        }

        if (other.TryGetComponent(out PlayerControllerB player) && player.IsLocalPlayer())
        {
            HandlePlayerInteractionsRpc(player);
        }
        else if (other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && enemyAICollisionDetect.mainScript.IsOwner)
        {
            HandleEnemyInteractionsRpc(new NetworkBehaviourReference(enemyAICollisionDetect.mainScript));
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void HandlePlayerInteractionsRpc(PlayerControllerReference playerControllerReference)
    {
        AudioSource.PlayOneShot(SnapSound);

        _armed = false;
        Animator.SetBool(ArmedHash, false);

        PlayerControllerB player = playerControllerReference;
        player.DamagePlayer(5, true, true, CauseOfDeath.Crushing, 0, false, default);
        StartCoroutine(StunPlayerTemporarily(player));
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void HandleEnemyInteractionsRpc(NetworkBehaviourReference enemyBehaviourReference)
    {
        AudioSource.PlayOneShot(SnapSound);

        _armed = false;
        Animator.SetBool(ArmedHash, false);

        EnemyAI enemyAI = (EnemyAI)(NetworkBehaviour)enemyBehaviourReference;
        enemyAI.HitEnemy(1, null, true, -1);
        StartCoroutine(StunEnemyTemporarily(enemyAI));
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        _armed = true;
        Animator.SetBool(ArmedHash, true);
        AudioSource.PlayOneShot(ArmingSound);

        StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
    }

    public override void EquipItem()
    {
        base.EquipItem();
        if (_armed)
        {
            playerHeldBy.DamagePlayer(5, true, true, CauseOfDeath.Crushing, 0, false, default);

            AudioSource.PlayOneShot(SnapSound);

            Animator.SetBool(ArmedHash, false);
            _armed = false;
        }
    }

    private IEnumerator StunPlayerTemporarily(PlayerControllerB player)
    {
        grabbable = false;
        player.disableMoveInput = true;
        yield return new WaitForSeconds(1f);
        player.disableMoveInput = false;
        grabbable = true;
    }

    private IEnumerator StunEnemyTemporarily(EnemyAI enemy)
    {
        grabbable = false;
        enemy.SetEnemyStunned(true, 2f, null);
        yield return new WaitForSeconds(2f);
        grabbable = true;
    }
}