using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Content.Weathers;
public class HurricaneWeather : CodeRebirthWeathers
{
	private IEnumerable<GameObject> nodes = [];
    private List<GameObject> alreadyUsedNodes = new();
	private void OnEnable()
	{
		Plugin.ExtendedLogging("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		ChangeCurrentLevelMaximumPower(outsidePower: -3, insidePower: 6, dayTimePower: -3);
        nodes = RoundManager.Instance.outsideAINodes;
		nodes = CullNodesByProximity(nodes, 5.0f, true, true, 50f);
		// WeatherRegistry.WeatherController.SetWeatherEffects(LevelWeatherType.Rainy);
		// WeatherRegistry.WeatherController.SetWeatherEffects(LevelWeatherType.Flooded);

		if(!IsAuthority()) return;
		StartCoroutine(TornadoSpawnerHandler());
	}

	private void OnDisable()
	{
		Plugin.ExtendedLogging("Cleaning up Weather.");
		ChangeCurrentLevelMaximumPower(outsidePower: 3, insidePower: -6, dayTimePower: 3);
	}

	private IEnumerator TornadoSpawnerHandler()
	{
		// Look into making the weather warning from vanilla into this.
		yield return new WaitForSeconds(20f);
		SpawnTornado(GetRandomTargetPosition(nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target)
	{
		RoundManager.Instance.SpawnEnemyGameObject(target, -1, -1, WeatherHandler.Instance.Tornado.HurricaneObj);
	}
}