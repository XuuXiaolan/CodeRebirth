using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AirUnitBulletTrail : NetworkBehaviour
{
    public AirUnitProjectile mainProjectile = null!;
    private void OnTriggerEnter(Collider other)
    {
        if (!mainProjectile.explodedOnTarget && other.TryGetComponent(out PlayerControllerB player))
        {
            Vector3 forceFlung = (mainProjectile.transform.position - this.transform.position).normalized * mainProjectile.bulletTrailForce;
            if (player.IsLocalPlayer())
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            }
            if (!player.isPlayerDead) StartCoroutine(PushPlayerFarAway(player, forceFlung));
        }
    }

    private IEnumerator PushPlayerFarAway(PlayerControllerB player, Vector3 force)
    {
        float duration = 1f;
        while (duration > 0)
        {
            duration -= Time.fixedDeltaTime;
            player.externalForces += force;
            yield return new WaitForFixedUpdate();
        }
        yield break;
    }
}