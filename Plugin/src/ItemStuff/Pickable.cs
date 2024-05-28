using System;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.ItemStuff;

[RequireComponent(typeof(InteractTrigger))]
public class Pickable : MonoBehaviour {
	[SerializeField]
	AudioSource unlockSFX;
	
	[SerializeField]
	UnityEvent onUnlock;

	public bool IsLocked { get; set; } = true;

	InteractTrigger trigger;

	void Awake() {
		trigger = GetComponent<InteractTrigger>();
	}

	public void Unlock() {
		if(!IsLocked) return;
        
		if(unlockSFX != null) unlockSFX.Play();
        
		onUnlock.Invoke();
		IsLocked = false;
	}
}