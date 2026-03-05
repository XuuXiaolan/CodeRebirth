using System.Collections.Generic;
using Unity.Netcode;
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
    public StoatProjectile StoatProjectile { get; private set; }

    [field: SerializeField]
    public Transform GunTip { get; private set; }

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
        SetHazardTooltips();
    }

    internal static void Init()
    {
        On.GameNetcodeStuff.PlayerControllerB.Crouch_performed += PlayerControllerB_Crouch_performed;
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
        SetHazardTooltips();
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

        if (keyboard.pKey.wasPressedThisFrame)
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

        if (keyboard.rKey.wasPressedThisFrame)
        {
            KillEnemiesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void KillEnemiesServerRpc()
    {
        foreach (EnemyAI enemyAI in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemyAI == null || !enemyAI.NetworkObject.IsSpawned)
            {
                continue;
            }

            enemyAI.NetworkObject.Despawn(true);
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
            Animator.SetTrigger(ShootAnimation);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            if (damageSelf)
            {
                if (holdingCtrl)
                {
                    playerHeldBy.KillPlayer(Vector3.zero, true, CauseOfDeath, (int)DeathAnimation);
                }
                else
                {
                    playerHeldBy.DamagePlayer(Damage, true, true, CauseOfDeath, (int)DeathAnimation, false, default);
                }
            }
            else
            {
                StoatProjectile projectile = GameObject.Instantiate(StoatProjectile, GunTip.position, Quaternion.identity);
                projectile.SetupProjectile(playerHeldBy.gameplayCamera.transform.forward, Damage, playerHeldBy, holdingCtrl, CauseOfDeath, DeathAnimation);
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
            "InstaKill : [Ctrl & LMB]",
            "Self Damage : " + damageSelf + " [P]",
            "CoDeath : " + CauseOfDeath.ToString() + " [Q]",
            "Death Anim : " + DeathAnimation.ToString() + " [E]",
            "Remove all Enemies : [R]",
        ];

        HUDManager.Instance.ChangeControlTipMultiple(tooltips.ToArray(), false, null);
    }
}