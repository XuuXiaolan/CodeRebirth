using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.Misc;

public class HittableCollisionDetect : MonoBehaviour, IHittable {
	[SerializeField]
	CRHittable _mainScript;
	
	public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false,
					int hitID = -1) {
		return _mainScript.Hit(force, hitDirection, playerWhoHit, playHitSFX, hitID);
	}
}