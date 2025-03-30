using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Items;
public class CRWeapon : GrabbableObject // partly or mostly modified from JLL's JMeleeWeapon
{
    [Header("Melee Weapon")]
    public int HitForce = 1;
    public Transform weaponTip = null!;

    [Tooltip("Shovel Default: 1.5\nKnife Default: 0.3")]
    public float weaponRange = 1.5f;
    [Tooltip("Shovel Default: 0.3\nKnife Default: 0.43")]
    public float weaponCooldown = 0.3f;

    [Tooltip("Leave blank for no blood particle on hit")]
    public ParticleSystem? bloodParticle = null;

    private float timeAtLastDamageDealt;

    [Tooltip("Player is the player who swung the weapon")]
    public UnityEvent<PlayerControllerB> OnHitSuccess = new UnityEvent<PlayerControllerB>();
    [Tooltip("Default: 1\nPop Butlers: 5")]
    public int HitId = 1;

    [Header("Heavy Weapons")]
    [Tooltip("Heavy Weapons are similar to the shovel or signs.\nNon Heavy Weapons are similar to the Kitchen Knife")]
    public bool isHeavyWeapon = true;
    [Tooltip("Shovel Default: 0.35")]
    public float reelingTime = 0.35f;
    private float reelingAnimSpeed = 1f;
    [Tooltip("Shovel Default: 0.13")]
    public float swingTime = 0.13f;

    private bool isHoldingButton;

    [Header("Audio")]
    public AudioClip[] hitSFX;
    [Tooltip("Only used for Heavy Weapons")]
    public AudioClip[] reelUpSFX;
    public AudioClip[] swingSFX;
    public AudioClip[] finishReelUpSFX;
    public AudioClip[] hitEnemySFX;
    public AudioSource weaponAudio;

    [HideInInspector] public float heldOverHeadTimer = 0f;
    private List<IHittable> iHittableList = new();
    private Coroutine? reelingRoutine = null;
    private RaycastHit[] cachedRaycastHits = new RaycastHit[8];
    private PlayerControllerB previousPlayerHeldBy;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        if (playerHeldBy == null)
        {
            return;
        }

        previousPlayerHeldBy = playerHeldBy;

        if (itemProperties.requiresBattery && insertedBattery.empty)
        {
            return;
        }

