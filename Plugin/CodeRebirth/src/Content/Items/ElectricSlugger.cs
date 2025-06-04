using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Items;
public class ElectricSlugger : GrabbableObject
{
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public Transform weaponTip = null!;
    public VisualEffect shootVFX = null!;
    [Header("Audio")]
    public AudioSource idleSource = null!;
    public AudioSource firingSource = null!;
    public AudioClip fireSound = null!;
    public AudioClip chargeSound = null!;

    private RaycastHit[] cachedRaycastHits = new RaycastHit[100];
    private float pumpTimer = 0f;
    private int pumpCount = 0;
    private bool canFire = true;

    public override void Start()
    {
        base.Start();
    }

    public override void GrabItem()
    {
        base.GrabItem();
        Plugin.InputActionsInstance.PumpSlugger.performed += OnPumpDone;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        Plugin.InputActionsInstance.PumpSlugger.performed -= OnPumpDone;
    }

    public void OnPumpDone(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (pumpTimer > 0f || insertedBattery.empty || insertedBattery.charge <= 0 || isBeingUsed || isPocketed) return;
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;

        if (btn.wasPressedThisFrame)
        {
            DoPumpActionServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoPumpActionServerRpc()
    {
        DoPumpActionClientRpc();
    }

    [ClientRpc]
    private void DoPumpActionClientRpc()
    {
        StartCoroutine(DoPumpAction());
    }

    private IEnumerator DoPumpAction()
    {
        pumpTimer = 1.5f;
        canFire = false;
        float elapsed = 0f;
        firingSource.PlayOneShot(chargeSound);
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(elapsed * 200, 0, 100));
            yield return null;
        }

        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(elapsed * 200, 0, 100));
            yield return null;
        }
        pumpCount++;
        canFire = true;
    }

    public override void Update()
    {
        base.Update();
        pumpTimer -= Time.deltaTime;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        ShakeTransform(this.transform, pumpCount);
    }

    public void ShakeTransform(Transform _transform, int intensity)
    {
        idleSource.volume = 0f;
        if (intensity > 0)
        {
            idleSource.volume = 1f;
            float offset = Mathf.Clamp(intensity * 0.00025f * UnityEngine.Random.Range(-1, 2), -0.002f, 0.002f);
            _transform.localPosition = new Vector3(_transform.localPosition.x + offset, _transform.localPosition.y + offset, _transform.localPosition.z + offset);
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!canFire || insertedBattery.empty || insertedBattery.charge <= 0) return;
        int numHits = Physics.SphereCastNonAlloc(playerHeldBy.gameplayCamera.transform.position, 1, playerHeldBy.gameObject.transform.forward, cachedRaycastHits, 999, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            CRUtilities.CreateExplosion(cachedRaycastHits[i].transform.position, true, 0, 0, 0, 0, playerHeldBy, null, 0);
            if (!cachedRaycastHits[i].collider.gameObject.TryGetComponent(out IHittable iHittable))
                continue;

            if (iHittable is PlayerControllerB playerController)
            {
                if (playerController == playerHeldBy)
                    continue;

                if (playerController.isPlayerDead)
                    continue;
            }

            if (GameNetworkManager.Instance.localPlayerController == playerHeldBy)
                iHittable.Hit(2 * (pumpCount + 1), playerHeldBy.gameplayCamera.transform.position, playerHeldBy, true, -1);
            // play sound and stuff prob
        }
        firingSource.PlayOneShot(fireSound);
        shootVFX.Play();
        insertedBattery.charge -= 0.1f * (pumpCount + 1);
        if (playerHeldBy == GameNetworkManager.Instance.localPlayerController) HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        playerHeldBy.externalForceAutoFade += (-playerHeldBy.gameplayCamera.transform.forward) * (pumpCount + 1) * 5f * (playerHeldBy.isCrouching ? 0.25f : 1f);
        int intensity = (pumpCount + 1) * 2;
        StartCoroutine(CRUtilities.ForcePlayerLookup(playerHeldBy, intensity));
        pumpCount = 0;
    }
}