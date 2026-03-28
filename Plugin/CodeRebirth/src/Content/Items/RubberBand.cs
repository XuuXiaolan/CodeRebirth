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
    private static readonly int ChargingAnimationHash = Animator.StringToHash("Charging"); // Bool
    private float _charging = 0f;

    public override void Update()
    {
        base.Update();
        if (isBeingUsed)
        {
            _charging += Time.deltaTime;
        }
        else
        {
            _charging = Mathf.Clamp01(_charging - Time.deltaTime);
        }

        if (isPocketed || !isHeld)
        {
            return;
        }

        float progress = UseCurveStrength.Evaluate(_charging);
        Animator.SetFloat(ProgressAnimationHash, progress);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        isBeingUsed = buttonDown;
        if (buttonDown)
        {
            Animator.SetBool(ChargingAnimationHash, true);
        }
        else
        {
            if (_charging <= 0.5f)
            {
                return;
            }
            float progress = UseCurveStrength.Evaluate(_charging);
            playerHeldBy.externalForceAutoFade += playerHeldBy.gameplayCamera.transform.forward * PushStrength * progress;
            _charging = 0f;
            Animator.SetFloat(ProgressAnimationHash, 1f);
            Animator.SetFloat(ProgressAnimationHash, 0f);
            Animator.SetBool(ChargingAnimationHash, false);
        }
        playerHeldBy.activatingItem = buttonDown;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        isBeingUsed = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        isBeingUsed = false;
    }
}