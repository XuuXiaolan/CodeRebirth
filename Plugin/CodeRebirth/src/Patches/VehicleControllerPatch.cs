
using System;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class VehicleControllerPatch
{
    public static void Init()
    {
        On.VehicleController.Awake += VehicleController_Awake;
    }

    private static void VehicleController_Awake(On.VehicleController.orig_Awake orig, VehicleController self)
    {
        orig(self);
        RabbitMagician.vehicleController = self;
        foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is not RabbitMagician)
                continue;

            foreach (var enemyCollider in enemy.GetComponentsInChildren<Collider>())
            {
                foreach (Collider collider in self.GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(enemyCollider, collider, true);
                }
            }
        }
    }
}