using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class GodRaySpawner : MonoBehaviour
{
    // these two should be set in the inspector
    public GodRayManager godRayManager = null!;
    public float timeBetweenGodRaySpawns;
    public float minX, maxX, minZ, maxZ;
    public List<Color> rayColours = new();
    private System.Random godRayRandom = null!;
    private int numberOfGodrays = 30;
    private Vector3 centerOfWorld;

    // Layer mask for "Room" and "Terrain"
    private LayerMask raycastLayerMask;

    private void Start()
    {
        centerOfWorld = CalculateCenterOfPoints(RoundManager.Instance.outsideAINodes.Select(x => x.transform.position).ToList());
        godRayRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        raycastLayerMask = LayerMask.GetMask("Room", "Terrain");
        StartCoroutine(SpawnGodRays());
    }

    private IEnumerator SpawnGodRays()
    {
        while (GodRayManager.Active && godRayManager.GodRays.Count() < numberOfGodrays)
        {
            yield return new WaitForSeconds(1f);
            Color colour = rayColours[godRayRandom.NextInt(0, rayColours.Count - 1)];

            Vector2 topPosition = new Vector3(godRayRandom.NextFloat(minX, maxX), 0, godRayRandom.NextFloat(minZ, maxZ));
            Vector3 bottomPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(godRayRandom.NextItem(RoundManager.Instance.outsideAINodes).transform.position, 100, default, godRayRandom);

            // Convert top and bottom positions to 3D vectors
            Vector3 raycastStart = bottomPosition;
            Vector3 raycastEnd = new Vector3(topPosition.x, 30f, topPosition.y); // End raycast just below the map
            Plugin.Logger.LogInfo($"Raycast start: {raycastStart}, Raycast end: {raycastEnd}");
            // Calculate the direction from top to bottom position
            Vector3 rayDirection = (raycastEnd - raycastStart).normalized;

            // Perform the raycast along the calculated direction
            float raycastDistance = Vector3.Distance(raycastStart, raycastEnd);
            if (!Physics.Raycast(raycastStart, rayDirection, out RaycastHit hit, raycastDistance, raycastLayerMask))
            {
                godRayManager.AddGodRay(new GodRay(
                    colour,
                    topPosition,
                    godRayRandom.NextFloat(2f, 4f),
                    godRayRandom.NextFloat(2f, 5f),
                    new Vector3(bottomPosition.x, -1f, bottomPosition.z),
                    8f,
                    colour
                ));
            }
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