using System.Collections.Generic;
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
        if (playerHeldBy == null || isInShipRoom || isInElevator)
        {
            return;
        }

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
            playerHeldBy.ResetFallGravity();
            if (playerHeldBy.isFaceUnderwaterOnServer)
            {
                playerHeldBy.externalForceAutoFade += Vector3.up * 0.1f;
            }
        }
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