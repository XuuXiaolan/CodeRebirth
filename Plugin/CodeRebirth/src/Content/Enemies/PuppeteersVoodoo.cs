using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
[RequireComponent(typeof(SmartAgentNavigator))]
public class PuppeteersVoodoo : NetworkBehaviour, IHittable
{
    public NavMeshAgent agent = null!;
    public Animator animator = null!;
    public Renderer renderer = null!;
    public Material[] materialVariants = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;
    [Header("Football-Like Kick Settings")]
    [Tooltip("Upward force added on kick.")]
    public float ballHitUpwardAmount = 0.5f;

    [Header("Sounds")]
    public AudioClip[] puppetFootstepSounds = [];
    public AudioClip[] hitSounds = [];
    public AudioClip deathSound = null!;

    [Header("Curves for the Arc/Fall Behavior")]
    public AnimationCurve dollFallCurve;
    public AnimationCurve dollVerticalFallCurve;
    public AnimationCurve dollVerticalOffset;
    public AnimationCurve dollVerticalFallCurveNoBounce;

    [Tooltip("Collision layers for the 'kick' raycast, etc.")]
    public LayerMask dollBallMask = 369101057;

    [Space(5f)]
    public AudioClip[] hitBallSFX = [];
    public AudioClip[] ballHitFloorSFX = [];
    public AudioSource dollAudio = null!;

    [HideInInspector] public Coroutine? breakDollRoutine = null;
    private Ray dollRay;
    private RaycastHit dollHit;
    private float hitTimer;
    private float fallTime;
    private bool hasHitGround;
    private Vector3 startFallingPosition;
    private Vector3 targetFloorPosition;
    private int damageTransferMultiplier = 20; // 20-30 as described
    [HideInInspector] public float lastTimeTakenDamageFromEnemy = 0f;
    [HideInInspector] public Puppeteer puppeteerCreatedBy = null!;
    [HideInInspector] public PlayerControllerB? playerControlled = null;

    private static readonly int OnHitAnimation = Animator.StringToHash("onHit"); // Triger
    private static readonly int IsKickedAnimation = Animator.StringToHash("isKicked"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    private System.Random puppetRandom = new(69);
    public static List<PuppeteersVoodoo> puppeteerList = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        puppeteerList.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        puppeteerList.Remove(this);
    }

    public void Start()
    {
        hitTimer = Time.realtimeSinceStartup + 3;
        smartAgentNavigator.SetAllValues(puppeteerCreatedBy.isOutside);
        puppetRandom = new System.Random(StartOfRound.Instance.randomMapSeed + puppeteerList.Count);

        if (puppetRandom.Next(5) == 0)
        {
            renderer.sharedMaterial = materialVariants[puppetRandom.Next(materialVariants.Length)];
        }
    }

    public void Init(PlayerControllerB player, Puppeteer puppeteer, int damageTransferMultiplier)
    {
        playerControlled = player;
        if (this.IsOwner)
        {
            NetworkObject.ChangeOwnership(playerControlled.OwnerClientId);
        }
        puppeteerCreatedBy = puppeteer;
        this.damageTransferMultiplier = damageTransferMultiplier;
    }

