using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Misc;

public abstract class CRHittable : NetworkBehaviour {
	public abstract bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null!,
							 bool playHitSFX = false,
							 int hitID = -1);
}