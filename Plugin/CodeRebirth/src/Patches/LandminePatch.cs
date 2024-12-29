using CodeRebirth.src.Content.Items;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class LandminePatch
{
    public static void Init()
    {
        On.Landmine.SpawnExplosion += Landmine_SpawnExplosion;
    }

    private static void Landmine_SpawnExplosion(On.Landmine.orig_SpawnExplosion orig, Vector3 explosionPosition, bool spawnExplosionEffect, float killRange, float damageRange, int nonLethalDamage, float physicsForce, GameObject overridePrefab, bool goThroughCar)
    {
        orig(explosionPosition, spawnExplosionEffect, killRange, damageRange, nonLethalDamage, physicsForce, overridePrefab, goThroughCar);
        foreach (var voodooDoll in PuppeteersVoodoo.puppeteerList.ToArray())
        {
            float distanceFromMine = Vector3.Distance(voodooDoll.transform.position, explosionPosition);
            if (distanceFromMine > 4f) return;
            if (!Physics.Linecast(explosionPosition, voodooDoll.transform.position + Vector3.up * 0.3f, out RaycastHit raycastHit, 1073742080, QueryTriggerInteraction.Ignore))
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