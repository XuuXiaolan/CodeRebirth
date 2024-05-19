using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;

public class Apocalypse : MonoBehaviour {
	Coroutine spawnHandler;
	List<GameObject> nodes;
	public List<Meteors> meteors = new List<Meteors>(); // Proper initialization
	Random random;
	public static Apocalypse Instance { get; private set; }
	public static bool Active => Instance != null;
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Apocalypse Weather.");
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		if(!IsAuthority()) return; // Only run on the host.
        
		random = new Random();
		spawnHandler = StartCoroutine(MeteorSpawnerHandler());
	}

	private void OnDisable() { // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			foreach (Meteors meteor in meteors) {
				if(!meteor.NetworkObject.IsSpawned || IsAuthority())
				Destroy(meteor.gameObject);
			}

			meteors = [];
			Instance = null;

			if(!IsAuthority()) return; // Only run on the host.
			StopCoroutine(spawnHandler);
		} catch {
			Plugin.Logger.LogFatal("Dont mind me~ - Xu");
		}
	}
    private Vector3 CalculateAverageLandNodePosition()
    {
        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (GameObject node in nodes)
        {
            sumPosition += node.transform.position;
            count++;
        }

        if (count > 0)
            return sumPosition / count;
        else
            return Vector3.zero; // Return a default position if no nodes are found
    }

	private IEnumerator MeteorSpawnerHandler() {
		yield return new WaitForSeconds(5f); // inital delay so clients don't get meteors before theyve inited everything.
		Plugin.Logger.LogInfo("Began spawning meteors.");
	    SpawnMeteor(CalculateAverageLandNodePosition());
	}

	private void SpawnMeteor(Vector3 target) {
		Vector3 origin = target + new Vector3(0, random.NextFloat(500, 800), 0);
            
		Meteors meteor = Instantiate(Plugin.Meteor, origin, Quaternion.identity).GetComponent<Meteors>();
        meteor.transform.localScale *= random.Next(40,60);
		meteor.NetworkObject.OnSpawn(() => {
			meteor.SetupMeteorClientRpc(origin, target, true);
		});
		meteor.NetworkObject.Spawn();
	}

	private bool IsAuthority() {
		return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
	}
}