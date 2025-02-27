using CodeRebirth.src.Util;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class MoleDigger : GrabbableObject
{
    public Animator moleAnimator = null!;
    public Transform endTransform = null!;

    private Collider[] cachedColliders = new Collider[8];
    private System.Random moleRandom = new();
    private float yankChainTimer = 0f;
    private float hitTimer = 0f;
    private static readonly int ActivatedAnimation = Animator.StringToHash("activated"); // Bool
    private static readonly int PullChainAnimation = Animator.StringToHash("pullChain"); // Trigger
    private static readonly int AttackingAnimation = Animator.StringToHash("isAttacking"); // Bool
    public override void Start() // when battery runs out,
    {
        base.Start();
        moleRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
    }

    public override void GrabItem()
    {
        base.GrabItem();
        Plugin.InputActionsInstance.PullChain.performed += OnChainYanked;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Plugin.ExtendedLogging($"Mole Digger Discarded and isBeingUsed: {isBeingUsed}");
        moleAnimator.SetBool(ActivatedAnimation, false);
        moleAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;

        Plugin.InputActionsInstance.PullChain.performed -= OnChainYanked;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Plugin.ExtendedLogging($"Mole Digger Pocketed and isBeingUsed: {isBeingUsed}");

        moleAnimator.SetBool(ActivatedAnimation, false);
        moleAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        moleAnimator.SetBool(ActivatedAnimation, false);
        moleAnimator.SetBool(AttackingAnimation, false);
    }

    public void OnChainYanked(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (yankChainTimer > 0f || isBeingUsed || moleAnimator.GetBool(ActivatedAnimation)) return;
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;

        if (btn.wasPressedThisFrame)
        {
            yankChainTimer = 1f;
            if (moleRandom.Next(0, 100) < 25)
            {
                moleAnimator.SetBool(ActivatedAnimation, true);
                Plugin.ExtendedLogging($"Mole Digger Activated");
            }
            moleAnimator.SetTrigger(PullChainAnimation);
        }
    }

    public override void Update()
    {
        base.Update();
        yankChainTimer -= Time.deltaTime;
        hitTimer -= Time.deltaTime;
        if (!isBeingUsed || hitTimer > 0 || playerHeldBy == null) return;
        int numHits = Physics.OverlapSphereNonAlloc(endTransform.position, 1f, cachedColliders, CodeRebirthUtils.Instance.enemiesMask, QueryTriggerInteraction.Collide);
        bool hitSomething = false;
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].TryGetComponent(out IHittable iHittable)) continue;
            if (IsOwner)
            {
                iHittable.Hit(1, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, -1);
            }
            hitSomething = true;
            Plugin.ExtendedLogging($"Mole Digger hit {cachedColliders[i].name}");
        }
        if (hitSomething)
        {
            hitTimer = 0.5f;
            insertedBattery.charge -= 0.1f;
            // take some battery charge.
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!moleAnimator.GetBool(ActivatedAnimation)) return;
        Plugin.ExtendedLogging($"Mole Digger used and button down: {used} {buttonDown}");
        if (!buttonDown)
        {
            moleAnimator.SetBool(AttackingAnimation, false);
        }
        else
        {
            moleAnimator.SetBool(AttackingAnimation, true);
        }
    }
}