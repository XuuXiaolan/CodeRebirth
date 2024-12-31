using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Enemies;
[RequireComponent(typeof(SmartAgentNavigator))]
public class PuppeteersVoodoo : NetworkBehaviour, IHittable
{
    public NavMeshAgent agent = null!;
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public Renderer renderer = null!;
    public Material[] materialVariants = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;
    [Header("Football-Like Kick Settings")]
    [Tooltip("Upward force added on kick.")]
    public float ballHitUpwardAmount = 0.5f;

    [Header("Curves for the Arc/Fall Behavior")]
    public AnimationCurve dollFallCurve;
    public AnimationCurve dollVerticalFallCurve;
    public AnimationCurve dollVerticalOffset;
    public AnimationCurve dollVerticalFallCurveNoBounce;

    [Tooltip("Collision layers for the 'kick' raycast, etc.")]
    public LayerMask dollBallMask = 369101057;

    [Space(5f)]
    public AudioClip[] hitBallSFX;
    public AudioClip[] ballHitFloorSFX;
    public AudioSource dollAudio;

    private Ray dollRay;
    private RaycastHit dollHit;
    private float hitTimer;
    private float fallTime;
    private bool hasHitGround;
    private Vector3 startFallingPosition;
    private Vector3 targetFloorPosition;
    private int damageTransferMultiplier = 20; // 20-30 as described
    [HideInInspector] public Puppeteer puppeteerCreatedBy = null!;
    [HideInInspector] public PlayerControllerB playerControlled = null!;

