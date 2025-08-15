using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirthLib.Utils;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class GodRaySpawner : MonoBehaviour
{
    // these four should be set in the inspector
    public GodRayManager godRayManager = null!;
    public float timeBetweenGodRaySpawns;
    public float minX, maxX, minZ, maxZ;
    public List<Color> rayColours = new();

    private System.Random godRayRandom = null!;
    private int numberOfGodrays = 30;

    // Layer mask for "Room" and "Terrain"
    private LayerMask raycastLayerMask;

    private void Start()
    {
        godRayRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        raycastLayerMask = LayerMask.GetMask("Room", "Terrain");
        StartCoroutine(SpawnGodRays());
    }

    private IEnumerator SpawnGodRays()
    {
        while (GodRayManager.Active && godRayManager.GodRays.Count() < numberOfGodrays)
        {
            yield return new WaitForSeconds(10f);
            Color colour = rayColours[godRayRandom.Next(rayColours.Count)];

            Vector3 topPosition = godRayRandom.NextItem(RoundManager.Instance.outsideAINodes).transform.position;
            Vector3 bottomPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(godRayRandom.NextItem(RoundManager.Instance.outsideAINodes).transform.position, 100, default, godRayRandom);

            // Convert top and bottom positions to 3D vectors
            Vector3 raycastStart = bottomPosition;
            Vector3 raycastEnd = new Vector3(topPosition.x, 30f, topPosition.z); // End raycast just below the map
            // Calculate the direction from top to bottom position
            Vector3 rayDirection = (raycastEnd - raycastStart).normalized;

            // Perform the raycast along the calculated direction
            float raycastDistance = Vector3.Distance(raycastStart, raycastEnd);
            float topRandomFloat = godRayRandom.NextFloat(2f, 8f);
            if (!Physics.Raycast(raycastStart, rayDirection, out _, raycastDistance, raycastLayerMask))
            {
                godRayManager.AddGodRay(new GodRay(
                    colour,
                    new Vector2(topPosition.x, topPosition.z),
                    topRandomFloat,
                    topRandomFloat,
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