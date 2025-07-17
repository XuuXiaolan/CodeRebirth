using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class DestructibleObject : NetworkBehaviour, IHittable
{
    [Header("References")]
    [SerializeField]
    private Collider[] colliders = [];
    [SerializeField]
    private Renderer[] renderers = [];
    [SerializeField]
    private ParticleSystem[] _particleSystems = [];
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip _destroySound = null!;

    [Header("Settings")]
    [SerializeField]
    private int _playerDamageAmount = 5;
    [SerializeField]
    private float _forceApplied = 5f;

    private bool _isDestructible = false;
    internal Coroutine? _destroyCactiRoutine = null;

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        DestroyDestructibleObjectServerRpc();
        return true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!_isDestructible)
            return;

        if (other.gameObject.TryGetComponent(out PlayerControllerB player) && player.IsLocalPlayer())
        {
            player.DamagePlayer(_playerDamageAmount, true, true, CauseOfDeath.Unknown, 0, false, (player.transform.position - this.transform.position).normalized * _forceApplied);
            DestroyDestructibleObjectServerRpc();
        }
    }

    public void SetDestructible(bool isDestructible)
    {
        _isDestructible = isDestructible;
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyDestructibleObjectServerRpc()
    {
        DestroyDestructibleObjectClientRpc();
    }

    [ClientRpc]
    private void DestroyDestructibleObjectClientRpc()
    {
        DestroyDestructibleObject();
    }

    public void DestroyDestructibleObject()
    {
        if (!_isDestructible)
            return;

        if (Vector3.Distance(this.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) <= 1f)
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(5, true, true, CauseOfDeath.Stabbing, 0, false, default);
        }

        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        foreach (var particleSystem in _particleSystems)
        {
            particleSystem.Play();
        }

        float objectLifeTime = 0f;
        if (_particleSystems[0] != null)
            objectLifeTime = _particleSystems[0].main.duration + 1;

        _audioSource.PlayOneShot(_destroySound);
        if (_destroyCactiRoutine != null)
        {
            StopCoroutine(_destroyCactiRoutine);
        }
        _destroyCactiRoutine = StartCoroutine(DestroyObjectWithDelay(objectLifeTime, false));
    }

    internal IEnumerator DestroyObjectWithDelay(float delay, bool bringDown)
    {
        yield return new WaitForSeconds(delay);
        if (bringDown)
        {
            RiseFromGroundOnSpawn riseFromGroundOnSpawn = this.GetComponent<RiseFromGroundOnSpawn>();
            riseFromGroundOnSpawn.enabled = true;
            yield return new WaitForSeconds(riseFromGroundOnSpawn._timeToTake);
        }
        if (!IsServer)
            yield break;

        this.NetworkObject.Despawn();
    }
}