    private static readonly int OnHitAnimation = Animator.StringToHash("onHit"); // Triger
    private static readonly int IsKickedAnimation = Animator.StringToHash("isKicked"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float

    public static List<PuppeteersVoodoo> puppeteerList = new List<PuppeteersVoodoo>();

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

        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + puppeteerList.Count);
        if (random.Next(0, 10) == 0)
        {
            renderer.SetMaterial(materialVariants[random.Next(0, materialVariants.Length)]);
        }
    }

    public void Init(PlayerControllerB player, Puppeteer puppeteer, int damageTransferMultiplier)
    {
        playerControlled = player;
        puppeteerCreatedBy = puppeteer;
        this.damageTransferMultiplier = damageTransferMultiplier;
    }

    public void Update()
    {
        if (!hasHitGround && fallTime < 1f)
        {
            FallWithCurve();
        }

        if (agent.enabled)
        {
            animator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 2);
            smartAgentNavigator.DoPathingToDestination(
                playerControlled.transform.position,
                playerControlled.isInsideFactory,
                true,
                playerControlled
            );
        }
        else
        {
            animator.SetFloat(RunSpeedFloat, 0);
        }
    }

    public void OnDollDamaged(int damage, CauseOfDeath causeOfDeath)
    {
        if (playerControlled != null)
        {
            int finalDamage = Mathf.RoundToInt(damage * damageTransferMultiplier);
            DoHitAnimationServerRpc();
            playerControlled.DamagePlayer(finalDamage, true, true, causeOfDeath);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoHitAnimationServerRpc()
    {
        networkAnimator.SetTrigger(OnHitAnimation);
    }

    public IEnumerator BreakDoll()
    {
        animator.SetBool(IsDeadAnimation, true);
        yield return new WaitForSeconds(4f);
        CodeRebirthUtils.Instance.SpawnScrapServerRpc("PuppetScrap", transform.position);
        NetworkObject.Despawn();
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        // Transfer damage to the bound player
        OnDollDamaged(force, CauseOfDeath.Bludgeoning);

        // If a player is the cause, also "kick" this doll
        if (playerWhoHit != null)
        {
            Vector3 fromPosition = playerWhoHit.transform.position + Vector3.up;
            BeginKickDoll(fromPosition, triggerCall: false);
        }
        return true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (Time.realtimeSinceStartup - hitTimer < 0.5f)
            return;

        // If the object is tagged PlayerBody or Enemy
        if (other.CompareTag("PlayerBody") || other.CompareTag("Enemy"))
        {
            // Check line-of-sight
            if (Physics.Linecast(
                    other.gameObject.transform.position + Vector3.up,
                    transform.position + Vector3.up * 0.5f,
                    StartOfRound.Instance.collidersAndRoomMaskAndDefault,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            BeginKickDoll(other.transform.position + Vector3.up, triggerCall: true);
        }
    }

    private void BeginKickDoll(Vector3 hitFromPosition, bool triggerCall)
    {
        hitTimer = Time.realtimeSinceStartup;

        Vector3 destination = GetKickDestination(hitFromPosition);
        if (destination == Vector3.zero) return;

        KickDollLocalClient(destination);

        // If triggered from OnTriggerEnter, don't do the server RPC
        if (triggerCall)
        {
            if (IsServer)
            {
                animator.SetBool(IsKickedAnimation, true);
            }
            return;
        }
        // Otherwise, replicate the effect over the network
        int playerID = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, GameNetworkManager.Instance.localPlayerController);
        KickDollServerRpc(destination, playerID);
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
        smartAgentNavigator.enabled = false;
        agent.enabled = false;

        // SFX
        if (hitBallSFX != null && hitBallSFX.Length > 0 && dollAudio != null)
        {
            RoundManager.PlayRandomClip(dollAudio, hitBallSFX, true, 1f, 10419, 1000);
        }

        // STEP 1) Re-parent to a common props container
        // (If you had logic for inside/outside, you can adapt below.)
        transform.SetParent(StartOfRound.Instance.propsContainer, true);

        // Reset states for the arc
        fallTime = 0f;
        hasHitGround = false;

        // STEP 4) Snap to floor in world space before we switch to local:
        // (So the final arc won’t float in midair.)
        Vector3 finalWorldDest = SnapToFloor(destinationPos);

        // STEP 2) Convert everything to local coordinates
        startFallingPosition = transform.localPosition + Vector3.up * 0.07f;
        targetFloorPosition   = transform.parent.InverseTransformPoint(finalWorldDest);

        Plugin.ExtendedLogging($"KickDollLocalClient (LOCAL): {startFallingPosition} -> {targetFloorPosition}");
    }

    /// <summary>
    /// Rays downward from the desired "forward arc" position to ensure we land on the floor.
    /// </summary>
    private Vector3 SnapToFloor(Vector3 desiredWorldPos)
    {
        // We cast from slightly above, downward
        Vector3 castFrom = desiredWorldPos + Vector3.up;
        Ray downRay = new Ray(castFrom, Vector3.down);

        // Try up to 65f downward (adjust as needed)
        if (Physics.Raycast(downRay, out RaycastHit downHit, 65f, dollBallMask, QueryTriggerInteraction.Ignore))
        {
            // Return the floor point (plus a tiny offset if you like)
            return downHit.point; 
        }

        // If no floor found, just return the original
        return desiredWorldPos;
    }

    [ServerRpc(RequireOwnership = false)]
    private void KickDollServerRpc(Vector3 dest, int playerWhoKicked)
    {
        animator.SetBool(IsKickedAnimation, true);
        // If this server RPC was triggered by another player, run the local effect
        if (playerWhoKicked != Array.IndexOf(StartOfRound.Instance.allPlayerScripts, GameNetworkManager.Instance.localPlayerController))
        {
            KickDollLocalClient(dest);
        }

        // Then replicate to all other clients
        KickDollClientRpc(dest, playerWhoKicked);
    }

    [ClientRpc]
    private void KickDollClientRpc(Vector3 dest, int playerWhoKicked)
    {
        if (IsServer) return;
        // If it’s us who kicked, skip
        if (playerWhoKicked == Array.IndexOf(StartOfRound.Instance.allPlayerScripts, GameNetworkManager.Instance.localPlayerController))
            return;

        // Otherwise replicate the local effect
        KickDollLocalClient(dest);
    }

    public void FallWithCurve()
    {
        // Distance in local space
        float distance = (startFallingPosition - targetFloorPosition).magnitude;

        // Rotate in local space around the Y-axis, if you prefer
        // or you could just skip rotation in local space. 
        // 
        // If you want "flat" rotation, might do something like:
        Vector3 euler = transform.localEulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        transform.localEulerAngles = Vector3.Lerp(
            transform.localEulerAngles,
            euler,
            14f * Time.deltaTime / Mathf.Max(distance, 0.01f)
        );

        // STEP 2) Use local position arcs
        Vector3 newPos = Vector3.Lerp(
            startFallingPosition,
            targetFloorPosition,
            dollFallCurve.Evaluate(fallTime)
        );

        // Handle the vertical portion
        if (distance < 3f)
        {
            // No bounce
            float yLerp = Mathf.Lerp(
                startFallingPosition.y,
                targetFloorPosition.y,
                dollVerticalFallCurveNoBounce.Evaluate(fallTime)
            );
            newPos.y = yLerp;
        }
        else
        {
            // Standard bounce
            float yLerp = Mathf.Lerp(
                startFallingPosition.y,
                targetFloorPosition.y,
                dollVerticalFallCurve.Evaluate(fallTime)
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
        if (ballHitFloorSFX != null && ballHitFloorSFX.Length > 0 && dollAudio != null)
        {
            RoundManager.PlayRandomClip(dollAudio, ballHitFloorSFX, true, 1f, 10419, 1000);
        }
        if (IsServer) animator.SetBool(IsKickedAnimation, false);
        hasHitGround = true;
        agent.enabled = true;
        smartAgentNavigator.enabled = true;
    }
}
