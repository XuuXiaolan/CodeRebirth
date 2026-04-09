
using System;
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

    private int hitsInSuccession = 0;

    private float hitTimer = 1.5f;

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
        weaponAudio.PlayOneShot(_launchUpSound);
        playerHeldBy.externalForces += Vector3.up * 50;
        playerHeldBy.externalForceAutoFade += Vector3.up * 50;
    }

    public override void Update()
    {
        base.Update();
        _groundLaunchTimer -= Time.deltaTime;
        if (hitsInSuccession <= 0)
        {
            return;
        }

        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0)
        {
            hitsInSuccession--;
            hitTimer = 2f;
        }
    }

    public void OnEnemyHitEvent(EnemyAI enemyAI)
    {
        // so fall value lower than -10 means the player is falling a sufficient speed,
        if (previousPlayerHeldBy.fallValue >= -3)
            return;

        float newFallValue = previousPlayerHeldBy.fallValue * -1;
        previousPlayerHeldBy.ResetFallGravity();
        Vector3 randomDirectionOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized * 4f;
        Vector3 pushValue = Vector3.up * newFallValue + randomDirectionOffset * Mathf.Clamp(hitsInSuccession, 0, 10f);
        previousPlayerHeldBy.externalForces = pushValue;
        previousPlayerHeldBy.externalForceAutoFade += pushValue;
        enemyAI.HitEnemyOnLocalClient(Math.Clamp((int)(newFallValue / 10) - 1, 0, 10), transform.position, previousPlayerHeldBy, true, -1);
        hitsInSuccession++;
        hitTimer = 2f;
    }
}