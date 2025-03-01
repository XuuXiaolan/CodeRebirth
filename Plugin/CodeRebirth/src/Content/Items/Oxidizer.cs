using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Oxidizer : GrabbableObject
{
    public ParticleSystem[] flameStreamParticleSystems = [];
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;

    private bool charged = true;
    public override void Update()
    {
        base.Update();
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
        skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) + Time.deltaTime*2.5f, 0, 100));
        if (skinnedMeshRenderer.GetBlendShapeWeight(0) >= 100)
        {
            charged = true;
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