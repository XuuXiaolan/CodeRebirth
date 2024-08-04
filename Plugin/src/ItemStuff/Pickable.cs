using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.ItemStuff;

[RequireComponent(typeof(InteractTrigger))]
public class Pickable : MonoBehaviour {
	[SerializeField]
	private AudioSource unlockSFX = null!;
	
	[SerializeField]
	private UnityEvent onUnlock = null!;

	public bool IsLocked { get; set; } = true;

	private InteractTrigger trigger = null!;

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