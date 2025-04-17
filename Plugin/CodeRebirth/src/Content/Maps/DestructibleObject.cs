using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class DestructibleObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Collider[] colliders = [];

    [SerializeField]
    private Renderer[] renderers = [];

    [SerializeField]
    private ParticleSystem[] _particleSystems = [];

    [Header("Settings")]
    [SerializeField]
    private int _playerDamageAmount = 5;

    [SerializeField]
    private float _forceApplied = 5f;

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerControllerB player))
        {
            player.DamagePlayer(_playerDamageAmount, true, false, CauseOfDeath.Unknown, 0, false, (player.transform.position - this.transform.position).normalized * _forceApplied);
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

            Destroy(gameObject, objectLifeTime);
        }
    }
}