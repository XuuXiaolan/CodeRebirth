using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.MiscScripts;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class LandminePatch
{
    public static void Init()
    {
        On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
        On.Landmine.OnTriggerExit += Landmine_OnTriggerExit;
        IL.Landmine.SpawnExplosion += AffectIExplodeable;
    }

    private static void AffectIExplodeable(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdloc(3),
                i => i.MatchLdloc(10),
                i => i.MatchLdelemRef(),
                i => i.MatchCallvirt<Component>("get_gameObject"),
                i => i.MatchCallvirt<GameObject>("GetComponentInChildren"),
                i => i.MatchStloc(13),
                i => i.MatchLdloc(13),
                i => i.MatchLdnull()))
        {
            Plugin.Logger.LogError($"Failed to find GetComponentsInChildren etc in {nameof(Landmine)}.{nameof(Landmine.SpawnExplosion)} 1");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_3);
        cursor.Emit(OpCodes.Ldloc_S, (byte)10);
        cursor.Emit(OpCodes.Ldelem_Ref);
        cursor.Emit(OpCodes.Castclass, typeof(Collider));

        cursor.Emit(OpCodes.Ldarg_0);

        cursor.Emit(OpCodes.Ldloc_S, (byte)11);

        cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(LandminePatch), nameof(AffectIExplodeableFromLandmine)));

        if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdloc(3),
                i => i.MatchLdloc(10),
                i => i.MatchLdelemRef(),
                i => i.MatchCallvirt<Component>("get_gameObject"),
                i => i.MatchCallvirt<GameObject>("GetComponentInChildren"),
                i => i.MatchStloc(14),
                i => i.MatchLdloc(14),
                i => i.MatchLdnull()))
        {
            Plugin.Logger.LogError($"Failed to find GetComponentsInChildren etc in {nameof(Landmine)}.{nameof(Landmine.SpawnExplosion)} 2");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_3);
        cursor.Emit(OpCodes.Ldloc_S, (byte)10);
        cursor.Emit(OpCodes.Ldelem_Ref);
        cursor.Emit(OpCodes.Castclass, typeof(Collider));

        cursor.Emit(OpCodes.Ldarg_0);

        cursor.Emit(OpCodes.Ldloc_S, (byte)11);

        cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(LandminePatch), nameof(AffectIExplodeableFromLandmine)));
    }

    private static void AffectIExplodeableFromLandmine(Collider collider, Vector3 explosionPosition, float distanceToExplosion)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (collider.TryGetComponent(out IExplodeable explodeable))
        {
            explodeable.OnExplosion(6, explosionPosition, distanceToExplosion);
        }
    }

    private static void Landmine_OnTriggerExit(On.Landmine.orig_OnTriggerExit orig, Landmine self, Collider other)
    {
        orig(self, other);
        if (self.IsServer && PuppeteersVoodoo.puppeteerList.Count > 0 && other.gameObject.layer == 19 && other.gameObject.name.Contains("PuppeteerPuppet"))
        {
            self.TriggerMineOnLocalClientByExiting();
        }
    }

    private static void Landmine_SpawnExplosion(On.Landmine.orig_SpawnExplosion orig, Vector3 explosionPosition, bool spawnExplosionEffect, float killRange, float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab, bool goThroughCar)
    {
        orig(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab, goThroughCar);
        for (int i = PuppeteersVoodoo.puppeteerList.Count - 1; i >= 0; i--)
        {
            PuppeteersVoodoo voodooDoll = PuppeteersVoodoo.puppeteerList[i];
            float distanceFromMine = Vector3.Distance(voodooDoll.transform.position, explosionPosition);
            if (distanceFromMine > 5f)
            {
                return;
            }

            Plugin.ExtendedLogging($"Hit voodoo doll with landmine: {voodooDoll}");
            if (!Physics.Linecast(explosionPosition, voodooDoll.transform.position + Vector3.up * 0.3f, out _, 1073742080, QueryTriggerInteraction.Ignore))
            {
                Vector3 vector = Vector3.Normalize(voodooDoll.transform.position - explosionPosition) * 80f / distanceFromMine;
                if (distanceFromMine < killRange)
                {
                    voodooDoll.Hit(10, vector, null, true, -1);
                }
                else if (distanceFromMine < damageRange)
                {
                    voodooDoll.Hit(2, vector, null, true, -1);
                }
            }            
        }
    }
}