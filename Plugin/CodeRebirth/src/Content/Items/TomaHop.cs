using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : CRWeapon
{
    [SerializeField]
    private float _groundLaunchTimer = 1f;

    [SerializeField]
    private AudioClip _launchUpSound = null!;

    public override void GrabItem()
    {
        base.GrabItem();
        OnEnemyHit.AddListener(OnEnemyHitEvent);
        Plugin.InputActionsInstance.JumpBoost.performed += OnGroundLaunch;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        OnEnemyHit.RemoveListener(OnEnemyHitEvent);
        Plugin.InputActionsInstance.JumpBoost.performed -= OnGroundLaunch;
    }

    public void OnGroundLaunch(InputAction.CallbackContext context)
    {
        if (_groundLaunchTimer > 0f)
            return;

        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
            return;

        if (isPocketed)
            return;

        if (!GameNetworkManager.Instance.localPlayerController.thisController.isGrounded)
            return;

        var btn = (ButtonControl)context.control;

        if (!btn.wasPressedThisFrame)
            return;

        _groundLaunchTimer = 1f;
        // weaponAudio.PlayOneShot(_launchUpSound);
        playerHeldBy.externalForces += Vector3.up * 50;
        playerHeldBy.externalForceAutoFade += Vector3.up * 50;
    }

    public override void Update()
    {
        base.Update();
        _groundLaunchTimer -= Time.deltaTime;
    }

    public void OnEnemyHitEvent(EnemyAI enemyAI)
    {
        // so fall value lower than -10 means the player is falling a sufficient speed,
        if (previousPlayerHeldBy.fallValue > 0)
            return;

        int newFallValue = (int)previousPlayerHeldBy.fallValue * -1;
        previousPlayerHeldBy.ResetFallGravity();
        previousPlayerHeldBy.externalForces = Vector3.up * newFallValue;
        previousPlayerHeldBy.externalForceAutoFade += Vector3.up * newFallValue;
        enemyAI.HitEnemyOnLocalClient(newFallValue / 10, transform.position, previousPlayerHeldBy, true, -1);
        // todo: press R and send the user up in the air.
        // todo: when not hitting anything whilst landing from a fall, take reduced fall damage.
        // todo: if hit an enemy, take no fall damage.
    }
}