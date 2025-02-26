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
        // On.KnifeItem.HitKnife += KnifeItem_HitKnife;
    }

    private static void KnifeItem_HitKnife(On.KnifeItem.orig_HitKnife orig, KnifeItem self, bool cancel)
    {
        orig(self, cancel);
    }
}