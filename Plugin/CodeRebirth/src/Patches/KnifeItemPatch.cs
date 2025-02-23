using System;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class KnifeItemPatch
{
    public static Collider[] cachedColliders = new Collider[10];

    public static void Init()
    {
        On.KnifeItem.HitKnife += KnifeItem_HitKnife;
    }

    private static void KnifeItem_HitKnife(On.KnifeItem.orig_HitKnife orig, KnifeItem self, bool cancel)
    {
        orig(self, cancel);
        if (cancel || self is not InfiniKey infiniKey) return;
        int numHits = Physics.OverlapSphereNonAlloc(self.transform.position, 2f, cachedColliders, CodeRebirthUtils.Instance.interactableMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            infiniKey.OnHit(cachedColliders[i]);
        }
    }
}