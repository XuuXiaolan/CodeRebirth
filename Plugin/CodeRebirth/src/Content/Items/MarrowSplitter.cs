using System;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class MarrowSplitter : GrabbableObject
{
    [SerializeField]
    private Animator _marrowSplitterAnimator = null!;
    [SerializeField]
    private OwnerNetworkAnimator _marrowSplitterOwnerNetworkAnimator = null!;
    [SerializeField]
    private Transform endTransform = null!;
    [SerializeField]
    private AudioSource _idleSource = null!;
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip _DeactivateSound = null!;
    [SerializeField]
    private AudioClip _activateSound = null!;
    [SerializeField]
    private AudioClip[] _tryHealPlayerSounds = [];

    private float _tryHealPlayerTimer = 0f;
    private float _hitTimer = 0f;

    private static readonly int AttackingAnimation = Animator.StringToHash("isAttacking"); // Bool

    public override void GrabItem()
    {
        base.GrabItem();
        Plugin.InputActionsInstance.MarrowHealPlayer.performed += OnTryHealPlayer;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Plugin.ExtendedLogging($"Marrow Splitter Discarded and isBeingUsed: {isBeingUsed}");
        _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;

        Plugin.InputActionsInstance.MarrowHealPlayer.performed -= OnTryHealPlayer;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Plugin.ExtendedLogging($"Marrow Splitter Pocketed and isBeingUsed: {isBeingUsed}");

        _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        if (IsOwner)
        {
            _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        }
    }

    public void OnTryHealPlayer(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_tryHealPlayerTimer > 0f || insertedBattery.empty || isBeingUsed || !isHeld || isPocketed) return;
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;

        if (!btn.wasPressedThisFrame)
            return;

        _tryHealPlayerTimer = 1f;
        _audioSource.PlayOneShot(_tryHealPlayerSounds[UnityEngine.Random.Range(0, _tryHealPlayerSounds.Length)]);

        int numHits = Physics.OverlapSphereNonAlloc(endTransform.position, 1f, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (_cachedColliders[i].transform == playerHeldBy.transform) continue;
            if (!_cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is not PlayerControllerB playerControllerB)
                continue;

            playerControllerB.DamagePlayerFromOtherClientServerRpc(-25, playerHeldBy.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
        }
    }

    public override void Update()
    {
        base.Update();
        _tryHealPlayerTimer -= Time.deltaTime;
        _hitTimer -= Time.deltaTime;
        if (!isBeingUsed || _hitTimer > 0 || playerHeldBy == null) return;
        DoHitStuff(1);
    }

    private Collider[] _cachedColliders = new Collider[8];
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();

    private void DoHitStuff(int damageToDeal)
    {
        _iHittableList.Clear();
        _enemyAIList.Clear();
        bool hitSomething = false;

        int numHits = Physics.OverlapSphereNonAlloc(endTransform.position, 1f, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (_cachedColliders[i].transform == playerHeldBy.transform) continue;
            if (_cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable))
            {
                if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect)
                {
                    if (_enemyAIList.Contains(enemyAICollisionDetect.mainScript))
                    {
                        continue;
                    }
                    _enemyAIList.Add(enemyAICollisionDetect.mainScript);
                }
                hitSomething = true;
                _iHittableList.Add(iHittable);
            }
        }
        foreach (var iHittable in _iHittableList)
        {
            if (IsOwner)
                iHittable.Hit(damageToDeal, playerHeldBy.gameplayCamera.transform.position, playerHeldBy, true, -1);
        }

        if (hitSomething)
        {
            _hitTimer = 0.4f;
            insertedBattery.charge -= 0.05f;
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        Plugin.ExtendedLogging($"Marrow Splitter used and button down: {used} {buttonDown}");
        if (!buttonDown)
        {
            _idleSource.volume = 0f;
            _idleSource.Stop();
            if (IsOwner) _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        }
        else
        {
            _idleSource.volume = 1f;
            _idleSource.Play();
            if (IsOwner) _marrowSplitterAnimator.SetBool(AttackingAnimation, true);
        }
    }
}