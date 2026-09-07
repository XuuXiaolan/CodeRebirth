using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class FakeSnailCat : GrabbableObject
{
    [SerializeField]
    private OwnerNetworkAnimator _ownerNetworkAnimator = null!;
    [SerializeField]
    private Animator _animator = null!;

    [SerializeField]
    private Renderer _renderer = null!;

    internal PlayerControllerB lastOwner = null!;
    internal Vector3 localScale = Vector3.one;
    internal string snailCatName = "Salted Beef";
    internal float shiftHash = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            return;
        }

        StartCoroutine(DelayForBit());
    }

    private IEnumerator DelayForBit()
    {
        yield return null;
        yield return null;
        InitaliseFakeSnailCatServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitaliseFakeSnailCatServerRpc()
    {
        InitaliseFakeSnailCatClientRpc(lastOwner, localScale, snailCatName, shiftHash);
    }

    [ClientRpc]
    private void InitaliseFakeSnailCatClientRpc(PlayerControllerReference currentOwner, Vector3 scale, string name, float magicalHashNumber)
    {
        StartCoroutine(InitaliseFakeSnailCat(currentOwner, scale, name, magicalHashNumber));
    }

    public IEnumerator InitaliseFakeSnailCat(PlayerControllerB currentOwner, Vector3 scale, string name, float magicalHashNumber)
    {
        yield return new WaitForEndOfFrame();
        this.transform.position = StartOfRound.Instance.shipInnerRoomBounds.bounds.center;
        GameNetworkManager.Instance.localPlayerController.SetItemInElevator(true, true, this);

        lastOwner = currentOwner;
        if (lastOwner.IsLocalPlayer())
        {
            CRUtilities.MakePlayerGrabObject(lastOwner, this);
        }

        this.transform.localScale = scale;
        originalScale = scale;
        snailCatName = name;
        this.GetComponentInChildren<ScanNodeProperties>().headerText = snailCatName;
        _renderer.GetMaterial().SetFloat(SnailCatAI.ShiftHash, magicalHashNumber);
    }

    public override void EquipItem()
    {
        base.EquipItem();
        _animator.SetBool(SnailCatPhysicsProp.GrabbedAnimation, true);
        _animator.SetBool(SnailCatPhysicsProp.SittingAnimation, false);
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        _animator.SetBool(SnailCatPhysicsProp.GrabbedAnimation, false);
        _animator.SetBool(SnailCatPhysicsProp.SittingAnimation, true);
    }

    private bool destroyed = false;
    public override void Update()
    {
        base.Update();
        if (!IsServer || destroyed)
            return;

        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving)
            return;

        destroyed = true;

        EnemyType enemyType = LethalContent.Enemies[CodeRebirthEnemyKeys.RealEnemySnailCat].EnemyType;
        NetworkObjectReference netObjRef = RoundManager.Instance.SpawnEnemyGameObject(this.transform.position, -1, -1, enemyType);
        SnailCatAI snailCatAI = ((NetworkObject)netObjRef).GetComponent<SnailCatAI>();
        snailCatAI.wasFake = true;
        if (isHeld && playerHeldBy != null)
        {
            snailCatAI.playerHolding = playerHeldBy;
        }

        snailCatAI.fakeLocalScale = this.transform.localScale;
        snailCatAI.currentName = snailCatName;
        snailCatAI.shiftHash = _renderer.GetMaterial().GetFloat(SnailCatAI.ShiftHash);
        this.NetworkObject.Despawn();
    }
}