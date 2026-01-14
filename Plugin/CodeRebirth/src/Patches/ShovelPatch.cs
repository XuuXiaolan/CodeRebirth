using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util;
using UnityEngine;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
public static class ShovelPatch
{
    public static void Init()
    {
        On.Shovel.HitShovel += Shovel_HitShovel;
    }

    private static void Shovel_HitShovel(On.Shovel.orig_HitShovel orig, Shovel self, bool cancel)
    {
        orig(self, cancel);

        PostHitShovel(ref self);
    }

    private static void PostHitShovel(ref Shovel self)
    {
        if (self is ScaryShrimp scaryShrimp)
        {
            scaryShrimp.playerHeldBy.twoHanded = true;
            bool hitSomething = DetermineIfShovelHitSomething(self);

            if (hitSomething)
            {
                scaryShrimp.playerHeldBy.externalForceAutoFade = -scaryShrimp.playerHeldBy.gameplayCamera.transform.forward * 10;
            }
            return;
        }
        TrySpawnCRHazard(ref self);
    }

    private static bool DetermineIfShovelHitSomething(Shovel self)
    {
        for (int i = 0; i < self.objectsHitByShovelList.Count; i++)
        {
            if (self.objectsHitByShovelList[i].transform.gameObject.layer == 8 || self.objectsHitByShovelList[i].transform.gameObject.layer == 11)
            {
                if (!self.objectsHitByShovelList[i].collider.isTrigger)
                {
                    return true;
                }
            }
            else if (self.objectsHitByShovelList[i].transform.TryGetComponent(out IHittable _) && !(self.objectsHitByShovelList[i].transform == self.previousPlayerHeldBy.transform) && (self.objectsHitByShovelList[i].point == Vector3.zero || !Physics.Linecast(self.previousPlayerHeldBy.gameplayCamera.transform.position, self.objectsHitByShovelList[i].point, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
            {
                return true;
            }
        }
        return false;
    }

    private static void TrySpawnCRHazard(ref Shovel shovel)
    {
        if (!Plugin.ModConfig.ConfigDebugMode.Value)
            return;

        CodeRebirthUtils.Instance.SpawnRandomCRHazardServerRpc(RoundManager.Instance.GetRandomNavMeshPositionInRadius(shovel.previousPlayerHeldBy.gameplayCamera.transform.position + shovel.previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f, 1f, default));
    }
}