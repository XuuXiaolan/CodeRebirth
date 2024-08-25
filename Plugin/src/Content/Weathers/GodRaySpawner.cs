using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Weathers;
public class GodRaySpawner : MonoBehaviour
{
    // these two should be set in the inspector
    public GodRayManager godRayManager = null!;
    public float timeBetweenGodRaySpawns;
    public float minX, maxX, minZ, maxZ;
    public List<Color> rayColours = new();
    private System.Random godRayRandom = null!;
    private int numberOfGodrays = 10;
    private Vector3 centerOfWorld;

    private void Start()
    {
        centerOfWorld = CalculateCenterOfPoints(RoundManager.Instance.outsideAINodes.Select(x => x.transform.position).ToList());
        godRayRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        StartCoroutine(SpawnGodRays());
    }

    private IEnumerator SpawnGodRays() {
        while (GodRayManager.Active && godRayManager.GodRays.Count() < numberOfGodrays) {
            yield return new WaitForSeconds(timeBetweenGodRaySpawns);
            Color colour = rayColours[godRayRandom.NextInt(0, rayColours.Count - 1)];

            Vector2 topPosition = new Vector2(godRayRandom.NextFloat(minX, maxX), godRayRandom.NextFloat(minZ, maxZ));
            Vector2 bottomPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(centerOfWorld, 200, default);
            godRayManager.AddGodRay(new GodRay(
                colour,
                topPosition,
                godRayRandom.NextFloat(2f, 4f),
                godRayRandom.NextFloat(2f, 5f),
                new Vector3(bottomPosition.x, -1f, bottomPosition.y),
                8f,
                colour
            ));
        }
    }

    private Vector3 CalculateCenterOfPoints(List<Vector3> points)
    {
        if (points == null || points.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }
}