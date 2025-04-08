using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class TurretPatch
{
    public static void Init()
    {
        On.Turret.CheckForPlayersInLineOfSight += Turret_CheckForPlayersInLineOfSight;
    }

    private static PlayerControllerB Turret_CheckForPlayersInLineOfSight(On.Turret.orig_CheckForPlayersInLineOfSight orig, Turret self, float radius, bool angleRangeCheck)
    {
        if (self.turretMode == TurretMode.Firing || self.turretMode == TurretMode.Berserk)
        {
            var shootRay = new Ray(self.centerPoint.position - Vector3.up * 0.3f, self.aimPoint.forward - Vector3.up * 0.3f);
            Plugin.ExtendedLogging($"Raycast from {self.centerPoint.position} to {self.aimPoint.forward}");
            if (!Physics.Raycast(shootRay, out var hit, 30f, CodeRebirthUtils.Instance.enemiesMask, QueryTriggerInteraction.Collide))
            {
                goto ret;
            }
            Plugin.ExtendedLogging($"Raycast hit {hit.transform.name}");
            if (hit.transform.CompareTag("Player") && hit.transform.TryGetComponent(out PuppeteersVoodoo puppet) && puppet.playerControlled != null)
            {
                Plugin.ExtendedLogging($"Hit player {puppet.playerControlled.name}");
                if (angleRangeCheck && Vector3.Angle(puppet.transform.position + Vector3.up * 1.75f - self.centerPoint.position, self.forwardFacingPos.forward) > self.rotationRange)
                {
                    Plugin.ExtendedLogging($"Angle too far");
                    goto ret;
                }
                return puppet.playerControlled;
            }
        }
    ret:
        return orig(self, radius, angleRangeCheck);
    }
}