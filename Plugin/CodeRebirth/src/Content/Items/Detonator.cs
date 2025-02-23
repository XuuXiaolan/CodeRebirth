using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Detonator : GrabbableObject
{
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        StartCoroutine(DoBlowAnimation());
    }

    private IEnumerator DoBlowAnimation()
    {
        float currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(0);
        while (currentWeight <= 100)
        {
            currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(0);
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(currentWeight + Time.deltaTime * 100f, 0, 100f));
            yield return null;
        }

        foreach (var crate in NitroCrate.nitroCrates)
        {
            if (IsServer) crate.RequestServerToDespawnServerRpc();
        }

        yield return new WaitForSeconds(4f);

        while (currentWeight >= 0)
        {
            currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(0);
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(currentWeight - Time.deltaTime * 100f, 0, 100f));
            yield return null;
        }
    }
}