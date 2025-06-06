using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class DestructibleObject : MonoBehaviour, IHittable
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

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {   
        DestroyDestructibleObject();
        return true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!_isDestructible)
            return;

        if (other.gameObject.TryGetComponent(out PlayerControllerB player))
        {
            player.DamagePlayer(_playerDamageAmount, true, false, CauseOfDeath.Unknown, 0, false, (player.transform.position - this.transform.position).normalized * _forceApplied);
            DestroyDestructibleObject();
        }
    }

    public void SetDestructible(bool isDestructible)
    {
        _isDestructible = isDestructible;
    }

    public void DestroyDestructibleObject()
    {
        if (!_isDestructible)
            return;

        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (Vector3.Distance(this.transform.position, player.transform.position) > 1f)
                continue;

            player.DamagePlayer(5, true, false, CauseOfDeath.Stabbing, 0, false, default);
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
            objectLifeTime = _particleSystems[0].main.duration;

        _audioSource.PlayOneShot(_destroySound);
        Destroy(gameObject, objectLifeTime);
    }
}