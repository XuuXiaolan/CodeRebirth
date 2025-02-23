using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class TornadoWeather : CodeRebirthWeathers
{
	private IEnumerable<GameObject> nodes = [];
    private List<GameObject> alreadyUsedNodes = new();
	private List<Tornados> spawnedTornados = new();
	private void OnEnable()
	{
		Plugin.ExtendedLogging("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		ChangeCurrentLevelMaximumPower(outsidePower: -3, insidePower: 6, dayTimePower: -3);
        nodes = RoundManager.Instance.outsideAINodes;
		nodes = CullNodesByProximity(nodes, 5.0f, true, true);
		
		if(!IsAuthority()) return;
		StartCoroutine(TornadoSpawnerHandler());
	}

	private void OnDisable()
	{
		Plugin.ExtendedLogging("Cleaning up Weather.");
		foreach (Tornados tornado in spawnedTornados)
		{
			if (tornado == null) continue;
			if (tornado.IsOwner) tornado.KillEnemyOnOwnerClient(true);
		}
		spawnedTornados.Clear();
		ChangeCurrentLevelMaximumPower(outsidePower: 3, insidePower: -6, dayTimePower: 3);
	}

	private IEnumerator TornadoSpawnerHandler()
	{
		// Look into making the weather warning from vanilla into this.
		yield return new WaitForSeconds(20f);
		var spawnNodes = CullNodesByProximity(nodes, 5, true, true, 50f);
		SpawnTornado(GetRandomTargetPosition(spawnNodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target)
	{
		var tornado = RoundManager.Instance.SpawnEnemyGameObject(target, -1, -1, WeatherHandler.Instance.Tornado.TornadoObj);
		spawnedTornados.Add(((GameObject)tornado).GetComponent<Tornados>());
	}
}