        if (isHeavyWeapon)
        {
            isHoldingButton = buttonDown;
            if (!buttonDown || reelingRoutine != null) return;
            reelingRoutine = StartCoroutine(ReelBackWeapon());
        }
        else
        {
            PlayRandomSFX(swingSFX);

            if (previousPlayerHeldBy.IsOwner)
            {
                previousPlayerHeldBy.playerBodyAnimator.SetTrigger("UseHeldItem1");
            }

            if (IsOwner && Time.realtimeSinceStartup - timeAtLastDamageDealt > weaponCooldown)
            {
                HitWeapon();
            }
        }
    }

    private IEnumerator ReelBackWeapon()
    {
        heldOverHeadTimer = 0f;
        playerHeldBy.activatingItem = true;
        playerHeldBy.twoHanded = true;
        playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
        playerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: true);
        if (playerHeldBy.IsOwner)
        {
            reelingAnimSpeed = 0.35f / reelingTime;
            playerHeldBy.playerBodyAnimator.speed = reelingAnimSpeed;
        }
        PlayRandomSFX(reelUpSFX);
        ReelUpSFXServerRpc();
        yield return new WaitForSeconds(reelingTime);
        PlayRandomSFX(finishReelUpSFX);
        while (isHoldingButton && isHeld)
        {
            heldOverHeadTimer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitUntil(() => !isHoldingButton || !isHeld);
        if (playerHeldBy.IsOwner) playerHeldBy.playerBodyAnimator.speed = 1f;
        SwingHeavyWeapon(!isHeld);
        float timeElapsed = 0f;
        bool success = false;
        while (timeElapsed <= swingTime && !success)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
            yield return new WaitForEndOfFrame();
            success = HitWeapon(!isHeld);
        }
        yield return new WaitForSeconds(weaponCooldown);
        heldOverHeadTimer = 0f;
        reelingRoutine = null;
    }

    [ServerRpc]
    public void ReelUpSFXServerRpc()
    {
        ReelUpSFXClientRpc();
    }

    [ClientRpc]
    public void ReelUpSFXClientRpc()
    {
        PlayRandomSFX(reelUpSFX);
    }

    public override void DiscardItem()
    {
        if (playerHeldBy != null)
        {
            playerHeldBy.activatingItem = false;
            if (playerHeldBy.IsOwner) playerHeldBy.playerBodyAnimator.speed = 1f;
        }

        base.DiscardItem();
    }

    public virtual void SwingHeavyWeapon(bool cancel = false)
    {
        previousPlayerHeldBy.playerBodyAnimator.SetBool("reelingUp", value: false);
        if (!cancel)
        {
            PlayRandomSFX(swingSFX);
            previousPlayerHeldBy.UpdateSpecialAnimationValue(specialAnimation: true, (short)previousPlayerHeldBy.transform.localEulerAngles.y, 0.4f);
        }
    }

    public bool HitWeapon(bool cancel = false)
    {
        if (previousPlayerHeldBy == null)
        {
            Plugin.Logger.LogError("Previousplayerheldby is null on this client when HitShovel is called.");
            return false;
        }

        previousPlayerHeldBy.activatingItem = false;
        int surfaceSound = -1;
        bool hitSomething = false;

        if (cancel) return false;
        previousPlayerHeldBy.twoHanded = false;
        int numHits = Physics.SphereCastNonAlloc(weaponTip.position, weaponRange / 2f, weaponTip.forward, cachedRaycastHits, 5f, CodeRebirthUtils.Instance.collidersAndRoomAndRailingAndTerrainAndHazardAndVehicleMask, QueryTriggerInteraction.Ignore);
        List<RaycastHit> objectsHitByShovelList = cachedRaycastHits.Where(hit => hit.transform != null).OrderBy(x => x.distance).ToList();

        iHittableList.Clear();

        foreach (RaycastHit hit in objectsHitByShovelList)
        {
            if (hit.collider.gameObject.layer == 8 || hit.collider.gameObject.layer == 11)
            {
                if (hit.collider.isTrigger) continue;
                for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
                {
                    if (hit.collider.gameObject.tag != StartOfRound.Instance.footstepSurfaces[i].surfaceTag) continue;
                    surfaceSound = i;
                    Plugin.ExtendedLogging($"Hit surface: {hit.collider.name} at position: {hit.point}");
                    hitSomething = true;
                    break;
                }
            }
            else
            {
                if (!hit.collider.TryGetComponent(out IHittable hittable) || hit.collider.gameObject == previousPlayerHeldBy.gameObject) continue;
                hitSomething = true;
                Plugin.ExtendedLogging($"Hit ihittable: {hit.collider.name}");
                iHittableList.Add(hittable);
            }
            if (hitSomething) break;
        }

        if (!hitSomething) return false;
        foreach (IHittable hittable in iHittableList)
        {
            OnWeaponHit(hittable, this.transform.position);
        }
        timeAtLastDamageDealt = Time.realtimeSinceStartup;

        RoundManager.Instance.PlayAudibleNoise(transform.position, 17f, 0.8f);
        if (surfaceSound != -1)
        {
            weaponAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[surfaceSound].hitSurfaceSFX);
            WalkieTalkie.TransmitOneShotAudio(weaponAudio, StartOfRound.Instance.footstepSurfaces[surfaceSound].hitSurfaceSFX);
            PlayRandomSFX(hitSFX); // hit wall etc sound from weapon
            HitWeaponServerRpc(surfaceSound); // hit wall etc sound from wall
        }
        else
        {
            PlayRandomSFX(hitEnemySFX);
            bloodParticle?.Play(true);
        }

        if (isHeavyWeapon)
        {
            playerHeldBy.playerBodyAnimator.SetTrigger("shovelHit");
        }

        return hitSomething;
    }

    public virtual bool OnWeaponHit(IHittable target, Vector3 hitDir)
    {
        return target.Hit(HitForce, hitDir, previousPlayerHeldBy, true, HitId);
    }

    [ServerRpc]
    public void HitWeaponServerRpc(int hitSurfaceID)
    {
        HitWeaponClientRpc(hitSurfaceID);
    }

    [ClientRpc]
    public void HitWeaponClientRpc(int hitSurfaceID)
    {
        PlayRandomSFX(hitSFX);
        OnHitSuccess.Invoke(previousPlayerHeldBy);

        if (hitSurfaceID != -1)
        {
            HitSurface(hitSurfaceID);
        }
    }

    private void PlayRandomSFX(AudioClip[] clips)
    {
        RoundManager.PlayRandomClip(weaponAudio, clips);
    }

    public virtual void HitSurface(int hitSurfaceID)
    {
        weaponAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(weaponAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
    }
}