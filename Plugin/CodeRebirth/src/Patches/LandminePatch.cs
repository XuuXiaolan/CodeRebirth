using System;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class LandminePatch
{
    public static void Init()
    {
        On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
        On.Landmine.OnTriggerExit += Landmine_OnTriggerExit;
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
        foreach (var voodooDoll in PuppeteersVoodoo.puppeteerList.ToArray())
        {
            float distanceFromMine = Vector3.Distance(voodooDoll.transform.position, explosionPosition);
            if (distanceFromMine > 5f) return;
            Plugin.ExtendedLogging($"Hit voodoo doll with landmine: {voodooDoll}", (int)Logging_Level.High);
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