    public void Update()
    {
        lastTimeTakenDamageFromEnemy += Time.deltaTime;
        if (IsServer)
        {
            if (puppeteerCreatedBy == null && breakDollRoutine == null)
            {
                breakDollRoutine = StartCoroutine(BreakDoll());
                playerControlled = null;
                return;
            }
            if ((playerControlled == null || playerControlled.isPlayerDead) && breakDollRoutine == null)
            {
                breakDollRoutine = StartCoroutine(BreakDoll());
                playerControlled = null;
                return;
            }
        }
        if (playerControlled == null)
        {
            return;
        }
        if (!hasHitGround && fallTime < 1f)
        {
            FallWithCurve();
        }

        if (agent.enabled)
        {
            animator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 2);
            smartAgentNavigator.DoPathingToDestination(playerControlled.transform.position);
            smartAgentNavigator.AdjustSpeedBasedOnDistance(0, 40, 2.5f, 10, 1);
        }
        else
        {
            animator.SetFloat(RunSpeedFloat, 0);
            if (hasHitGround)
            {
                this.transform.position = playerControlled.transform.position;
                agent.enabled = true;
            }
        }
    }

    public void OnDollDamaged(int damage, CauseOfDeath causeOfDeath)
    {
        if (playerControlled != null)
        {
            int finalDamage = Mathf.RoundToInt(damage * damageTransferMultiplier);
            DoHitAnimationServerRpc(finalDamage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoHitAnimationServerRpc(int finalDamage)
    {
        PlayMiscSoundsClientRpc(1, finalDamage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayMiscSoundsServerRpc(int soundID)
    {
        PlayMiscSoundsClientRpc(soundID);
    }

    [ClientRpc]
    private void PlayMiscSoundsClientRpc(int soundID, int finalDamage = 0)
    {
        switch (soundID)
        {
            case 0:
                dollAudio.PlayOneShot(deathSound);
                break;
            case 1:
                animator.SetTrigger(OnHitAnimation);
                lastTimeTakenDamageFromEnemy = 0;
                if (playerControlled != null)
                {
                    playerControlled.DamagePlayer(finalDamage, true, true, CauseOfDeath.Unknown);
                }
                else
                {
                    Plugin.Logger.LogError("PlayerControlled is null");
                }
                dollAudio.PlayOneShot(hitSounds[puppetRandom.Next(hitSounds.Length)]);
                break;
        }
    }

    public IEnumerator BreakDoll()
    {
        PlayMiscSoundsServerRpc(0);
        animator.SetBool(IsDeadAnimation, true);
        DeadAnimationRpc();
        yield return new WaitForSeconds(4f);
        if (playerControlled != null && !playerControlled.isPlayerDead)
        {
            CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.PuppeteersVoodoo].Item, transform.position, false, true, 0);
        }
        NetworkObject.Despawn();
    }

    [Rpc(SendTo.NotMe)]
    private void DeadAnimationRpc()
    {
        animator.SetBool(IsDeadAnimation, true);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        OnDollDamaged(force, CauseOfDeath.Bludgeoning);

        if (playerWhoHit != null)
        {
            Vector3 fromPosition = playerWhoHit.transform.position + Vector3.up;
            BeginKickDoll(fromPosition);
        }
        return true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (Time.realtimeSinceStartup - hitTimer < 0.5f)
            return;

        if (!IsOwner)
            return;

        if (other.tag.StartsWith("PlayerBody") || other.CompareTag("Enemy"))
        {
            BeginKickDoll(other.transform.position + Vector3.up);
        }
    }

    private void BeginKickDoll(Vector3 hitFromPosition)
    {
        hitTimer = Time.realtimeSinceStartup;

        Vector3 destination = GetKickDestination(hitFromPosition);
        if (destination == Vector3.zero) return;

        KickDollLocalClient(destination);
        Vector3 position = destination;
        position.y = this.transform.position.y;
        this.transform.LookAt(position);
        // Otherwise, replicate the effect over the network
        KickDollRpc(destination);
    }

    private Vector3 GetKickDestination(Vector3 hitFromPosition)
    {
        // 1) Simple forward ray
        Vector3 direction = (transform.position - hitFromPosition).normalized;
        direction.y = 0.2f; // small upward tilt
        float distanceToTravel = 10f;

        dollRay = new Ray(transform.position + Vector3.up * 0.22f, direction);

        // Attempt a forward ray
        if (Physics.Raycast(dollRay, out dollHit, distanceToTravel, dollBallMask, QueryTriggerInteraction.Ignore))
        {
            // If something is hit, place just before collision
            return dollRay.GetPoint(Mathf.Max(0.1f, dollHit.distance - 0.05f));
        }
        else
        {
            // Otherwise, go the full distance
            return dollRay.GetPoint(distanceToTravel);
        }
    }

    private void KickDollLocalClient(Vector3 destinationPos)
    {
        // Disable agent/AI to allow “physics” style arc
        animator.SetBool(IsKickedAnimation, true);
        smartAgentNavigator.enabled = false;
        agent.enabled = false;

        dollAudio.PlayOneShot(hitBallSFX[puppetRandom.Next(hitBallSFX.Length)]);

        transform.SetParent(StartOfRound.Instance.propsContainer, true);

        // Reset states for the arc
        fallTime = 0f;
        hasHitGround = false;

        Vector3 finalWorldDest = SnapToFloor(destinationPos);

        startFallingPosition = transform.localPosition + Vector3.up * 0.07f;
        targetFloorPosition = transform.parent.InverseTransformPoint(finalWorldDest);

        // Plugin.ExtendedLogging($"KickDollLocalClient (LOCAL): {startFallingPosition} -> {targetFloorPosition}");
    }

    private Vector3 SnapToFloor(Vector3 desiredWorldPos)
    {
        Vector3 castFrom = desiredWorldPos + Vector3.up;
        Ray downRay = new Ray(castFrom, Vector3.down);

        if (Physics.Raycast(downRay, out RaycastHit downHit, 65f, dollBallMask, QueryTriggerInteraction.Ignore))
        {
            return downHit.point;
        }

        // If no floor found, just return the original
        return desiredWorldPos;
    }

    [Rpc(SendTo.NotOwner, RequireOwnership = false)]
    private void KickDollRpc(Vector3 dest)
    {
        KickDollLocalClient(dest);
    }

    public void FallWithCurve()
    {
        // Distance in local space
        float distance = (startFallingPosition - targetFloorPosition).magnitude;

        Vector3 euler = transform.localEulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        transform.localEulerAngles = Vector3.Lerp(transform.localEulerAngles, euler, 14f * Time.deltaTime / Mathf.Max(distance, 0.01f));

        Vector3 newPos = Vector3.Lerp(startFallingPosition, targetFloorPosition, dollFallCurve.Evaluate(fallTime));

        // Handle the vertical portion
        if (distance < 3f)
        {
            // No bounce
            float yLerp = Mathf.Lerp(startFallingPosition.y, targetFloorPosition.y, dollVerticalFallCurveNoBounce.Evaluate(fallTime)
            );
            newPos.y = yLerp;
        }
        else
        {
            // Standard bounce
            float yLerp = Mathf.Lerp(startFallingPosition.y, targetFloorPosition.y, dollVerticalFallCurve.Evaluate(fallTime)
            );
            newPos.y = yLerp + dollVerticalOffset.Evaluate(fallTime) * ballHitUpwardAmount;
        }

        // Actually move in local space
        transform.localPosition = newPos;

        // Progress along the curve
        fallTime += Mathf.Abs(Time.deltaTime * 12f / Mathf.Max(distance, 0.01f));

        // Once the curve is done, trigger “land” logic
        if (fallTime >= 1f && !hasHitGround)
        {
            PlayDropSFX();
        }
    }

    private void PlayDropSFX()
    {
        // Play the "doll lands" SFX if any
        dollAudio.PlayOneShot(ballHitFloorSFX[puppetRandom.Next(ballHitFloorSFX.Length)]);

        // Only the server should toggle the animation state
        animator.SetBool(IsKickedAnimation, false);

        hasHitGround = true;

        // Attempt to snap the doll to a valid NavMesh position
        SnapToClosestNavmeshPoint();

        // Re-enable the agent and smart navigator
        agent.enabled = true;
        smartAgentNavigator.enabled = true;
    }

    private void SnapToClosestNavmeshPoint()
    {
        float maxSearchRadius = 1f;  // Adjust as necessary
        Vector3 puppetPosition = transform.position;

        if (TryFindClosestNavmeshPoint(puppetPosition, maxSearchRadius, out Vector3 validPoint))
        {
            transform.position = validPoint;
            return;
        }

        if (playerControlled != null)
        {
            Vector3 playerPos = playerControlled.transform.position;
            if (TryFindClosestNavmeshPoint(playerPos, maxSearchRadius, out Vector3 fallbackPoint))
            {
                transform.position = fallbackPoint;
                return;
            }
        }
    }

    private bool TryFindClosestNavmeshPoint(Vector3 origin, float maxDistance, out Vector3 result)
    {
        if (NavMesh.SamplePosition(origin, out NavMeshHit navHit, maxDistance, NavMesh.AllAreas))
        {
            result = navHit.position;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    public void PlayFootstepSoundAnimEvent()
    {
        dollAudio.PlayOneShot(puppetFootstepSounds[puppetRandom.Next(puppetFootstepSounds.Length)]);
    }
}