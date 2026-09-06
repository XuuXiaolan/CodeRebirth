using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class DuckyTube : GrabbableObject
{
    [field: SerializeField]
    public Collider MainCollider { get; private set; }

    private static List<Collider> _waterColliders = new();
    private PlayerControllerB? previouslyHeldByPlayer;
    private bool _wasWaterLastFrame;

    internal static void Init()
    {
        new Hook(AccessTools.DeclaredMethod(typeof(QuicksandTrigger), "Awake"), QuicksandTrigger_Awake);
        new Hook(AccessTools.DeclaredMethod(typeof(QuicksandTrigger), "OnDestroy"), QuicksandTrigger_OnDestroy);
    }

    private static void QuicksandTrigger_Awake(RuntimeILReferenceBag.FastDelegateInvokers.Action<QuicksandTrigger> orig, QuicksandTrigger self)
    {
        orig(self);
        _waterColliders.Add(self.GetComponent<Collider>());
    }

    private static void QuicksandTrigger_OnDestroy(RuntimeILReferenceBag.FastDelegateInvokers.Action<QuicksandTrigger> orig, QuicksandTrigger self)
    {
        orig(self);
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
                playerHeldBy.externalForceAutoFade += Vector3.up * Time.deltaTime;
            }
        }
        else if (_wasWaterLastFrame)
        {
            playerHeldBy.isMovementHindered--;
            playerHeldBy.slipperyFloor = 0f;
        }

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