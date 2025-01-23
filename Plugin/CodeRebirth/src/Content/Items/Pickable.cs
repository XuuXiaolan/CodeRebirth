using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Items;

[RequireComponent(typeof(InteractTrigger))]
public class Pickable : NetworkBehaviour
{
	[SerializeField]
	private AudioSource? unlockSFX = null;
	
	[SerializeField]
	private UnityEvent? onUnlock = null;

	public bool IsLocked { get; set; } = true;

	public void Unlock()
	{
        UnlockStuffServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	public void UnlockStuffServerRpc()
	{
		UnlockStuffClientRpc();
	}

	[ClientRpc]
	public void UnlockStuffClientRpc()
	{
		UnlockStuffLocally();
	}

	public void UnlockStuffLocally()
	{
		if (!IsLocked) return;

		Plugin.ExtendedLogging($"Unlocking {this}");
		unlockSFX?.Play();
        
		onUnlock?.Invoke();
		IsLocked = false;
	}
}