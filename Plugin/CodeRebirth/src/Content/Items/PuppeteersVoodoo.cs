using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Items;
[RequireComponent(typeof(SmartAgentNavigator))]
public class PuppeteersVoodoo : NetworkBehaviour, IHittable
{
    public NavMeshAgent agent = null!;
    public SmartAgentNavigator smartAgentNavigator = null!;
    [HideInInspector] public Puppeteer puppeteerCreatedBy = null!;
    [HideInInspector] public PlayerControllerB playerControlled = null!;

    [Header("Voodoo Damage Settings")]
    [Tooltip("Multiplier for the damage transferred to the linked player.")]
    [SerializeField]
    private int damageTransferMultiplier = 20; // 20-30 as described, can be randomized or set

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

    [Tooltip("Optional collider transform if you want to scale or manipulate differently.")]
    public Transform dollCollider;

    private Ray dollRay;
    private RaycastHit dollHit;
    private float hitTimer;
    private float fallTime;
    private bool hasHitGround;
    private int previousPlayerHit;
    private Vector3 startFallingPosition;
    private Vector3 targetFloorPosition;

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
        if (puppeteerCreatedBy == null)
        {
            if (IsServer)
            {
                NetworkObject.Despawn();
            }
            return;
        }

        // Example: set NavMesh/AI behavior if needed
        smartAgentNavigator.SetAllValues(puppeteerCreatedBy.isOutside);
    }

    public void Update()
    {
        // Example: if puppeteer is dead, we remove the doll
        if (puppeteerCreatedBy.isEnemyDead && IsServer)
        {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("VoodooDoll", transform.position, false, true, 0);
            NetworkObject.Despawn();
        }

        // If you want constant "arc" or "fall" behavior, run it here:
        if (!hasHitGround && fallTime < 1f)
        {
            FallWithCurve();
        }
    }

    public void OnDollDamaged(int damage, CauseOfDeath causeOfDeath)
    {
        if (playerControlled != null)
        {
            int finalDamage = Mathf.RoundToInt(damage * damageTransferMultiplier);
            playerControlled.DamagePlayer(finalDamage, true, true, causeOfDeath);
        }
    }

    public void BreakDoll()
    {
        // e.g. some unique break effects or despawn
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        // Transfer damage to the bound player
        OnDollDamaged(force, CauseOfDeath.Bludgeoning);

        // If a player is the cause, also "kick" this doll
        if (playerWhoHit != null)
        {
            // The "hitDirection" can be used to position from where the doll is "struck"
            Vector3 fromPosition = playerWhoHit.transform.position + Vector3.up;

            // Initiate our new "kick" effect
            BeginKickDoll(fromPosition, (int)playerWhoHit.playerClientId);
        }
        return true;
    }

    private void BeginKickDoll(Vector3 hitFromPosition, int playerId)
    {
        // Avoid spamming the same user in quick succession
        if (previousPlayerHit == playerId && Time.realtimeSinceStartup - hitTimer < 0.35f)
            return;

        hitTimer = Time.realtimeSinceStartup;
        previousPlayerHit = playerId;

        // 1. Determine a “destination” for the arc or bounce
        Vector3 destination = GetKickDestination(hitFromPosition);
        if (destination == Vector3.zero) return;

        // 2. Execute local client effect
        KickDollLocalClient(destination);

        // 3. Call the server RPC so the server can replicate to other clients
        KickDollServerRpc(destination, playerId);
    }

    private Vector3 GetKickDestination(Vector3 hitFromPosition)
    {
        // Example: simply add forward impulse from the hit direction
        // Or do raycasts to ensure we place the doll in a valid spot.
        Vector3 direction = (transform.position - hitFromPosition).normalized;
        direction.y = 0.2f; // give it some upward angle
        float distanceToTravel = 10f;

        // Setup a ray
        dollRay = new Ray(transform.position + Vector3.up * 0.22f, direction);
        if (Physics.Raycast(dollRay, out dollHit, distanceToTravel, dollBallMask, QueryTriggerInteraction.Ignore))
        {
            // If we hit something, put the final position slightly before impact
            return dollRay.GetPoint(Mathf.Max(0.1f, dollHit.distance - 0.05f));
        }
        else
        {
            // No collisions, just pick a point at "distanceToTravel"
            return dollRay.GetPoint(distanceToTravel);
        }
    }

    private void KickDollLocalClient(Vector3 destinationPos)
    {
        smartAgentNavigator.enabled = false;
        agent.enabled = false;
        // Example SFX
        if (hitBallSFX != null && hitBallSFX.Length > 0 && dollAudio != null)
        {
            RoundManager.PlayRandomClip(dollAudio, hitBallSFX, true, 1f, 10419, 1000);
        }

        // Reset states for the arc
        fallTime = 0f;
        hasHitGround = false;

        startFallingPosition = transform.position + Vector3.up * 0.07f;
        targetFloorPosition = destinationPos;
    }

    [ServerRpc(RequireOwnership = false)]
    private void KickDollServerRpc(Vector3 dest, int playerWhoKicked)
    {
        if (playerWhoKicked != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
        {
            KickDollLocalClient(dest);
            previousPlayerHit = playerWhoKicked;
        }

        // Now replicate to all clients
        KickDollClientRpc(dest, playerWhoKicked);
    }

    [ClientRpc]
    private void KickDollClientRpc(Vector3 dest, int playerWhoKicked)
    {
        if (IsServer) return;
        if (playerWhoKicked == (int)GameNetworkManager.Instance.localPlayerController.playerClientId) return;

        // Otherwise, replicate the local effect
        previousPlayerHit = playerWhoKicked;
        KickDollLocalClient(dest);
    }

    public void FallWithCurve()
    {
        float distance = (startFallingPosition - targetFloorPosition).magnitude;

        // Possibly rotate the doll in flight
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, transform.eulerAngles.y, 0), 
            14f * Time.deltaTime / distance);

        // Horizontal movement
        Vector3 newPos = Vector3.Lerp(
            startFallingPosition, 
            targetFloorPosition, 
            dollFallCurve.Evaluate(fallTime));

        // Vertical movement: if short distance => no bounce
        if (distance < 3f)
        {
            newPos.y = Mathf.Lerp(
                startFallingPosition.y,
                targetFloorPosition.y,
                dollVerticalFallCurveNoBounce.Evaluate(fallTime)
            );
        }
        else
        {
            newPos.y = Mathf.Lerp(
                startFallingPosition.y,
                targetFloorPosition.y,
                dollVerticalFallCurve.Evaluate(fallTime)
            );
            // Optional vertical offset for a bounce
            newPos.y += dollVerticalOffset.Evaluate(fallTime) * ballHitUpwardAmount;
        }

        transform.position = newPos;
        fallTime += Mathf.Abs(Time.deltaTime * 12f / distance);

        // If we have basically completed the arc, play "hit ground" SFX
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
        hasHitGround = true;
        agent.enabled = true;
        smartAgentNavigator.enabled = true;
    }
}