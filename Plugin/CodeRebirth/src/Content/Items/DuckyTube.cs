using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Mono.Cecil.Cil;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;

namespace CodeRebirth.src.Content.Items;

public class DuckyTube : GrabbableObject, ICollisionProxy
{
    [field: SerializeField]
    public List<AudioClip> BumpSounds { get; private set; }
    [field: SerializeField]
    public List<AudioClip> FootstepSqueakSounds { get; private set; }
    [field: SerializeField]
    public AudioSource MainAudioSource { get; private set; }
    [field: SerializeField]
    public SphereCollider MainCollider { get; private set; }
    [field: SerializeField]
    public float BounceMultiplier { get; private set; } = 1f;

    private static List<Collider> _waterColliders = new();
    private PlayerControllerB? previouslyHeldByPlayer;
    private bool _wasWaterLastFrame;
    private bool _wasUnderwaterLastFrame;
    private Vector3 _lastColliderCenter;
    private bool _resetBouncePosition;

    internal static void Init()
    {
        new Hook(AccessTools.DeclaredMethod(typeof(QuicksandTrigger), "Awake"), QuicksandTrigger_Awake);
        new Hook(AccessTools.DeclaredMethod(typeof(QuicksandTrigger), "OnDestroy"), QuicksandTrigger_OnDestroy);
        IL.GameNetcodeStuff.PlayerControllerB.PlayFootstepSound += AdjustSoundForDuckyTube;
    }

    private static void AdjustSoundForDuckyTube(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(
            MoveType.Before,
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<PlayerControllerB>(nameof(PlayerControllerB.movementAudio)),
            il => il.MatchCall(out _),
            il => il.MatchLdfld<StartOfRound>(nameof(StartOfRound.footstepSurfaces)),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<PlayerControllerB>(nameof(PlayerControllerB.currentFootstepSurfaceIndex)),
            il => il.MatchLdelemRef()
        ))
        {
            Plugin.Logger.LogWarning("Failed to find the target instruction for AdjustSoundForDuckyTube");
            return;
        }

