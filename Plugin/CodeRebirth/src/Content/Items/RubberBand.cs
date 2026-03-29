using Dawn;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class RubberBand : GrabbableObject
{
    [field: SerializeField]
    public Animator Animator { get; private set; }
    [field: SerializeField]
    public AnimationCurve UseCurveStrength { get; private set; }
    [field: SerializeField]
    public float PushStrength { get; private set; }
    // sort of like slime launcher from those minecraft mods, costs 1 health and sends you forward, try to get rid of the next incoming fall damage?

    private static readonly int ProgressAnimationHash = Animator.StringToHash("Progress"); // Float
    private static readonly int ChargingAnimationHash = Animator.StringToHash("Charging"); // Trigger
    private static readonly int ReleaseAnimationHash = Animator.StringToHash("Release"); // Trigger
    private static readonly int MaxChargeAnimationHash = Animator.StringToHash("MaxCharge"); // Trigger

    private float _charging = 0f;
    private float _cooldown = 0.5f;

    public override void Update()
    {
        base.Update();
        _cooldown -= Time.deltaTime;
        if (isPocketed || !isHeld)
        {
            return;
        }

        if (_charging >= 3f)
        {
            return;
        }

        if (isBeingUsed)
        {
            _charging += Time.deltaTime;
        }

        float progress = UseCurveStrength.Evaluate(_charging);
        Animator.SetFloat(ProgressAnimationHash, progress);
        if (_charging >= 3f)
        {
            Animator.SetTrigger(MaxChargeAnimationHash);
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (_cooldown > 0f)
        {
            return;
        }

        isBeingUsed = buttonDown;
        if (buttonDown)
        {
            playerHeldBy.isMovementHindered = Mathf.Clamp(1, 1, playerHeldBy.isMovementHindered);
            Animator.SetFloat(ProgressAnimationHash, 0f);
            Animator.SetTrigger(ChargingAnimationHash);
        }
        else
        {
            _cooldown = 0.5f;
            playerHeldBy.isMovementHindered = Mathf.Clamp(playerHeldBy.isMovementHindered - 1, 0, playerHeldBy.isMovementHindered);
            float progress = UseCurveStrength.Evaluate(_charging);
            playerHeldBy.externalForceAutoFade += playerHeldBy.gameplayCamera.transform.forward * PushStrength * progress * 10f;
            _charging = 0f;
            Animator.SetFloat(ProgressAnimationHash, 1f);
            Animator.SetTrigger(ReleaseAnimationHash);
        }
        playerHeldBy.activatingItem = buttonDown;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Animator.SetFloat(ProgressAnimationHash, 0f);
        Animator.ResetTrigger(ChargingAnimationHash);
        Animator.ResetTrigger(ReleaseAnimationHash);
        Animator.ResetTrigger(MaxChargeAnimationHash);
        isBeingUsed = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Animator.SetFloat(ProgressAnimationHash, 0f);
        Animator.ResetTrigger(ChargingAnimationHash);
        Animator.ResetTrigger(ReleaseAnimationHash);
        Animator.ResetTrigger(MaxChargeAnimationHash);
        isBeingUsed = false;
    }
}