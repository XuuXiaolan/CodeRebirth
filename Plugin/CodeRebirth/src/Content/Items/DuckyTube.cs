using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Mono.Cecil.Cil;

namespace CodeRebirth.src.Content.Items;

public class DuckyTube : GrabbableObject
{
    [field: SerializeField]
    public List<AudioClip> BumpSounds { get; private set; }
    [field: SerializeField]
    public List<AudioClip> FootstepSqueakSounds { get; private set; }
    [field: SerializeField]
    public AudioSource MainAudioSource { get; private set; }
    [field: SerializeField]
    public Collider MainCollider { get; private set; }

    private static List<Collider> _waterColliders = new();
    private PlayerControllerB? previouslyHeldByPlayer;
    private bool _wasWaterLastFrame;
    private bool _wasUnderwaterLastFrame;

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

    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null)
        {
            if (_wasWaterLastFrame && previouslyHeldByPlayer != null)
            {
                previouslyHeldByPlayer.isMovementHindered--;
                previouslyHeldByPlayer.slipperyFloor = 0f;
            }
            _wasUnderwaterLastFrame = false;
            _wasWaterLastFrame = false;
            return;
        }

        previouslyHeldByPlayer = playerHeldBy;
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
            if (!_wasWaterLastFrame)
            {
                playerHeldBy.isMovementHindered++;
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
            playerHeldBy.slipperyFloor = 0f;
        }

        _wasUnderwaterLastFrame = playerHeldBy.isFaceUnderwaterOnServer;
        _wasWaterLastFrame = _inWater;
    }

    public override void GrabItem()
    {
        base.GrabItem();
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
    }

    public override void EquipItem()
    {
        base.EquipItem();
        parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
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
    }
}