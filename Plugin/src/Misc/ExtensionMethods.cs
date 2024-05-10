using System;
using System.Collections;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Misc;

public static class ExtensionMethods {
	public static IEnumerator WaitUntilSpawned(this NetworkObject networkObject) {
		yield return new WaitUntil(() => networkObject.IsSpawned);
	}

	static IEnumerator RunActionAfterSpawned(NetworkObject networkObject, Action action) {
		yield return networkObject.WaitUntilSpawned();
		action();
	}
	
	public static void OnSpawn(this NetworkObject networkObject, Action action) {
		networkObject.StartCoroutine(RunActionAfterSpawned(networkObject, action));
	}
}