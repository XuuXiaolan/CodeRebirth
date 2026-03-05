using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.DevTools;

public enum DeathAnimation
{
    Default,
    HeadBurst,
    Spring,
    Electrocuted,
    ComedyMask,
    TragedyMask,
    Burnt,
    Snipped,
    SliceHead,
    Pieces,
}

public class StoatGun : GrabbableObject
{
    [field: SerializeField]
    public Animator Animator { get; private set; }

    [field: SerializeField]
    public int Damage { get; private set; }

    [field: SerializeField]
    public float Range { get; private set; }

    [field: SerializeField]
    public float Ammo { get; private set; }

    [field: SerializeField]
    public DeathAnimation DeathAnimation { get; private set; }

    [field: SerializeField]
    public CauseOfDeath CauseOfDeath { get; private set; }

    private bool damageSelf = false;

    private static readonly int IsHeldAnimation = Animator.StringToHash("IsHeld"); // Boolean
    private static readonly int ShootAnimation = Animator.StringToHash("Shoot"); // Trigger

    public override void Start()
    {
        base.Start();
        On.GameNetcodeStuff.PlayerControllerB.Crouch_performed += PlayerControllerB_Crouch_performed;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        On.GameNetcodeStuff.PlayerControllerB.Crouch_performed -= PlayerControllerB_Crouch_performed;
    }

    private static void PlayerControllerB_Crouch_performed(On.GameNetcodeStuff.PlayerControllerB.orig_Crouch_performed orig, GameNetcodeStuff.PlayerControllerB self, InputAction.CallbackContext context)
    {
        if (self.currentlyHeldObjectServer != null && self.currentlyHeldObjectServer is StoatGun)
        {
            return;
        }

        orig(self, context);
    }

    private bool holdingCtrl = false;

    public override void EquipItem()
    {
        base.EquipItem();
        Animator.SetBool(IsHeldAnimation, true);
        if (playerHeldBy.isCrouching)
        {
            playerHeldBy.Crouch(false);
        }
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Animator.SetBool(IsHeldAnimation, false);
    }

    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null || isPocketed)
        {
            return;
        }

        KeyboardInteractions();
        MouseInteractions();
    }

    private void KeyboardInteractions()
    {
        Keyboard? keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.ctrlKey.IsPressed())
        {
            holdingCtrl = true;
            playerHeldBy.twoHanded = true;
        }
        else
        {
            holdingCtrl = false;
            playerHeldBy.twoHanded = false;
        }

        if (keyboard.shiftKey.wasPressedThisFrame)
        {
            damageSelf = !damageSelf;
            SetHazardTooltips();
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            CauseOfDeath = (CauseOfDeath)(((int)CauseOfDeath + 1) % System.Enum.GetValues(typeof(CauseOfDeath)).Length);
            SetHazardTooltips();
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            DeathAnimation = (DeathAnimation)(((int)DeathAnimation + 1) % System.Enum.GetValues(typeof(DeathAnimation)).Length);
            SetHazardTooltips();
        }
    }

    private void MouseInteractions()
    {
        Mouse? mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (holdingCtrl)
        {
            Vector2 scrollValue = mouse.scroll.ReadValue().normalized;
            Damage += (int)scrollValue.y;
            SetHazardTooltips();
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (damageSelf)
            {
                playerHeldBy.DamagePlayer(Damage, true, true, CauseOfDeath, (int)DeathAnimation, false, default);
            }
        }
    }

    public void SetHazardTooltips()
    {
        HUDManager.Instance.ClearControlTips();

        List<string> tooltips =
        [
            "Gugugugugugu others : [LMB]",
            "Damage : " + Damage + " [Ctrl & Scroll]",
            "InstaKill : [Ctrl & Click]",
            "Self Damage : " + damageSelf + " [Shift]",
            "Death Cause : " + CauseOfDeath.ToString() + " [Q]",
            "Death Anima : " + DeathAnimation.ToString() + " [E]",
            "Remove all Enemies : [R]",
        ];

        HUDManager.Instance.ChangeControlTipMultiple(tooltips.ToArray(), false, null);
    }
}