using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class DamageCooldownTrigger : MonoBehaviour //todo: add a check to see if the same player is still in the lava
{
    public enum DeathAnimation {
        Default, 
        HeadBurst, 
        Spring, 
        Electrocuted, 
        ComedyMask, 
        TragedyMask, 
        Burnt, 
        Snipped, 
        SliceHead
    }
    public enum ForceDirection {
        Forward,
        Backward,
        Up,
        Down,
        Left,
        Right,
        Center,
    }
    
    [Tooltip("Different ragdoll body types that spawn after death.")]
    public DeathAnimation deathAnimation = DeathAnimation.Default;
    [Tooltip("The force direction of the damage.")]
    public ForceDirection forceDirection = ForceDirection.Forward;
    [Tooltip("Cause of death displayed in ScanNode after death.")]
    public CauseOfDeath causeOfDeath = CauseOfDeath.Unknown;
    [Tooltip("The force magnitude of the damage.")]
    public float forceMagnitudeAfterDamage = 0f;
    [Tooltip("The force magnitude after death of player.")]
    public float forceMagnitudeAfterDeath = 0f;
    [Tooltip("Whether to trigger for enemies.")]
    public bool triggerForEnemies = false;
    [Tooltip("Whether to use shared cooldown between different GameObjects that use this script.")]
    public bool sharedCooldown = false;
    [Tooltip("Whether to play default player damage SFX when damage is dealt.")]
    public bool playDefaultPlayerDamageSFX = false;
    [Tooltip("If true, the force direction will be calculated from the object's transform. If false, the force direction will be calculated from the player's transform.")]
    public bool forceDirectionFromThisObject = true;
    [Tooltip("Whether to play sound when damage is dealt to player that enemies can hear.")]
    public bool soundAttractsDogs = false;
    [Tooltip("Timer in which the gameobject will disable itself, 0 will not disable itself after any point of time.")]
    public float damageDuration = 0f;
    [Tooltip("Damage to deal every interval for players.")]
    public int damageToDealForPlayers = 0;
    [Tooltip("Damage to deal every interval for enemies.")]
    public int damageToDealForEnemies = 0;
    [Tooltip("Cooldown to deal damage for players.")]
    public float damageIntervalForPlayers = 0.25f;
    [Tooltip("Cooldown to deal damage for enemies.")]
    public float damageIntervalForEnemies = 0.25f;
    [Tooltip("Damage clip to play when damage is dealt to player/enemy.")]
    public List<AudioClip>? damageClip = null;
    [Tooltip("Damage audio sources to play when damage is dealt to player (picks the closest AudioSource to the player).")]
    public List<AudioSource>? damageAudioSources = null;

    private static float lastDamageTime = -Mathf.Infinity; // Last time damage was dealt across all instances
    private bool isCoroutineRunning = false; // Flag to check if DamageCooldown coroutine is running

    private void OnEnable()
    {
        StartCoroutine(ManageDamageTimer());
    }

    private IEnumerator ManageDamageTimer()
    {
        if (damageDuration <= 0f)
            yield break;
        yield return new WaitForSeconds(damageDuration);
        gameObject.SetActive(false); // Disable this component or GameObject after the damage duration
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && GameNetworkManager.Instance.localPlayerController == other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            if (sharedCooldown)
            {
                if (Time.time >= lastDamageTime + damageIntervalForPlayers && !isCoroutineRunning)
                {
                    lastDamageTime = Time.time; // Update the last damage time
                    StartCoroutine(DamageCooldown(damageIntervalForPlayers, player: player));
                }
            }
            else
            {
                if (!isCoroutineRunning) // Check if coroutine is already running
                {
                    StartCoroutine(DamageCooldown(damageIntervalForPlayers, player: player));
                }
            }
            return;
        }

        Transform? parent = TryFindRoot(other.transform);
        if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy) && !enemy.isEnemyDead) {
            if (sharedCooldown) {
                if (Time.time >= lastDamageTime + damageIntervalForEnemies && !isCoroutineRunning) {
                    lastDamageTime = Time.time; // Update the last damage time
                    StartCoroutine(DamageCooldown(damageIntervalForEnemies, enemy: enemy));
                }
            } else {
                if (!isCoroutineRunning) // Check if coroutine is already running
                {
                    StartCoroutine(DamageCooldown(damageIntervalForEnemies, enemy: enemy));
                }
            }
            return;
        }
    }

    private IEnumerator DamageCooldown(float interval, PlayerControllerB? player = null, EnemyAI? enemy = null)
    {
        isCoroutineRunning = true; // Set the flag to true when coroutine starts

        // Calculate the force direction after damage
        if (player != null) {
            Vector3 calculatedForceAfterDamage = CalculateForceDirection(player, forceMagnitudeAfterDamage);
            Vector3 calculatedForceAfterDeath = CalculateForceDirection(player, forceMagnitudeAfterDeath);

            player.DamagePlayer(damageToDealForPlayers, playDefaultPlayerDamageSFX, true, causeOfDeath, (int)deathAnimation, false, calculatedForceAfterDeath);
            if (damageClip != null && damageAudioSources != null && damageAudioSources.Count > 0) {
                AudioSource closestAudioSource = damageAudioSources[0];
                float closestDistance = float.MaxValue;
                foreach (AudioSource audioSource in damageAudioSources) {
                    float distanceToPlayer = Vector3.Distance(audioSource.transform.position, player.transform.position);

                    if (distanceToPlayer < closestDistance) {
                        closestDistance = distanceToPlayer;
                        closestAudioSource = audioSource;
                    }
                }
                if (soundAttractsDogs) RoundManager.Instance.PlayAudibleNoise(closestAudioSource.transform.position, closestAudioSource.maxDistance, closestAudioSource.volume, 0, false, 0);
                WalkieTalkie.TransmitOneShotAudio(closestAudioSource, damageClip[Random.Range(0, damageClip.Count)], closestAudioSource.volume);
                RoundManager.PlayRandomClip(closestAudioSource, damageClip.ToArray(), true, closestAudioSource.volume, 0, damageClip.Count);
                closestAudioSource.PlayOneShot(damageClip[Random.Range(0, damageClip.Count)]);
            }
            if (!player.isPlayerDead) {
                player.externalForces += calculatedForceAfterDamage;
            }
        }
        if (enemy != null) {
            enemy.HitEnemy(damageToDealForEnemies, null, false, -1);
            if (damageClip != null && damageAudioSources != null && damageAudioSources.Count > 0) {
                AudioSource closestAudioSource = damageAudioSources[0];
                float closestDistance = float.MaxValue;
                foreach (AudioSource audioSource in damageAudioSources) {
                    float distanceToPlayer = Vector3.Distance(audioSource.transform.position, enemy.transform.position);

                    if (distanceToPlayer < closestDistance) {
                        closestDistance = distanceToPlayer;
                        closestAudioSource = audioSource;
                    }
                }
                if (soundAttractsDogs) RoundManager.Instance.PlayAudibleNoise(closestAudioSource.transform.position, closestAudioSource.maxDistance, closestAudioSource.volume, 0, false, 0);
                WalkieTalkie.TransmitOneShotAudio(closestAudioSource, damageClip[Random.Range(0, damageClip.Count)], closestAudioSource.volume);
                RoundManager.PlayRandomClip(closestAudioSource, damageClip.ToArray(), true, closestAudioSource.volume, 0, damageClip.Count);
                closestAudioSource.PlayOneShot(damageClip[Random.Range(0, damageClip.Count)]);
            }
        }
        yield return new WaitForSeconds(interval);
        isCoroutineRunning = false; // Reset the flag when coroutine finishes
    }

    private Vector3 CalculateForceDirection(PlayerControllerB player, float baseForce)
    {
        Vector3 forceDirectionVector = Vector3.zero;

        // Determine the base direction vector based on the enum
        switch (forceDirection)
        {
            case ForceDirection.Forward:
                forceDirectionVector = forceDirectionFromThisObject ? transform.forward : player.transform.forward;
                break;
            case ForceDirection.Backward:
                forceDirectionVector = forceDirectionFromThisObject ? -transform.forward : -player.transform.forward;
                break;
            case ForceDirection.Up:
                forceDirectionVector = Vector3.up;
                break;
            case ForceDirection.Down:
                forceDirectionVector = Vector3.down;
                break;
            case ForceDirection.Left:
                forceDirectionVector = forceDirectionFromThisObject ? -transform.right : -player.transform.right;
                break;
            case ForceDirection.Right:
                forceDirectionVector = forceDirectionFromThisObject ? transform.right : player.transform.right;
                break;
            case ForceDirection.Center:
                forceDirectionVector = forceDirectionFromThisObject ? (player.transform.position - transform.position).normalized : (transform.position - player.transform.position).normalized;
                break;
        }

        // Multiply the direction vector by the base force magnitude
        return forceDirectionVector.normalized * baseForce;
    }

    public void OnDisable()
    {
        StopAllCoroutines();
        isCoroutineRunning = false; // Ensure the flag is reset if the object is disabled
    }

    public static Transform? TryFindRoot(Transform child)
    {
        // iterate upwards until we find a NetworkObject
        Transform current = child;
        while (current != null)
        {
            if (current.GetComponent<NetworkObject>() != null)
            {
                return current;
            }
            current = current.transform.parent;
        }
        return null;
    }
}
