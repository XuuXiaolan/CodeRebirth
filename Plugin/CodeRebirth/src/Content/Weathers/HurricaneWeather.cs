using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Content.Weathers;
public class HurricaneWeather : CodeRebirthWeathers
{
	private IEnumerable<GameObject> nodes = [];
    private List<GameObject> alreadyUsedNodes = new();
	private List<Tornados> spawnedTornados = new();

	private void OnEnable()
	{
		Plugin.ExtendedLogging("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		ChangeCurrentLevelMaximumPower(outsidePower: -3, insidePower: 6, dayTimePower: -3);
        nodes = RoundManager.Instance.outsideAINodes;
		nodes = CullNodesByProximity(nodes, 5.0f, true, true, 50f);

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
		// 20 second buffer from ship start before spawning tornado stuff
		yield return new WaitForSeconds(20f);
		WeatherController.AddWeatherEffect(LevelWeatherType.Flooded);
		WeatherController.AddWeatherEffect(LevelWeatherType.Rainy);
		SpawnTornado(GetRandomTargetPosition(nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target)
	{
		var tornado = RoundManager.Instance.SpawnEnemyGameObject(target, -1, -1, WeatherHandler.Instance.Tornado.HurricaneObj);
		spawnedTornados.Add(((GameObject)tornado).GetComponent<Tornados>());
	}
}