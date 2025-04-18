using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using CodeRebirth.src.Util;
using System;

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

    [Header("Damage Targets")]
    public bool damagePlayers = true;
    [Tooltip("Passes a Player that has been damaged by the weapon")]
    public UnityEvent<PlayerControllerB> OnPlayerHit = new UnityEvent<PlayerControllerB>();

    public bool damageEnemies = true;
    [Tooltip("Passes an Enemy that has been damaged by the weapon")]
    public UnityEvent<EnemyAI> OnEnemyHit = new UnityEvent<EnemyAI>();

    public bool damageVehicles = false;
    [Tooltip("Passes a Vehicle that has been damaged by the weapon")]
    public UnityEvent<VehicleController> OnVehicleHit = new UnityEvent<VehicleController>();

    public bool damageObjects = true;
    [Tooltip("Passes an Object that has been damaged by the weapon")]
    public UnityEvent<IHittable> OnObjectHit = new UnityEvent<IHittable>();

    public bool damageSurfaces = true;
    [Tooltip("Passes a Surface that has been damaged by the weapon")]
    public UnityEvent<int> OnSurfaceHit = new UnityEvent<int>();

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
    public AudioClip[] hitSFX = [];
    [Tooltip("Only used for Heavy Weapons")]
    public AudioClip[] reelUpSFX = [];
    public AudioClip[] swingSFX = [];
    public AudioClip[] finishReelUpSFX = [];
    public AudioClip[] hitEnemySFX = [];
    public AudioSource weaponAudio;
    public bool tryHitAllTimes = false;

    [HideInInspector] public float heldOverHeadTimer = 0f;
    private Coroutine? reelingRoutine = null;
    [HideInInspector] public RaycastHit[] cachedRaycastHits = new RaycastHit[16];
    [HideInInspector] public PlayerControllerB? previousPlayerHeldBy = null;

    [HideInInspector] public UnityEvent OnHitSuccess = new UnityEvent();
    private List<IHittable> iHittableList = new();
    private List<VehicleController> hitVehicles = new();
    private List<PlayerControllerB> hitPlayers = new();
    private List<EnemyAICollisionDetect> hitEnemies = new();

    private static readonly int UseHeldItem1Animation = Animator.StringToHash("UseHeldItem1"); // Trigger
    private static readonly int ShovelHitAnimation = Animator.StringToHash("shovelHit"); // Trigger
    private static readonly int ReelingUpAnimation = Animator.StringToHash("reelingUp"); // Bool

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
                previousPlayerHeldBy.playerBodyAnimator.SetTrigger(UseHeldItem1Animation);
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
        playerHeldBy.playerBodyAnimator.ResetTrigger(ShovelHitAnimation);
        playerHeldBy.playerBodyAnimator.SetBool(ReelingUpAnimation, true);
        reelingAnimSpeed = 0.35f / reelingTime;
        playerHeldBy.playerBodyAnimator.speed = reelingAnimSpeed;
        PlayRandomSFX(reelUpSFX);
        ReelUpSFXServerRpc();
        yield return new WaitForSeconds(reelingTime);
        playerHeldBy.playerBodyAnimator.speed = 1f;
        PlayRandomSFX(finishReelUpSFX);
        while (isHoldingButton && isHeld)
        {
            heldOverHeadTimer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitUntil(() => !isHoldingButton || !isHeld);
        SwingHeavyWeapon(!isHeld);
        if (tryHitAllTimes)
        {
            float timeElapsed = 0f;
            bool success = false;
            while (timeElapsed <= swingTime && !success)
            {
                timeElapsed += Time.deltaTime;
                yield return null;
                yield return new WaitForEndOfFrame();
                success = HitWeapon(!isHeld);
            }
        }
        else
        {
            yield return new WaitForSeconds(swingTime);
            HitWeapon(!isHeld);
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
        if (IsOwner) return;
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
        previousPlayerHeldBy.playerBodyAnimator.SetBool(ReelingUpAnimation, false);
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

        if (cancel) return false;
        previousPlayerHeldBy.twoHanded = false;
        int numHits = Physics.SphereCastNonAlloc(weaponTip.position, weaponRange, weaponTip.forward, cachedRaycastHits, 1.5f, CodeRebirthUtils.Instance.collidersAndRoomAndRailingAndTerrainAndHazardAndVehicleAndDefaultMask, QueryTriggerInteraction.Ignore);
        var objectsHit = cachedRaycastHits.Take(numHits).OrderBy(hit => hit.distance);

        hitVehicles.Clear();
        foreach (RaycastHit hit in objectsHit)
        {
            if (hit.collider.gameObject.CompareTag("Player") || hit.collider.gameObject.CompareTag("Enemy")) continue;
            VehicleController? hitVehicle = GrabVehicleFromHit(hit);
            if (hitVehicle != null)
            {
                hitVehicles.Add(hitVehicle);
            }
            for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
            {
                if (!hit.collider.gameObject.CompareTag(StartOfRound.Instance.footstepSurfaces[i].surfaceTag)) continue;
                surfaceSound = i;
                Plugin.ExtendedLogging($"Hit surface: {hit.collider.name} at position: {hit.collider.gameObject.transform.position}");
                break;
            }
        }

        iHittableList.Clear();
        hitEnemies.Clear();
        hitPlayers.Clear();

        numHits = Physics.SphereCastNonAlloc(weaponTip.position, weaponRange, weaponTip.forward, cachedRaycastHits, 1.5f, CodeRebirthUtils.Instance.playersAndEnemiesAndHazardMask, QueryTriggerInteraction.Collide);
        objectsHit = cachedRaycastHits.Take(numHits).OrderBy(hit => hit.distance);
        foreach (RaycastHit hit in objectsHit)
        {
            if (!hit.collider.gameObject.TryGetComponent(out IHittable hittable) || hit.collider.gameObject == previousPlayerHeldBy.gameObject) continue;
            Plugin.ExtendedLogging($"Hit hittable: {hit.collider.name} at position: {hit.collider.gameObject.transform.position}");
            if (hittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (hitEnemies.Contains(enemyAICollisionDetect)) continue;
                Plugin.ExtendedLogging($"Hit enemy: {hit.collider.name} at position: {hit.collider.gameObject.transform.position}");
                hitEnemies.Add(enemyAICollisionDetect);
                continue;
            }
            else if (hittable is PlayerControllerB playerControllerB)
            {
                if (hitPlayers.Contains(playerControllerB)) continue;
                Plugin.ExtendedLogging($"Hit player: {hit.collider.name} at position: {hit.collider.gameObject.transform.position}");
                hitPlayers.Add(playerControllerB);
                continue;
            }
            iHittableList.Add(hittable);
        }

        if (surfaceSound == -1 && hitEnemies.Count <= 0 && hitPlayers.Count <= 0 && hitVehicles.Count <= 0) return false;
        foreach (var hittable in iHittableList)
        {
            OnWeaponHit(hittable, this.transform.position);
        }
        timeAtLastDamageDealt = Time.realtimeSinceStartup;

        RoundManager.Instance.PlayAudibleNoise(transform.position, 17f, 0.8f);
        HandleHittingPlayers(hitPlayers);
        HandleHittingEnemies(hitEnemies);
        HandleHittingVehicles(hitVehicles);
        HandleHittingSurface(surfaceSound);

        if (isHeavyWeapon)
        {
            playerHeldBy.playerBodyAnimator.SetTrigger(ShovelHitAnimation);
        }

        OnHitSuccess.Invoke();
        return true;
    }

    private void HandleHittingSurface(int surfaceID)
    {
        if (!damageSurfaces) return;
        if (surfaceID == -1) return;
        OnSurfaceHit.Invoke(surfaceID);
        weaponAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[surfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(weaponAudio, StartOfRound.Instance.footstepSurfaces[surfaceID].hitSurfaceSFX);
        PlayRandomSFX(hitSFX); // hit wall etc sound from weapon
        HitWeaponServerRpc(surfaceID); // hit wall etc sound from wall
    }

    private void HandleHittingPlayers(List<PlayerControllerB> _hitPlayers)
    {
        if (!damagePlayers) return;
        if (_hitPlayers.Count <= 0) return;
        List<CentipedeAI> _relevantCentipedeAI = new();
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy == null || enemy.isEnemyDead) continue;
            if (enemy is CentipedeAI centipedeAI && centipedeAI.clingingToPlayer != null)
            {
                _relevantCentipedeAI.Add(centipedeAI);
            }
        }
        foreach (var player in _hitPlayers)
        {
            Plugin.ExtendedLogging($"Hitting player: {player}");
            OnPlayerHit.Invoke(player);
            if (_relevantCentipedeAI.Any(x => x.clingingToPlayer == player))
                continue;
            player.DamagePlayerFromOtherClientServerRpc(HitForce * 10, weaponTip.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
    }

    private void HandleHittingEnemies(List<EnemyAICollisionDetect> hitEnemies)
    {
        if (!damageEnemies) return;
        if (hitEnemies.Count <= 0) return;
        foreach (var enemyAICollisionDetect in hitEnemies)
        {
            Plugin.ExtendedLogging($"Hitting enemy: {enemyAICollisionDetect.mainScript}");
            OnEnemyHit.Invoke(enemyAICollisionDetect.mainScript);
            if (!enemyAICollisionDetect.mainScript.isEnemyDead) enemyAICollisionDetect.mainScript.HitEnemyOnLocalClient(HitForce, weaponTip.transform.position, previousPlayerHeldBy, true, HitId);
        }
        PlayRandomSFX(hitEnemySFX);
        bloodParticle?.Play(true);
    }

    private void HandleHittingVehicles(List<VehicleController> hitVehicles)
    {
        if (!damageVehicles) return;
        if (hitVehicles.Count <= 0) return;

        foreach (var vehicle in hitVehicles)
        {
            vehicle.PushTruckServerRpc(previousPlayerHeldBy.transform.position, weaponTip.transform.position);
            vehicle.DealPermanentDamage(HitForce, previousPlayerHeldBy.transform.position);

            OnVehicleHit.Invoke(vehicle);
        }

    }

    public VehicleController? GrabVehicleFromHit(RaycastHit hit)
    {
        if (!damageVehicles) return null;
        if (hit.collider.gameObject.layer != 30) return null;
        if (!hit.collider.TryGetComponent(out VehicleController vehicle)) return null;
        if (hitVehicles.Contains(vehicle)) return null;
        return vehicle;
    }

    public bool OnWeaponHit(IHittable target, Vector3 hitDir)
    {
        if (!damageObjects) return false;
        OnObjectHit.Invoke(target);
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
        if (IsOwner) return;
        PlayRandomSFX(hitSFX); // hit wall etc sound from weapon
        HitSurface(hitSurfaceID);
    }

    private void PlayRandomSFX(AudioClip[] clips)
    {
        if (clips.Length == 0) return;
        RoundManager.PlayRandomClip(weaponAudio, clips);
    }

    private void HitSurface(int hitSurfaceID)
    {
        weaponAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
        WalkieTalkie.TransmitOneShotAudio(weaponAudio, StartOfRound.Instance.footstepSurfaces[hitSurfaceID].hitSurfaceSFX);
    }
}