        cursor.Index++;
        cursor.Emit(OpCodes.Ldloc_2);
        cursor.EmitDelegate((PlayerControllerB player, float volume) =>
        {
            foreach (GrabbableObject grabbableObject in player.ItemSlots)
            {
                if (grabbableObject == null || grabbableObject is not DuckyTube duckyTube)
                {
                    continue;
                }

                int randomClipIndex = UnityEngine.Random.Range(0, duckyTube.FootstepSqueakSounds.Count);
                AudioClip clip = duckyTube.FootstepSqueakSounds[randomClipIndex];
                player.movementAudio.PlayOneShot(clip, volume);
                WalkieTalkie.TransmitOneShotAudio(player.movementAudio, clip, volume);
            }
        });
        cursor.Emit(OpCodes.Ldarg_0);
    }

    private static void QuicksandTrigger_Awake(RuntimeILReferenceBag.FastDelegateInvokers.Action<QuicksandTrigger> orig, QuicksandTrigger self)
    {
        orig(self);
        if (!self.isWater)
        {
            return;
        }

        _waterColliders.Add(self.GetComponent<Collider>());
    }

    private static void QuicksandTrigger_OnDestroy(RuntimeILReferenceBag.FastDelegateInvokers.Action<QuicksandTrigger> orig, QuicksandTrigger self)
    {
        orig(self);
        if (!self.isWater)
        {
            return;
        }

        _waterColliders.Remove(self.GetComponent<Collider>());
    }

    public override void Start()
    {
        base.Start();
        MainCollider.excludeLayers = 8;
    }

    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null)
        {
            if (_wasWaterLastFrame && previouslyHeldByPlayer != null)
            {
                previouslyHeldByPlayer.isMovementHindered--;
                previouslyHeldByPlayer.slipperyFloor = 0f;
                previouslyHeldByPlayer.slimeSlipAudio.mute = false;
            }
            _wasUnderwaterLastFrame = false;
            _wasWaterLastFrame = false;
            _resetBouncePosition = false;
            return;
        }

        HandleBounce();
        HandleWater();
        HandleQuicksand();
        previouslyHeldByPlayer = playerHeldBy;
    }

    private void HandleBounce()
    {
        Vector3 currentCenter = MainCollider.bounds.center;
        if (_resetBouncePosition)
        {
            _resetBouncePosition = false;
            _lastColliderCenter = currentCenter;
            return;
        }

        Vector3 displacement = currentCenter - _lastColliderCenter;
        if (displacement.sqrMagnitude <= 0.000001f)
        {
            _lastColliderCenter = currentCenter;
            return;
        }

        Vector3 direction = displacement.normalized;
        float distance = displacement.magnitude;
        float radius = MainCollider.radius * Mathf.Max(
            Mathf.Abs(MainCollider.transform.lossyScale.x),
            Mathf.Abs(MainCollider.transform.lossyScale.y),
            Mathf.Abs(MainCollider.transform.lossyScale.z));

        if (Physics.SphereCast(_lastColliderCenter, radius, direction, out RaycastHit hit, distance, MoreLayerMasks.CollidersAndRoomAndDefaultAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != MainCollider)
            {
                Vector3 velocity = displacement / Time.deltaTime;
                float intoSurfaceSpeed = Mathf.Clamp(Vector3.Dot(velocity, -hit.normal), 0f, 100f);

                if (intoSurfaceSpeed > 0f)
                {
                    playerHeldBy.externalForceAutoFade += hit.normal * intoSurfaceSpeed * (1f + BounceMultiplier);
                    MainAudioSource.PlayOneShot(BumpSounds[UnityEngine.Random.Range(0, BumpSounds.Count)]);
                }
            }
        }

        _lastColliderCenter = currentCenter;
    }

    private void HandleWater()
    {
        bool _inWater = false;
        foreach (Collider waterCollider in _waterColliders)
        {
            if (waterCollider.bounds.Contains(MainCollider.bounds.center))
            {
                _inWater = true;
                break;
            }
        }

        if (_inWater)
        {
            playerHeldBy.slipperyFloor = 8f;
            playerHeldBy.slimeSlipAudio.mute = true;
            if (!_wasWaterLastFrame)
            {
                playerHeldBy.isMovementHindered++;
                playerHeldBy.walkForce /= 2f;
            }
            playerHeldBy.ResetFallGravity();
            if (playerHeldBy.isFaceUnderwaterOnServer)
            {
                playerHeldBy.externalForceAutoFade += 2f * Time.deltaTime * Vector3.up;
            }
            else if (_wasUnderwaterLastFrame)
            {
                playerHeldBy.externalForceAutoFade += 35f * Time.deltaTime * Vector3.up;
            }
        }
        else if (_wasWaterLastFrame)
        {
            playerHeldBy.isMovementHindered--;
            playerHeldBy.walkForce *= 2f;
            playerHeldBy.slipperyFloor = 0f;
            playerHeldBy.slimeSlipAudio.mute = false;
        }

        _wasUnderwaterLastFrame = playerHeldBy.isFaceUnderwaterOnServer;
        _wasWaterLastFrame = _inWater;
    }

    private void HandleQuicksand()
    {
        if (playerHeldBy.playingQuickSpecialAnimation)
        {
            playerHeldBy.externalForceAutoFade += 35f * Time.deltaTime * Vector3.up;
        }
    }

    public override void GrabItem()
    {
        base.GrabItem();
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
        _resetBouncePosition = true;
    }

    public override void EquipItem()
    {
        base.EquipItem();
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
        _resetBouncePosition = true;
    }

    public override void PocketItem()
    {
        if (IsOwner)
        {
            playerHeldBy.IsInspectingItem = false;
            playerHeldBy.equippedUsableItemQE = false;
        }

        isPocketed = true;
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
        _resetBouncePosition = true;
    }
}