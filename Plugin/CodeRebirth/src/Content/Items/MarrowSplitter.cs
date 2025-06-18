using System;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
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
    private SkinnedMeshRenderer _skinnedMeshRenderer = null!;
    [SerializeField]
    private ParticleSystem _bloodParticles = null!;
    [SerializeField]
    private Transform _endTransform = null!;
    [SerializeField]
    private AudioSource _idleSource = null!;
    [SerializeField]
    private AudioClip[] _fillUpSounds = [];
    [SerializeField]
    private AudioClip[] _hitEnemySounds = [];
    [SerializeField]
    private AudioClip _healingSound = null!;

    [SerializeField]
    private AudioClip[] _hitObjectSounds = [];

    [SerializeField]
    private int _increaseAmount = 1;
    [SerializeField]
    private int _decreaseAmount = 2;

    private float _tryHealPlayerTimer = 1f;
    private float _hitTimer = 0f;
    private bool _isHealing = false;

    private static readonly int AttackingAnimation = Animator.StringToHash("isAttacking"); // Bool

    public override void EquipItem()
    {
        base.EquipItem();
        Plugin.InputActionsInstance.MarrowHealPlayer.performed += OnTryStartHealPlayer;
        Plugin.InputActionsInstance.MarrowHealPlayer.canceled += OnTryCancelHealPlayer;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Plugin.ExtendedLogging($"Marrow Splitter Discarded and isBeingUsed: {isBeingUsed}");

        if (IsOwner)
            _marrowSplitterAnimator.SetBool(AttackingAnimation, false);

        isBeingUsed = false;

        Plugin.InputActionsInstance.MarrowHealPlayer.performed -= OnTryStartHealPlayer;
        Plugin.InputActionsInstance.MarrowHealPlayer.canceled -= OnTryCancelHealPlayer;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Plugin.ExtendedLogging($"Marrow Splitter Pocketed and isBeingUsed: {isBeingUsed}");

        if (IsOwner)
            _marrowSplitterAnimator.SetBool(AttackingAnimation, false);

        _isHealing = false;
        _idleSource.volume = 0f;
        _idleSource.Stop();
        isBeingUsed = false;
        Plugin.InputActionsInstance.MarrowHealPlayer.performed -= OnTryStartHealPlayer;
        Plugin.InputActionsInstance.MarrowHealPlayer.canceled -= OnTryCancelHealPlayer;
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        if (IsOwner)
            _marrowSplitterAnimator.SetBool(AttackingAnimation, false);

        _isHealing = false;
        _idleSource.volume = 0f;
        _idleSource.Stop();
        isBeingUsed = false;
        Plugin.InputActionsInstance.MarrowHealPlayer.performed -= OnTryStartHealPlayer;
        Plugin.InputActionsInstance.MarrowHealPlayer.canceled -= OnTryCancelHealPlayer;
    }

    public void OnTryStartHealPlayer(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isBeingUsed || !isHeld || isPocketed)
            return;

        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
            return;

        var btn = (ButtonControl)context.control;

        if (!btn.wasPressedThisFrame)
            return;

        int currentAmount = Mathf.FloorToInt(_skinnedMeshRenderer.GetBlendShapeWeight(0));

        if (currentAmount <= 0)
            return;

        _marrowSplitterAnimator.SetBool(AttackingAnimation, true);
        ActivateOrStopSourceForHealingServerRpc(true);
    }

    public void OnTryCancelHealPlayer(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy || isPocketed || !isHeld)
            return;

        var btn = (ButtonControl)context.control;

        if (!btn.wasReleasedThisFrame)
            return;

        if (!isBeingUsed)
            return;

        _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        ActivateOrStopSourceForHealingServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateOrStopSourceForHealingServerRpc(bool activate)
    {
        ActivateOrStopSourceForHealingClientRpc(activate);
    }

    [ClientRpc]
    private void ActivateOrStopSourceForHealingClientRpc(bool activate)
    {
        if (activate)
        {
            isBeingUsed = true;
            _isHealing = true;
            _idleSource.PlayOneShot(_healingSound);
            _idleSource.volume = 1f;
            _idleSource.Play();
        }
        else
        {
            isBeingUsed = false;
            _isHealing = false;
            _idleSource.volume = 0f;
            _idleSource.Stop();
        }
    }

    public override void Update()
    {
        base.Update();
        if (_isHealing && isBeingUsed)
            _tryHealPlayerTimer -= Time.deltaTime;

        if (!_isHealing && isBeingUsed)
            _hitTimer -= Time.deltaTime;

        if (!isBeingUsed || playerHeldBy == null || isPocketed) return;
        if (_isHealing && _tryHealPlayerTimer <= 0)
        {
            DoHealingPlayers();
        }
        else if (!_isHealing && _hitTimer <= 0)
        {
            DoHitStuff(1);
        }
    }

    private Collider[] _cachedColliders = new Collider[8];
    private List<IHittable> _iHittableList = new();
    private List<EnemyAI> _enemyAIList = new();

    private void DoHitStuff(int damageToDeal)
    {
        _iHittableList.Clear();
        _enemyAIList.Clear();
        bool hitSomething = false;

        int numHits = Physics.OverlapSphereNonAlloc(_endTransform.position, 1f, _cachedColliders, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            if (!collider.TryGetComponent(out IHittable hittable))
                continue;

            if (hittable is PlayerControllerB player)
            {
                if (player == playerHeldBy)
                    continue;

                hitSomething = true;
                player.DamagePlayer(damageToDeal, true, false, CauseOfDeath.Fan, 0, false, default);
            }
            else if (hittable is EnemyAICollisionDetect enemy)
            {
                hitSomething = true;
                if (_enemyAIList.Contains(enemy.mainScript))
                    continue;

                if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
                    continue;

                _enemyAIList.Add(enemy.mainScript);
                enemy.mainScript.HitEnemyOnLocalClient(1, this.transform.position, playerHeldBy, true, -1);
            }
            else
            {
                hitSomething = true;
                if (GameNetworkManager.Instance.localPlayerController != playerHeldBy)
                    continue;

                hittable.Hit(1, this.transform.position, playerHeldBy, true, -1);
            }
        }

        if (hitSomething)
        {
            _bloodParticles.Play(true);
            _idleSource.PlayOneShot(_hitEnemySounds[UnityEngine.Random.Range(0, _hitEnemySounds.Length)]);
            _hitTimer = 0.4f;
            insertedBattery.charge -= 0.05f;
            float currentAmount = _skinnedMeshRenderer.GetBlendShapeWeight(0);
            float newAmount = Mathf.Clamp(currentAmount + _increaseAmount, 0, 100);
            if (newAmount > currentAmount)
            {
                _idleSource.PlayOneShot(_fillUpSounds[UnityEngine.Random.Range(0, _fillUpSounds.Length)]);
            }
            Plugin.ExtendedLogging($"Increasing blendshape weight to {newAmount}");
            _skinnedMeshRenderer.SetBlendShapeWeight(0, newAmount);
        }
        else
        {
            numHits = Physics.OverlapSphereNonAlloc(_endTransform.position, 1f, _cachedColliders, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < numHits; i++)
            {
                for (int j = 0; j < StartOfRound.Instance.footstepSurfaces.Length; j++)
                {
                    if (!_cachedColliders[i].gameObject.CompareTag(StartOfRound.Instance.footstepSurfaces[j].surfaceTag)) continue;
                    HandleHittingSurface(j);
                    _hitTimer = 0.4f;
                    break;
                }
            }
        }
    }

    private void DoHealingPlayers()
    {
        int numHits = Physics.OverlapSphereNonAlloc(_endTransform.position, 1f, _cachedColliders, MoreLayerMasks.PlayersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        bool healingAnotherPlayer = false;
        for (int i = 0; i < numHits; i++)
        {
            if (_cachedColliders[i].transform == playerHeldBy.transform)
                continue;

            if (!_cachedColliders[i].gameObject.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is not PlayerControllerB playerControllerB || playerControllerB == playerHeldBy)
                continue;

            if (playerControllerB.IsPseudoDead() || playerControllerB.isPlayerDead || !playerControllerB.isPlayerControlled)
                continue;

            if (playerControllerB.health >= 100)
                continue;

            int currentAmount = Mathf.FloorToInt(_skinnedMeshRenderer.GetBlendShapeWeight(0));
            _skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(currentAmount - _decreaseAmount, 0, 100));

            healingAnotherPlayer = true;
            _bloodParticles.Play(true);
            if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
            {
                playerControllerB.DamagePlayerFromOtherClientServerRpc(-_decreaseAmount, playerHeldBy.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
            }

            if (currentAmount - _decreaseAmount <= 0)
            {
                isBeingUsed = false;
                _isHealing = false;
                _idleSource.volume = 0f;
                _idleSource.Stop();

                if (IsOwner)
                    _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
                return;
            }
        }

        if (!healingAnotherPlayer && playerHeldBy.health < 100)
        {
            _bloodParticles.Play(true);
            int currentAmount = Mathf.FloorToInt(_skinnedMeshRenderer.GetBlendShapeWeight(0));
            _skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(currentAmount - _decreaseAmount, 0, 100));
            playerHeldBy.DamagePlayer(-_decreaseAmount, true, false, CauseOfDeath.Stabbing, 0, false, default);

            if (currentAmount - _decreaseAmount <= 0)
            {
                isBeingUsed = false;
                _isHealing = false;
                _idleSource.volume = 0f;
                _idleSource.Stop();

                if (IsOwner)
                    _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
            }
        }
    }

    private void HandleHittingSurface(int surfaceID)
    {
        if (surfaceID == -1) return;
        _idleSource.PlayOneShot(StartOfRound.Instance.footstepSurfaces[surfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(_idleSource, StartOfRound.Instance.footstepSurfaces[surfaceID].hitSurfaceSFX);
        _idleSource.PlayOneShot(_hitObjectSounds[UnityEngine.Random.Range(0, _hitObjectSounds.Length)]);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (_isHealing)
            return;

        Plugin.ExtendedLogging($"Marrow Splitter used and button down: {used} {buttonDown}");
        if (!buttonDown)
        {
            isBeingUsed = false;
            _idleSource.volume = 0f;
            _idleSource.Stop();
            if (IsOwner)
                _marrowSplitterAnimator.SetBool(AttackingAnimation, false);
        }
        else
        {
            isBeingUsed = true;
            _idleSource.volume = 1f;
            _idleSource.Play();
            if (IsOwner)
                _marrowSplitterAnimator.SetBool(AttackingAnimation, true);
        }
    }
}