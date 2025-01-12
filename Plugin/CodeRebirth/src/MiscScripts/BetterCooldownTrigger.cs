using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class BetterCooldownTrigger : NetworkBehaviour
{
    #region Enums

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
        SliceHead,
        PlaceHolder1,
        PlaceHolder2,
        PlaceHolder3,
        PlaceHolder4,
        PlaceHolder5,
        PlaceHolder6
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

    #endregion

    #region Fields
    [Header("Script Enabled")]
    [Tooltip("Whether this script is enabled.")]
    public bool enabledScript = true;

    [Header("Death Animation Settings")]
    [Tooltip("Different ragdoll body types that spawn after death.")]
    public DeathAnimation deathAnimation = DeathAnimation.Default;
    [Space(2)]
    
    [Header("Force Settings")]
    [Tooltip("The force direction of the damage.")]
    public ForceDirection forceDirection = ForceDirection.Forward;
    [Tooltip("The force magnitude of the damage.")]
    public float forceMagnitudeAfterDamage = 0f;
    [Tooltip("The force magnitude after death of player.")]
    public float forceMagnitudeAfterDeath = 0f;
    [Tooltip("If true, the force direction will be calculated from the object's transform. If false, the force direction will be calculated from the player's transform.")]
    public bool forceDirectionFromThisObject = true;
    [Space(2)]
    
    [Header("Cause of Death")]
    [Tooltip("Cause of death displayed in ScanNode after death.")]
    public CauseOfDeath causeOfDeath = CauseOfDeath.Unknown;
    [Space(2)]
    
    [Header("Trigger Settings")]
    [Tooltip("Whether to trigger for players.")]
    public bool triggerForPlayers = false;
    [Tooltip("Whether to trigger for enemies.")]
    public bool triggerForEnemies = false;
    [Tooltip("Whether player/enemy can exit the trigger's effect.")]
    public bool canThingExit = true;
    [Tooltip("Whether to use shared cooldown between different GameObjects that use this script.")]
    public bool sharedCooldown = false;
    [Tooltip("Whether to play default player damage SFX when damage is dealt.")]
    public bool playDefaultPlayerDamageSFX = false;
    [Tooltip("Whether to play sound when damage is dealt to player that enemies can hear.")]
    public bool soundAttractsDogs = false;
    [Space(2)]
    
    [Header("Damage Settings")]
    [Tooltip("Enemy Main Script if put on an enemy")]
    public EnemyAI? enemyMainScript = null;
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
    [Space(2)]
    
    [Header("Audio Settings")]
    [Tooltip("Damage clip to play when damage is dealt to player/enemy.")]
    public List<AudioClip> damageClips = new();
    [Tooltip("Damage audio sources to play when damage is dealt to player (picks the closest AudioSource to the player).")]
    public List<AudioSource> damageAudioSources = new();
    [Space(2)]
    
    [Header("Death Prefab Settings")]
    [Tooltip("Prefab to spawn when the player dies.")]
    public GameObject? deathPrefabForPlayer = null;
    [Tooltip("Prefab to spawn when the enemy dies.")]
    public GameObject? deathPrefabForEnemy = null;
    [Space(2)]
    
    [Header("Particle System Settings")]
    [Tooltip("Use particle systems when damage is dealt to player/enemy.")]
    public bool useParticleSystems = false;
    [Tooltip("Teleport particle system to enemy/player when damage is dealt.")]
    public bool teleportParticles = false;
    [Tooltip("Particle system to play when the player dies.")]
    public List<ParticleSystem> deathParticlesForPlayer = new();
    [Tooltip("Particle system to play when the player is damaged.")]
    public List<ParticleSystem> damageParticlesForPlayer = new();
    [Tooltip("Particle system to play when the enemy dies.")]
    public List<ParticleSystem> deathParticlesForEnemy = new();
    [Tooltip("Particle system to play when the enemy is damaged.")]
    public List<ParticleSystem> damageParticlesForEnemy = new();
    [Space(2f)]
    
    [Header("Display Message Settings")]
    [Tooltip("Whether to display message to player once damage is dealt to player.")]
    public bool displayMessage = false;
    [Tooltip("Header of message to display to player once damage is dealt to player.")]
    public string headerText = "Damage Dealt!";
    [Tooltip("Message to display to player once damage is dealt to player.")]
    public string message = "Damage dealt!";
    [Tooltip("Interval between displaying message to player once damage is dealt to player.")]
    public float messageInterval = 10f;
    #endregion

    #region Private Fields

    private static float lastDamageTime = -Mathf.Infinity;
    private static float lastTipTime = -Mathf.Infinity;

    private Dictionary<PlayerControllerB, bool> playerCoroutineStatus = new Dictionary<PlayerControllerB, bool>();
    private Dictionary<EnemyAI, bool> enemyCoroutineStatus = new Dictionary<EnemyAI, bool>();
    private Dictionary<PlayerControllerB, AudioSource> playerClosestAudioSources = new Dictionary<PlayerControllerB, AudioSource>();
    private Dictionary<EnemyAI, AudioSource> enemyClosestAudioSources = new Dictionary<EnemyAI, AudioSource>();

    #endregion
    public void SetupFields(bool enableScripts = true, bool damagePlayers = true, bool damageEnemies = true, DeathAnimation deathAnimation = DeathAnimation.Default, ForceDirection forceDirection = ForceDirection.Down, float forceMagnitudeAfterDamage = 0f, float forceMagnitudeAfterDeath = 0f, bool forceDirectionFromThisObject = true, CauseOfDeath causeOfDeath = CauseOfDeath.Abandoned, bool canThingExit = true, bool sharedCooldown = false, bool playDefaultPlayerDamageSFX = true, bool soundAttractsDogs = true, float damageDuration = 0f, int damageToDealForPlayers = 0, float damageIntervalForPlayers = 0f, int damageToDealForEnemies = 0, float damageIntervalForEnemies = 0, List<AudioClip>? damageClips = null, List<AudioSource>? damageAudioSources = null, GameObject? deathPrefabForPlayer = null, GameObject? deathPrefabForEnemy = null, bool useParticleSystems = false, bool teleportParticles = false, List<ParticleSystem>? damageParticlesForPlayer = null, List<ParticleSystem>? deathParticlesForPlayer = null, List<ParticleSystem>? damageParticlesForEnemy = null, List<ParticleSystem>? deathParticlesForEnemy = null, bool displayMessage = false, string headerText = "Damage", string message = "Damage dealt!", float messageInterval = 10f)
    {
        enabledScript = enableScripts;
        triggerForPlayers = damagePlayers;
        triggerForEnemies = damageEnemies;
        this.deathAnimation = deathAnimation;
        this.forceDirection = forceDirection;
        this.forceMagnitudeAfterDamage = forceMagnitudeAfterDamage;
        this.forceMagnitudeAfterDeath = forceMagnitudeAfterDeath;
        this.forceDirectionFromThisObject = forceDirectionFromThisObject;
        this.causeOfDeath = causeOfDeath;
        this.canThingExit = canThingExit;
        this.sharedCooldown = sharedCooldown;
        this.playDefaultPlayerDamageSFX = playDefaultPlayerDamageSFX;
        this.soundAttractsDogs = soundAttractsDogs;
        this.damageDuration = damageDuration;
        this.damageToDealForPlayers = damageToDealForPlayers;
        this.damageIntervalForPlayers = damageIntervalForPlayers;
        this.damageToDealForEnemies = damageToDealForEnemies;
        this.damageIntervalForEnemies = damageIntervalForEnemies;
        damageClips ??= new List<AudioClip>();
        this.damageClips = damageClips;
        damageAudioSources ??= new List<AudioSource>();
        this.damageAudioSources = damageAudioSources;
        this.deathPrefabForPlayer = deathPrefabForPlayer;
        this.deathPrefabForEnemy = deathPrefabForEnemy;
        this.useParticleSystems = useParticleSystems;
        this.teleportParticles = teleportParticles;
        damageParticlesForPlayer ??= new List<ParticleSystem>();
        this.damageParticlesForPlayer = damageParticlesForPlayer;
        deathParticlesForPlayer ??= new List<ParticleSystem>();
        this.deathParticlesForPlayer = deathParticlesForPlayer;
        damageParticlesForEnemy ??= new List<ParticleSystem>();
        this.damageParticlesForEnemy = damageParticlesForEnemy;
        deathParticlesForEnemy ??= new List<ParticleSystem>();
        this.deathParticlesForEnemy = deathParticlesForEnemy;
        this.displayMessage = displayMessage;
        this.headerText = headerText;
        this.message = message;
        this.messageInterval = messageInterval;
    }

    public void OnEnable()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts) {
            playerCoroutineStatus[player] = false;
        }
        StartOfRound.Instance.playerTeleportedEvent.AddListener(new UnityAction<PlayerControllerB>(this.RemovePlayerFromList));
        StartCoroutine(ManageDamageTimer());
    }


    private void RemovePlayerFromList(PlayerControllerB player)
    {
        if (playerCoroutineStatus.ContainsKey(player) && playerCoroutineStatus[player])
		{
			playerCoroutineStatus[player] = false;
		}
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
        if (!enabledScript) return;
        if (triggerForPlayers && other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player) && player == GameNetworkManager.Instance.localPlayerController)
        {
            if (playerCoroutineStatus.ContainsKey(player))
            {
                playerCoroutineStatus[player] = true;
                if (damageAudioSources.Count > 0)
                {
                    playerClosestAudioSources[player] = GetClosestAudioSource(player.transform);
                }
                Plugin.ExtendedLogging("Player Coroutine Started", (int)Logging_Level.Medium);
                StartCoroutine(DamagePlayerCoroutine(player));
            }
        }
        if (triggerForEnemies)
        {
            Transform? parent = CRUtilities.TryFindRoot(other.transform);
            if (parent != null && parent.TryGetComponent<EnemyAI>(out EnemyAI enemy) && !enemy.isEnemyDead && !(enemyMainScript != null && (enemy == enemyMainScript || enemyMainScript is RedwoodTitanAI redwoodTitanAI && enemy == redwoodTitanAI.targetEnemy)))
            {
                if (!enemyCoroutineStatus.ContainsKey(enemy))
                {
                    enemyCoroutineStatus[enemy] = true;
                    if (damageAudioSources.Count > 0)
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
        if (!enabledScript || !canThingExit) return;

        if (triggerForPlayers && other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player) && GameNetworkManager.Instance.localPlayerController == player)
        {
            Plugin.ExtendedLogging("Player Coroutine Stopped", (int)Logging_Level.Medium);
            RemovePlayerFromList(player);
            playerClosestAudioSources.Remove(player);
        }
        if (triggerForEnemies)
        {
            Transform? parent = CRUtilities.TryFindRoot(other.transform);
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
        if (displayMessage && Time.time >= lastTipTime + damageIntervalForPlayers)
        {
            HUDManager.Instance.DisplayTip(headerText, message, true);
            lastTipTime = Time.time;
        }

        PlayDamageSound(player.transform, playerClosestAudioSources.ContainsKey(player) ? playerClosestAudioSources[player] : null);
        if (teleportParticles)
        {
            foreach (ParticleSystem? particle in damageParticlesForPlayer)
            {
                if (particle != null) particle.transform.position = player.transform.position;
            }
            foreach (ParticleSystem? particle in deathParticlesForPlayer)
            {
                if (particle != null) particle.transform.position = player.transform.position;
            }
        }
        if (!player.isPlayerDead)
        {
            player.externalForces += calculatedForceAfterDamage;
        }
        else
        {
            if (deathPrefabForPlayer != null && deathPrefabForPlayer.GetComponent<NetworkObject>() != null)
            {
                SpawnDeathPrefabServerRpc(player.transform.position, player.transform.rotation, true);
            }
            else if (deathPrefabForPlayer != null)
            {
                Instantiate(deathPrefabForPlayer, player.transform.position, player.transform.rotation);
                playerCoroutineStatus[player] = false;
                playerClosestAudioSources.Remove(player);
            }
        }
        if (useParticleSystems) HandleParticleSystemStuffServerRpc(player.transform.position, true, player.isPlayerDead);
    }

    private void ApplyDamageToEnemy(EnemyAI enemy)
    {
        enemy.HitEnemy(damageToDealForEnemies, null, false, -1);
        PlayDamageSound(enemy.transform, enemyClosestAudioSources.ContainsKey(enemy) ? enemyClosestAudioSources[enemy] : null);

        if (enemy.isEnemyDead)
        {
            if (deathPrefabForEnemy != null && deathPrefabForEnemy.GetComponent<NetworkObject>() != null)
            {
                SpawnDeathPrefabServerRpc(enemy.transform.position, enemy.transform.rotation, false);
            }
            else if (deathPrefabForEnemy != null)
            {
                Instantiate(deathPrefabForEnemy, enemy.transform.position, enemy.transform.rotation);
            }
            enemyCoroutineStatus[enemy] = false;
            enemyClosestAudioSources.Remove(enemy);
        }

        if (useParticleSystems) HandleParticleSystemStuffServerRpc(enemy.transform.position, false, enemy.isEnemyDead);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleParticleSystemStuffServerRpc(Vector3 position, bool forPlayer, bool isDead) {
        HandleParticleSystemStuffClientRpc(position, forPlayer, isDead);
    }

    [ClientRpc]
    private void HandleParticleSystemStuffClientRpc(Vector3 position, bool forPlayer, bool isDead) {
        if (teleportParticles)
        {
            if (forPlayer)
            {
                foreach (ParticleSystem? particle in damageParticlesForPlayer)
                {
                    if (particle != null) particle.transform.position = position;
                }
                foreach (ParticleSystem? particle in deathParticlesForPlayer)
                {
                    if (particle != null) particle.transform.position = position;
                }  
            }
            else
            {
                foreach (ParticleSystem? particle in damageParticlesForEnemy)
                {
                    if (particle != null) particle.transform.position = position;
                }
                foreach (ParticleSystem? particle in deathParticlesForEnemy)
                {
                    if (particle != null) particle.transform.position = position;
                }
            }
        }

        if (forPlayer)
        {
            if (!isDead && damageParticlesForPlayer.Count > 0)
            {
                var particleSystem = damageParticlesForPlayer[Random.Range(0, damageParticlesForPlayer.Count)];
                particleSystem.Play();
            }
            else if (isDead && deathParticlesForPlayer.Count > 0)
            {
                var particleSystem = deathParticlesForPlayer[Random.Range(0, deathParticlesForPlayer.Count)];
                particleSystem.Play();
            }
        }
        else
        {
            if (!isDead && damageParticlesForEnemy.Count > 0)
            {
                var particleSystem = damageParticlesForEnemy[Random.Range(0, damageParticlesForEnemy.Count)];
                particleSystem.Play();
            }
            else if (isDead && deathParticlesForEnemy.Count > 0)
            {
                var particleSystem = deathParticlesForEnemy[Random.Range(0, deathParticlesForEnemy.Count)];
                particleSystem.Play();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnDeathPrefabServerRpc(Vector3 position, Quaternion rotation, bool forPlayer)
    {
        if (forPlayer)
        {
            Instantiate(deathPrefabForPlayer, position, rotation);
            deathPrefabForPlayer?.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Instantiate(deathPrefabForEnemy, position, rotation);
            deathPrefabForEnemy?.GetComponent<NetworkObject>().Spawn();
        }
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
                forceDirectionVector = forceDirectionFromThisObject ? transform.up : player.transform.up;
                break;
            case ForceDirection.Down:
                forceDirectionVector = forceDirectionFromThisObject ? -transform.up : -player.transform.up;
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
        if (soundAttractsDogs)
        {
            RoundManager.Instance.PlayAudibleNoise(targetTransform.transform.position, audioSource != null ? (float)audioSource.maxDistance : 10f, audioSource != null ? audioSource.volume : 0.85f, 0, false, 0);
        }
        if (damageClips.Count > 0 && audioSource != null)
        {
            WalkieTalkie.TransmitOneShotAudio(audioSource, damageClips[Random.Range(0, damageClips.Count)], audioSource.volume);
            RoundManager.PlayRandomClip(audioSource, damageClips.ToArray(), true, audioSource.volume, 0, damageClips.Count);
        }
    }

    private AudioSource GetClosestAudioSource(Transform targetTransform)
    {
        AudioSource closest = damageAudioSources[0];
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
        StartOfRound.Instance.playerTeleportedEvent.RemoveListener(new UnityAction<PlayerControllerB>(this.RemovePlayerFromList));
    }
}