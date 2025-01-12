using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Weapons;
public class NaturesMace : CodeRebirthWeapons
{ // Added for potential future implementations
	private readonly int staffMask = 11012424;

    public List<PlayerControllerB> HitNaturesMace()
    {
        List<PlayerControllerB> playersHit = new();
        if (this.isHeld)
        {
            var objectsHitByStaff = Physics.SphereCastAll(
                previousPlayerHeldBy.gameplayCamera.transform.position + previousPlayerHeldBy.gameplayCamera.transform.right * -0.35f,
                0.8f,
                previousPlayerHeldBy.gameplayCamera.transform.forward,
                1.5f,
                staffMask,
                QueryTriggerInteraction.Collide
            );

            var objectsHitByStaffList = objectsHitByStaff.OrderBy(hit => hit.distance).ToList();

            foreach (var hit in objectsHitByStaffList)
            {
                if (hit.transform.gameObject.layer != 8 && hit.transform.gameObject.layer != 11)
                {
                    if (hit.transform.TryGetComponent(out IHittable hittable) && hit.transform != previousPlayerHeldBy.transform)
                    {
                        if (hit.transform.gameObject.layer == 3)
                        {
                            var player = hit.transform.GetComponent<PlayerControllerB>();
                            if (player != null)
                            {
                                playersHit.Add(player);
                            }
                        }
                    }
                }
            }
        }
        playersHit = playersHit.Distinct().ToList();
        return playersHit;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int playerIndex)
    {
        HealClientRpc(playerIndex);
    }

    [ClientRpc]
	public void HealClientRpc(int playerToHealIndex)
	{
        PlayerControllerB playerToHeal = StartOfRound.Instance.allPlayerScripts[playerToHealIndex];
		playerToHeal.DamagePlayer(-50, false, false, 0, 0, false, default);
		Plugin.ExtendedLogging("playerToHeal: " + playerToHeal.playerUsername + "| HealthAfterRpc: " + playerToHeal.health, (int)Logging_Level.Medium);
		if (playerToHeal.health >= 20)
		{
			playerToHeal.criticallyInjured = false;
		}
	}
}