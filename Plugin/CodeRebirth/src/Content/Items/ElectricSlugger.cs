using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class ElectricSlugger : GrabbableObject
{
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public LineRenderer[] lineRenderers = null!;
    public Transform weaponTip = null!;

    private RaycastHit[] cachedRaycastHits = new RaycastHit[20];
    private float pumpTimer = 0f;
    private int pumpCount = 0;
    private bool canFire = true;

    public override void Start()
    {
        base.Start();
        foreach (var renderer in lineRenderers)
        {
            renderer.enabled = false;
        }
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
        if (pumpTimer > 0f || insertedBattery.empty || isBeingUsed) return;
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
        pumpTimer = 2.5f;
        canFire = false;
        float elapsed = 0f;
        while (elapsed < 0.5f)
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

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!canFire) return;
        float finalDestinationDistance = 100;
        bool hitSomething = Physics.Raycast(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameObject.transform.forward, out RaycastHit raycastHit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
        if (hitSomething)
        {
            finalDestinationDistance = Vector3.Distance(playerHeldBy.transform.position, raycastHit.point);
        }
        int numHits = Physics.RaycastNonAlloc(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameObject.transform.forward, cachedRaycastHits, finalDestinationDistance, CodeRebirthUtils.Instance.enemiesMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedRaycastHits[i].collider.gameObject.TryGetComponent(out IHittable iHittable)) continue;
            if (GameNetworkManager.Instance.localPlayerController == playerHeldBy) iHittable.Hit(2 * (pumpCount + 1), playerHeldBy.gameplayCamera.transform.position, playerHeldBy, true, -1);
            // play sound and stuff prob
        }
        insertedBattery.charge -= 0.1f * (pumpCount + 1);
        pumpCount = 0;
        CRUtilities.CreateExplosion(playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward * finalDestinationDistance, true, 0, 0, 0, 0, playerHeldBy, null, 0);
        foreach (var renderer in lineRenderers)
        {
            renderer.enabled = true;
            renderer.SetPosition(0, weaponTip.transform.position);
            renderer.SetPosition(1, playerHeldBy.gameplayCamera.transform.position + playerHeldBy.gameplayCamera.transform.forward * finalDestinationDistance);
        }
        StartCoroutine(DisableRenderersRoutine());
        // Create a LineRenderer2D that goes from the player to the direction vector * finalDestinationDistance
    }

    private IEnumerator DisableRenderersRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (var renderer in lineRenderers)
        {
            renderer.enabled = false;
        }
    }
}