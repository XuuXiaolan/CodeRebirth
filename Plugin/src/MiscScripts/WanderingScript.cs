using CodeRebirth;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.MiscScripts;
public class WanderingCreatureController : NetworkBehaviour
{
    [Header("Movement")]
    [Tooltip("The maximum distance from the creature's current position within which it can wander.")]
    public float wanderRadius = 10f;
    [Tooltip("The duration the creature waits at its destination before selecting a new destination to wander towards.")]
    public float waitTime = 3f;
    [Tooltip("If enabled, the wait time becomes random within the range [0, Wait Time].")]
    public bool randomTime = false;
    [Tooltip("Toggle whether the creature is allowed to wander or not.")]
    public bool wanderEnabled = true;
    [Tooltip("When enabled, the creature will only wander around its spawn point within a radius defined by the Wander Radius. If disabled, the creature can wander from any point within the Wander Radius.")]
    public bool anchoredWandering = true;

    [Header("Audio")]
    public AudioSource ambientAudioSource = null!;
    [Tooltip("An array of audio clips that can be played randomly at intervals while the creature is wandering.")]
    public AudioClip[] ambientAudioClips = null!;
    [Tooltip("The minimum interval between playing ambient audio clips.")]
    public float minAmbientAudioInterval = 3f;
    [Tooltip("The maximum interval between playing ambient audio clips.")]
    public float maxAmbientAudioInterval = 7f;
    [Tooltip("The volume for ambient audio.")]
    [Range(0f, 2f)]
    public float ambientAudioVolume = 1f;
    [Space]
    public AudioSource walkingAudioSource = null!;
    [Tooltip("An array of audio clips that can be played randomly at intervals while the creature is moving.")]
    public AudioClip[] walkingAudioClips = null!;
    [Tooltip("The interval between playing walking audio clips.")]
    public float walkingAudioInterval = 0.5f;
    [Tooltip("The volume for walking audio.")]
    [Range(0f, 2f)]
    public float walkingAudioVolume = 1f;

    [Header("Rotation")]
    [Tooltip("If enabled, the creature will follow the surface normal underneath it.")]
    public bool followSurface = false;
    [Tooltip("The distance to cast the ray to detect the surface normal.")]
    public float surfaceNormalRaycastDistance = 2f;

    private NavMeshAgent agent = null!;
    private bool isMoving = false;
    private bool startMovingSignal = false;
    private float waitTimer = 0f;
    private float ambientAudioTimer = 0f;
    private float walkingAudioTimer = 0f;
    private int currentAmbientClipIndex;
    private Vector3 startPosition;
    private float lastSyncedAmbientAudioTimer = 0f;
    private float syncedWaitTimer = 0f;

    void Start()
    {
        startPosition = transform.position;

        agent = GetComponent<NavMeshAgent>();

        if (IsServer)
        {
            GenerateDestination();
            startMovingSignal = true; // Signal to start moving on the server
        }

        if (ambientAudioSource == null || walkingAudioSource == null)
        {
            // Plugin.Logger.LogDebug("WaderingCreatureController: One or both AudioSource components are not assigned!");
            return;
        }

        ambientAudioSource.volume = ambientAudioVolume;
        walkingAudioSource.volume = walkingAudioVolume;
    }

