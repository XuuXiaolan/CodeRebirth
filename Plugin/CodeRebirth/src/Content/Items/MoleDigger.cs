using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class MoleDigger : GrabbableObject
{
    public Animator moleAnimator = null!;
    public OwnerNetworkAnimator moleOwnerNetworkAnimator = null!;
    public Transform endTransform = null!;
    public GameObject lightObject = null!;
    public AudioSource idleSource = null!;
    public AudioClip attackIdleSound = null!;
    public AudioClip normalIdleSound = null!;
    public AudioSource audioSource = null!;
    public AudioClip DeactivateSound = null!;
    public AudioClip[] chainYankSound = [];
    public AudioClip activateSound = null!;

    private float _yankChainTimer = 0f;
    private float _hitTimer = 0f;
    private static readonly int ActivatedAnimation = Animator.StringToHash("activated"); // Bool
    private static readonly int PullChainAnimation = Animator.StringToHash("pullChain"); // Trigger
    private static readonly int AttackingAnimation = Animator.StringToHash("isAttacking"); // Bool
    public override void Start() // when battery runs out,
    {
        base.Start();
        lightObject.SetActive(false);
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
        ActivateLightObjectServerRpc(false);
        isBeingUsed = false;

        Plugin.InputActionsInstance.PullChain.performed -= OnChainYanked;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Plugin.ExtendedLogging($"Mole Digger Pocketed and isBeingUsed: {isBeingUsed}");

        moleAnimator.SetBool(ActivatedAnimation, false);
        moleAnimator.SetBool(AttackingAnimation, false);
        ActivateLightObjectServerRpc(false);
        isBeingUsed = false;
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        if (IsOwner)
        {
            moleAnimator.SetBool(ActivatedAnimation, false);
            moleAnimator.SetBool(AttackingAnimation, false);
        }
        lightObject.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateLightObjectServerRpc(bool enable)
    {
        ActivateLightObjectClientRpc(enable);
    }

    [ClientRpc]
    private void ActivateLightObjectClientRpc(bool enable)
    {
        idleSource.volume = enable ? 1f : 0f;
        idleSource.clip = normalIdleSound;
        audioSource.PlayOneShot(enable ? activateSound : DeactivateSound);
        lightObject.SetActive(enable);
    }

    public void OnChainYanked(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_yankChainTimer > 0f || insertedBattery.empty || isBeingUsed || moleAnimator.GetBool(ActivatedAnimation)) return;
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;

        if (btn.wasPressedThisFrame)
        {
            _yankChainTimer = 1f;
            audioSource.PlayOneShot(chainYankSound[UnityEngine.Random.Range(0, chainYankSound.Length)]);
            if (UnityEngine.Random.Range(0, 100) < 25)
            {
                moleAnimator.SetBool(ActivatedAnimation, true);
                ActivateLightObjectServerRpc(true);
                Plugin.ExtendedLogging($"Mole Digger Activated");
            }
            moleOwnerNetworkAnimator.SetTrigger(PullChainAnimation);
        }
    }

    public override void Update()
    {
        base.Update();
        _yankChainTimer -= Time.deltaTime;
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
        if (!moleAnimator.GetBool(ActivatedAnimation))
        {
            isBeingUsed = false;
            return;
        }
        Plugin.ExtendedLogging($"Mole Digger used and button down: {used} {buttonDown}");
        if (!buttonDown)
        {
            idleSource.clip = normalIdleSound;
            idleSource.Stop();
            idleSource.Play();
            if (IsOwner) moleAnimator.SetBool(AttackingAnimation, false);
        }
        else
        {
            idleSource.clip = attackIdleSound;
            idleSource.Stop();
            idleSource.Play();
            if (IsOwner) moleAnimator.SetBool(AttackingAnimation, true);
        }
    }
}