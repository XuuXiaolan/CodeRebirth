using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class Turbulence : CRWeapon
{
    [SerializeField]
    private AudioSource _jetActiveIdleSource = null!;
    [SerializeField]
    private ParticleSystem _jetParticles = null!;

    private bool stuckToGround = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnHitSuccess.AddListener(OnHitSuccessEvent);
        OnEnemyHit.AddListener(OnEnemyHitEvent);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OnHitSuccess.RemoveListener(OnHitSuccessEvent);
        OnEnemyHit.RemoveListener(OnEnemyHitEvent);
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
        RoundManager.Instance.DestroyTreeOnLocalClient(this.transform.position);
        OnHitSuccessServerRpc(this.transform.position, this.transform.rotation);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnHitSuccessServerRpc(Vector3 position, Quaternion rotation)
    {
        OnHitSuccessClientRpc(position, rotation);
    }

    [ClientRpc]
    public void OnHitSuccessClientRpc(Vector3 position, Quaternion rotation)
    {
        this.transform.position = position;
        this.transform.rotation = rotation;
        stuckToGround = true;
        float distance = Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);

        if (distance <= 15)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
        if (!playerHeldBy.IsLocalPlayer())
        {
            return;
        }
        playerHeldBy.externalForceAutoFade += (-playerHeldBy.gameplayCamera.transform.forward) * 150f;
        StartCoroutine(playerHeldBy.waitToEndOfFrameToDiscard());
    }

    public override void OnStartReelup()
    {
        base.OnStartReelup();
        _jetParticles.Play(true);

        PlayJetParticlesServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayJetParticlesServerRpc()
    {
        PlayJetParticlesClientRpc();
    }

    [ClientRpc]
    public void PlayJetParticlesClientRpc()
    {
        if (IsOwner)
            return;

        _jetParticles.Play(true);
    }

    public override void EndWeaponHit(bool success)
    {
        base.EndWeaponHit(success);
        _jetParticles.Stop(true);
        StopJetParticlesServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopJetParticlesServerRpc()
    {
        StopJetParticlesClientRpc();
    }

    [ClientRpc]
    public void StopJetParticlesClientRpc()
    {
        if (IsOwner)
            return;

        _jetParticles.Stop(true);
    }

    public override void StartHeldOverHead()
    {
        base.StartHeldOverHead();
        _jetActiveIdleSource.Play();

        ActivateJetIdleSoundServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateJetIdleSoundServerRpc()
    {
        ActivateJetIdleSoundClientRpc();
    }

    [ClientRpc]
    public void ActivateJetIdleSoundClientRpc()
    {
        if (IsOwner)
            return;

        _jetActiveIdleSource.Play();
    }

    public override void EndHeldOverHead()
    {
        base.EndHeldOverHead();
        _jetActiveIdleSource.Stop();

        DeactivateJetIdleSoundServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeactivateJetIdleSoundServerRpc()
    {
        DeactivateJetIdleSoundClientRpc();
    }

    [ClientRpc]
    public void DeactivateJetIdleSoundClientRpc()
    {
        if (IsOwner)
            return;

        _jetActiveIdleSource.Stop();
    }

    public override void FallWithCurve()
    {
        if (stuckToGround)
            return;

        base.FallWithCurve();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        grabbable = true;
        stuckToGround = false;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (reelingUp && IsServer)
        {
            heldOverHeadTimer.Value += Time.deltaTime;
        }
        ShakeTransform(this.transform, Mathf.Clamp(heldOverHeadTimer.Value * 2f, 0, 20));
    }

    public void ShakeTransform(Transform _transform, float intensity)
    {
        if (intensity <= 0)
            return;

        float offset = Mathf.Clamp(intensity * 0.00025f * UnityEngine.Random.Range(-1, 2), -0.004f, 0.004f);
        _transform.localPosition = new Vector3(_transform.localPosition.x + offset, _transform.localPosition.y + offset, _transform.localPosition.z + offset);
    }
}