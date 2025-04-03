using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class MarrowSplitter : GrabbableObject
{
    public Animator marrowAnimator = null!;
    public OwnerNetworkAnimator marrowOwnerNetworkAnimator = null!;
    public Transform endTransform = null!;
    public AudioSource idleSource = null!;
    public AudioSource audioSource = null!;
    public AudioClip DeactivateSound = null!;
    public AudioClip activateSound = null!;

    private Collider[] cachedColliders = new Collider[8];
    private float hitTimer = 0f;
    private static readonly int AttackingAnimation = Animator.StringToHash("isAttacking"); // Bool

    public override void DiscardItem()
    {
        base.DiscardItem();
        Plugin.ExtendedLogging($"Marrow Splitter Discarded and isBeingUsed: {isBeingUsed}");
        marrowAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Plugin.ExtendedLogging($"Marrow Splitter Pocketed and isBeingUsed: {isBeingUsed}");
        marrowAnimator.SetBool(AttackingAnimation, false);
        isBeingUsed = false;
    }

    public override void Update()
    {
        base.Update();
        hitTimer -= Time.deltaTime;
        if (!isBeingUsed || hitTimer > 0 || playerHeldBy == null) return;
        int numHits = Physics.OverlapSphereNonAlloc(endTransform.position, 1f, cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].TryGetComponent(out IHittable iHittable) || cachedColliders[i].transform.position == playerHeldBy.transform.position) continue;
            if (IsOwner)
            {
                iHittable.Hit(1, playerHeldBy.gameplayCamera.transform.forward, playerHeldBy, true, -1);
            }
            hitTimer = 0.4f;
            Plugin.ExtendedLogging($"Marrow Splitter hit {cachedColliders[i].name}");
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        Plugin.ExtendedLogging($"Marrow Splitter used and button down: {used} {buttonDown}");
        if (!buttonDown)
        {
            idleSource.volume = 0f;
            idleSource.Stop();
            if (IsOwner) marrowAnimator.SetBool(AttackingAnimation, false);
        }
        else
        {
            idleSource.volume = 1f;
            idleSource.Play();
            if (IsOwner) marrowAnimator.SetBool(AttackingAnimation, true);
        }
    }
}