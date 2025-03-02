using System.Collections.Generic;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Oxidizer : GrabbableObject
{
    public ParticleSystem[] flameStreamParticleSystems = [];
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public Transform capsuleTransform = null!;

    private RaycastHit[] cachedRaycastHits = new RaycastHit[20];
    private bool charged = true;
    private float updateHitInterval = 0.2f;
    public override void Update()
    {
        base.Update();
        updateHitInterval -= Time.deltaTime;

        if (!charged && skinnedMeshRenderer.GetBlendShapeWeight(0) < 80)
        {
            charged = true;
        }
        else if (charged && !isBeingUsed && skinnedMeshRenderer.GetBlendShapeWeight(0) >= 80)
        {
            charged = false;
        }

        if (!isBeingUsed)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) - Time.deltaTime, 0, 100));
            return;
        }
        else
        {
            if (updateHitInterval <= 0)
            {
                updateHitInterval = 0.2f;
                int numHits = Physics.SphereCastNonAlloc(capsuleTransform.position, 1.3f, flameStreamParticleSystems[0].transform.forward, cachedRaycastHits, 4, CodeRebirthUtils.Instance.playersAndEnemiesMask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < numHits; i++)
                {
                    if (cachedRaycastHits[i].collider.gameObject.TryGetComponent(out IHittable iHittable))
                    {
                        iHittable.Hit(1, flameStreamParticleSystems[0].transform.position, playerHeldBy, true, -1);
                    }
                }
            }
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) + Time.deltaTime*2.5f, 0, 100));
        if (skinnedMeshRenderer.GetBlendShapeWeight(0) >= 100)
        {
            isBeingUsed = false;
            playerHeldBy.activatingItem = false;
            foreach (var particleSystem in flameStreamParticleSystems)
            {
                particleSystem.Stop();
            }
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (!charged) return;
        isBeingUsed = buttonDown;
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

    public override void DiscardItem()
    {
        base.DiscardItem();
        isBeingUsed = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        isBeingUsed = false;
    }
}