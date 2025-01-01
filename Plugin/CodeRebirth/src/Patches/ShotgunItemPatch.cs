using System.Linq;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class ShotgunItemPatch
{
    public static void Init()
    {
        On.ShotgunItem.ShootGun += ShotgunItem_ShootGun;
    }

    private static void ShotgunItem_ShootGun(On.ShotgunItem.orig_ShootGun orig, ShotgunItem self, Vector3 shotgunPosition, Vector3 shotgunForward)
    {
        orig(self, shotgunPosition, shotgunForward);
        foreach (RaycastHit raycastHit in self.enemyColliders.ToArray())
        {
            if (raycastHit.transform == null) continue;
            if (raycastHit.transform.TryGetComponent(out PuppeteersVoodoo voodooDoll) && !Physics.Linecast(shotgunPosition, raycastHit.point, out _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                float distanceFromShotgun = Vector3.Distance(shotgunPosition, raycastHit.point);
                int damageAmount = 2;
                if (distanceFromShotgun < 3.7f)
                {
                    damageAmount = 5;
                }
                else if (distanceFromShotgun < 6f)
                {
                    damageAmount = 3;
                }
                voodooDoll.Hit(damageAmount, shotgunForward, self.playerHeldBy, true, -1);
            }

        }
    }
}