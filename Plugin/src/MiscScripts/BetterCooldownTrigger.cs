using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class BetterCooldownTrigger : MonoBehaviour
{
    public enum DeathAnimation
    {
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

    public enum ForceDirection
    {
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

    private Dictionary<PlayerControllerB, bool> playerCoroutineStatus = new Dictionary<PlayerControllerB, bool>();
    private Dictionary<EnemyAI, bool> enemyCoroutineStatus = new Dictionary<EnemyAI, bool>();
    private Dictionary<PlayerControllerB, AudioSource> playerClosestAudioSources = new Dictionary<PlayerControllerB, AudioSource>();
    private Dictionary<EnemyAI, AudioSource> enemyClosestAudioSources = new Dictionary<EnemyAI, AudioSource>();

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && GameNetworkManager.Instance.localPlayerController == other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            if (!playerCoroutineStatus.ContainsKey(player))
            {
                playerCoroutineStatus[player] = true;
                if (damageAudioSources != null && damageAudioSources.Count > 0)
                {
                    playerClosestAudioSources[player] = GetClosestAudioSource(player.transform);
                }
                StartCoroutine(DamagePlayerCoroutine(player));
            }
        }
        else if (triggerForEnemies)
        {
            Transform? parent = TryFindRoot(other.transform);
            if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy) && !enemy.isEnemyDead)
            {
                if (!enemyCoroutineStatus.ContainsKey(enemy))
                {
                    enemyCoroutineStatus[enemy] = true;
                    if (damageAudioSources != null && damageAudioSources.Count > 0)
                    {
                        enemyClosestAudioSources[enemy] = GetClosestAudioSource(enemy.transform);
                    }
                    StartCoroutine(DamageEnemyCoroutine(enemy));
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && GameNetworkManager.Instance.localPlayerController == other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            playerCoroutineStatus[player] = false;
            playerClosestAudioSources.Remove(player);
        }
        else if (triggerForEnemies)
        {
            Transform? parent = TryFindRoot(other.transform);
            if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy))
            {
                enemyCoroutineStatus[enemy] = false;
                enemyClosestAudioSources.Remove(enemy);
            }
        }
    }

    private IEnumerator DamagePlayerCoroutine(PlayerControllerB player)
    {
        while (playerCoroutineStatus[player])
        {
            if (sharedCooldown && Time.time < lastDamageTime + damageIntervalForPlayers)
            {
                yield return null;
                continue;
            }

            lastDamageTime = Time.time;
            ApplyDamageToPlayer(player);
            yield return new WaitForSeconds(damageIntervalForPlayers);
        }
    }

    private IEnumerator DamageEnemyCoroutine(EnemyAI enemy)
    {
        while (enemyCoroutineStatus[enemy])
        {
            if (sharedCooldown && Time.time < lastDamageTime + damageIntervalForEnemies)
            {
                yield return null;
                continue;
            }

            lastDamageTime = Time.time;
            ApplyDamageToEnemy(enemy);
            yield return new WaitForSeconds(damageIntervalForEnemies);
        }
    }

    private void ApplyDamageToPlayer(PlayerControllerB player)
    {
        Vector3 calculatedForceAfterDamage = CalculateForceDirection(player, forceMagnitudeAfterDamage);
        Vector3 calculatedForceAfterDeath = CalculateForceDirection(player, forceMagnitudeAfterDeath);

        player.DamagePlayer(damageToDealForPlayers, playDefaultPlayerDamageSFX, true, causeOfDeath, (int)deathAnimation, false, calculatedForceAfterDeath);
        PlayDamageSound(player.transform, playerClosestAudioSources.ContainsKey(player) ? playerClosestAudioSources[player] : null);

        if (!player.isPlayerDead)
        {
            player.externalForces += calculatedForceAfterDamage;
        }
    }

    private void ApplyDamageToEnemy(EnemyAI enemy)
    {
        enemy.HitEnemy(damageToDealForEnemies, null, false, -1);
        PlayDamageSound(enemy.transform, enemyClosestAudioSources.ContainsKey(enemy) ? enemyClosestAudioSources[enemy] : null);
    }

    private Vector3 CalculateForceDirection(PlayerControllerB player, float baseForce)
    {
        Vector3 forceDirectionVector = Vector3.zero;

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

        return forceDirectionVector.normalized * baseForce;
    }

    private void PlayDamageSound(Transform targetTransform, AudioSource? audioSource)
    {
        if (damageClip != null && audioSource != null)
        {
            if (soundAttractsDogs)
            {
                RoundManager.Instance.PlayAudibleNoise(audioSource.transform.position, audioSource.maxDistance, audioSource.volume, 0, false, 0);
            }

            WalkieTalkie.TransmitOneShotAudio(audioSource, damageClip[Random.Range(0, damageClip.Count)], audioSource.volume);
            RoundManager.PlayRandomClip(audioSource, damageClip.ToArray(), true, audioSource.volume, 0, damageClip.Count);
            audioSource.PlayOneShot(damageClip[Random.Range(0, damageClip.Count)]);
        }
    }

    private AudioSource GetClosestAudioSource(Transform targetTransform)
    {
        AudioSource closest = damageAudioSources![0];
        float closestDistance = Vector3.Distance(closest.transform.position, targetTransform.position);

        foreach (AudioSource source in damageAudioSources)
        {
            float distance = Vector3.Distance(source.transform.position, targetTransform.position);
            if (distance < closestDistance)
            {
                closest = source;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public void OnDisable()
    {
        StopAllCoroutines();
        playerCoroutineStatus.Clear();
        enemyCoroutineStatus.Clear();
        playerClosestAudioSources.Clear();
        enemyClosestAudioSources.Clear();
    }

    public static Transform? TryFindRoot(Transform child)
    {
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