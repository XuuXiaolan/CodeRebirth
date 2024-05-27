using System;
using CodeRebirth.ItemStuff;
using HarmonyLib;
using Unity.Netcode;

namespace CodeRebirth.MapStuff;

public class ItemCrate : NetworkBehaviour {

	InteractTrigger trigger;
	Pickable pickable;
	
	void Awake() {
		trigger = GetComponent<InteractTrigger>();
		pickable = GetComponent <Pickable>();
	}

	public void Open() {
		pickable.IsLocked = false;
		Plugin.Logger.LogInfo("Opening Item Crate..");
	}
}