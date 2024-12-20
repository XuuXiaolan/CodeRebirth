﻿using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Items;

[RequireComponent(typeof(InteractTrigger))]
public class Pickable : MonoBehaviour
{
	[SerializeField]
	private AudioSource? unlockSFX = null;
	
	[SerializeField]
	private UnityEvent onUnlock = null!;

	public bool IsLocked { get; set; } = true;

	public InteractTrigger trigger = null!;

	public void Unlock()
	{
		if (!IsLocked) return;

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
		if (!IsLocked) return;

		unlockSFX?.Play();
        
		onUnlock.Invoke();
		IsLocked = false;
	}
}