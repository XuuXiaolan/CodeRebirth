using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class Oxidizer : GrabbableObject
{
    public ParticleSystem[] flameStreamParticleSystems = [];
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public Transform capsuleTransform = null!;
    public AudioSource flameSource = null!;
    public AudioSource oxidizerSource = null!;
    public AudioClip outOfOxygenClip = null!;
    public AudioClip releaseHoldClip = null!;
    public AudioClip holdStartClip = null!;
    public AudioClip bigBlastClip = null!;
    public float depleteMultiplier = 10f;
    public float rechargeMultiplier = 1f;

    private RaycastHit[] cachedRaycastHits = new RaycastHit[24];
    private List<IHittable> iHittableList = new();
    private bool charged = true;
    private bool superCharged = false;
    private bool nerfed = false;
    private float updateHitInterval = 0.2f;
    public override void Update()
    {
        base.Update();
        updateHitInterval -= Time.deltaTime;

        if (superCharged) return;
        if (!charged && skinnedMeshRenderer.GetBlendShapeWeight(0) < 80)
        {
            charged = true;
            oxidizerSource.PlayOneShot(holdStartClip);
        }
        else if (charged && !isBeingUsed && skinnedMeshRenderer.GetBlendShapeWeight(0) >= 80)
        {
            charged = false;
        }

        if (!isBeingUsed)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) - Time.deltaTime * rechargeMultiplier * (nerfed ? 0.5f : 1f), 0, 100));
            return;
        }
        else
        {
            if (updateHitInterval <= 0)
            {
                updateHitInterval = 0.2f;
                iHittableList.Clear();
                int numHits = Physics.SphereCastNonAlloc(capsuleTransform.position, 1.3f, flameStreamParticleSystems[0].transform.forward, cachedRaycastHits, 4, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < numHits; i++)
                {
                    if (cachedRaycastHits[i].collider.gameObject.TryGetComponent(out IHittable iHittable))
                    {
                        iHittableList.Add(iHittable);
                    }
                }
                foreach (var iHittable in iHittableList)
                {
                    iHittable.Hit(1, flameStreamParticleSystems[0].transform.position, playerHeldBy, true, -1);
                }
            }
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) + Time.deltaTime * depleteMultiplier, 0, 100));
        if (skinnedMeshRenderer.GetBlendShapeWeight(0) >= 100)
        {
            isBeingUsed = false;
            playerHeldBy.activatingItem = false;
            foreach (var particleSystem in flameStreamParticleSystems)
            {
                particleSystem.Stop();
            }
            oxidizerSource.PlayOneShot(outOfOxygenClip);
            flameSource.Stop();
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!charged) return;
        isBeingUsed = buttonDown;
        if (buttonDown)
        {
            oxidizerSource.PlayOneShot(holdStartClip);
            flameSource.Play();
        }
        else
        {
            oxidizerSource.PlayOneShot(releaseHoldClip);
            flameSource.Stop();
        }
        playerHeldBy.activatingItem = buttonDown;
        foreach (var particleSystem in flameStreamParticleSystems)
        {
            if (buttonDown)
            {
                particleSystem.Play();
            }
            else
            {
                particleSystem.Stop();
            }
        }
    }

    public override void GrabItem()
    {
        base.GrabItem();
        Plugin.InputActionsInstance.ExhaustFuel.performed += OnExhaustFuel;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        isBeingUsed = false;
        Plugin.InputActionsInstance.ExhaustFuel.performed -= OnExhaustFuel;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        isBeingUsed = false;
    }

    public void OnExhaustFuel(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!charged || isPocketed || superCharged) return;
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;

        if (btn.wasPressedThisFrame)
        {
            DoExhaustFuelServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoExhaustFuelServerRpc()
    {
        DoExhaustFuelClientRpc();
    }

    [ClientRpc]
    private void DoExhaustFuelClientRpc()
    {
        superCharged = true;
        oxidizerSource.PlayOneShot(bigBlastClip);
        float fuelLeft = 100 - skinnedMeshRenderer.GetBlendShapeWeight(0);
        int damageToDeal = Mathf.FloorToInt(fuelLeft / 5f);
        Plugin.ExtendedLogging($"Oxidizer: {damageToDeal} damage dealt.");
        int numHits = Physics.SphereCastNonAlloc(capsuleTransform.position, 2f, flameStreamParticleSystems[0].transform.forward, cachedRaycastHits, 6, CodeRebirthUtils.Instance.playersAndEnemiesMask, QueryTriggerInteraction.Collide);
        iHittableList.Clear();
        for (int i = 0; i < numHits; i++)
        {
            if (cachedRaycastHits[i].transform == playerHeldBy.transform) continue;
            if (cachedRaycastHits[i].collider.gameObject.TryGetComponent(out IHittable iHittable))
            {
                iHittableList.Add(iHittable);
            }
        }
        foreach (var iHittable in iHittableList)
        {
            if (IsOwner) iHittable.Hit(damageToDeal, flameStreamParticleSystems[0].transform.position, playerHeldBy, true, -1);
        }
        foreach (var ps in flameStreamParticleSystems)
        {
            var mainModule = ps.main;
            mainModule.startSize = new ParticleSystem.MinMaxCurve(mainModule.startSize.constant * 3f);

            ps.Play();
        }
        playerHeldBy.externalForceAutoFade += (-playerHeldBy.gameplayCamera.transform.forward) * 25f * (playerHeldBy.isCrouching ? 0.25f : 1f);
        StartCoroutine(CRUtilities.ForcePlayerLookup(playerHeldBy, 5));
        StartCoroutine(SetWeightTo100());
    }

    private IEnumerator SetWeightTo100()
    {
        while (skinnedMeshRenderer.GetBlendShapeWeight(0) < 100)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(0, skinnedMeshRenderer.GetBlendShapeWeight(0) + 100 * Time.deltaTime);
            yield return null;
        }
        foreach (var ps in flameStreamParticleSystems)
        {
            var mainModule = ps.main;
            mainModule.startSize = new ParticleSystem.MinMaxCurve(mainModule.startSize.constant / 3f);

            ps.Stop();
        }
        charged = false;
        superCharged = false;
        StartCoroutine(SuperChargeNerf());
    }

    private IEnumerator SuperChargeNerf()
    {
        nerfed = true;
        yield return new WaitForSeconds(15f);
        nerfed = false;
    }
}