    void Update()
    {
        if (!wanderEnabled)
            return;

        // If startMovingSignal is true and the timer has expired, start moving
        if (startMovingSignal && !isMoving && waitTimer <= 0f)
        {
            GenerateDestinationServerRpc();
            startMovingSignal = false; // Reset the flag
        }

        // Movement logic
        if (isMoving)
        {
            // Check if the agent has reached its destination
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    isMoving = false;

                    // Synchronize wait timer with clients
                    syncedWaitTimer = waitTimer;
                    WaitTimerSyncServerRpc(waitTimer);

                    if (randomTime)
                    {
                        waitTimer = Random.Range(0, waitTime);
                    }
                    else
                    {
                        waitTimer = waitTime;
                    }
                }
            }
        }
        else
        {
            // If not moving, decrement wait timer
            if (waitTimer > 0)
            {
                waitTimer -= Time.deltaTime;
            }
            else
            {
                // If wait time is over, set new destination
                if (IsServer)
                {
                    GenerateDestinationServerRpc();
                }
            }
        }

        // Ambient audio logic
        if (ambientAudioClips != null && ambientAudioClips.Length > 0)
        {
            // Only the server should handle the ambient audio timer
            if (IsServer)
            {
                ambientAudioTimer -= Time.deltaTime;

                // Check if it's time to synchronize the timer with clients
                if (ambientAudioTimer <= 0f)
                {
                    ambientAudioTimer = Random.Range(minAmbientAudioInterval, maxAmbientAudioInterval);
                    lastSyncedAmbientAudioTimer = ambientAudioTimer;
                    AmbientAudioTimerSyncServerRpc(ambientAudioTimer);
                }
            }
            else
            {
                // On clients, use the last synchronized value
                ambientAudioTimer = lastSyncedAmbientAudioTimer;
            }

            // Check if it's time to play ambient audio
            if (ambientAudioTimer <= 0f)
            {
                SelectAmbientAudioClipServerRpc();
            }
        }

        // Walking audio logic
        if (isMoving && walkingAudioClips != null)
        {
            walkingAudioTimer -= Time.deltaTime;
            if (walkingAudioTimer <= 0f)
            {
                int randomIndex = Random.Range(0, walkingAudioClips.Length);
                walkingAudioSource.clip = walkingAudioClips[randomIndex];
                walkingAudioSource.Play();

                walkingAudioTimer = walkingAudioInterval;
            }
        }

        // Rotate to follow surface normal
        if (followSurface)
        {
            RotateToFollowSurfaceNormal();
        }
    }

    void GenerateDestination()
    {
        // Generate random direction within the wander radius
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;

        if (anchoredWandering)
        {
            randomDirection += startPosition;
        }

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas);
        Vector3 newDestination = hit.position;

        // Set agent's destination
        agent.SetDestination(newDestination);
        isMoving = true;
    }

    void RotateToFollowSurfaceNormal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, surfaceNormalRaycastDistance))
        {
            // Get the surface normal
            Vector3 surfaceNormal = hit.normal;

            // Calculate the target rotation based on the surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;

            // Set the rotation directly to the target rotation
            transform.rotation = targetRotation;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw the wander radius gizmo
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }

    [ServerRpc(RequireOwnership = false)]
    void GenerateDestinationServerRpc()
    {
        GenerateDestination();
        // Inform clients about the change in position and start moving
        GenerateDestinationClientRpc();
    }

    [ClientRpc]
    void GenerateDestinationClientRpc()
    {
        // Start moving on clients
        isMoving = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void SelectAmbientAudioClipServerRpc()
    {
        currentAmbientClipIndex = Random.Range(0, ambientAudioClips.Length);
        SelectAmbientAudioClipClientRpc(currentAmbientClipIndex);
    }

    [ClientRpc]
    void SelectAmbientAudioClipClientRpc(int clipIndex)
    {
        if (ambientAudioClips != null && ambientAudioClips.Length > 0)
        {
            currentAmbientClipIndex = clipIndex;
            ambientAudioSource.clip = ambientAudioClips[currentAmbientClipIndex];
            ambientAudioSource.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void AmbientAudioTimerSyncServerRpc(float timerValue)
    {
        lastSyncedAmbientAudioTimer = timerValue;
        AmbientAudioTimerSyncClientRpc(timerValue);
    }

    [ClientRpc]
    void AmbientAudioTimerSyncClientRpc(float timerValue)
    {
        lastSyncedAmbientAudioTimer = timerValue;
    }

    [ServerRpc(RequireOwnership = false)]
    void WaitTimerSyncServerRpc(float timerValue)
    {
        syncedWaitTimer = timerValue;
        WaitTimerSyncClientRpc(timerValue);
    }

    [ClientRpc]
    void WaitTimerSyncClientRpc(float timerValue)
    {
        syncedWaitTimer = timerValue;
    }
}