using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class TerrainScanner : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public float duration = 10;
    public float size = 500;

    private GameObject? objectToFollow = null;
    public void SpawnTerrainScanner(Vector3 spawnPosition, GameObject objectToFollow)
    {
        GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);
        /*ParticleSystem[] terrainScannerPSs = terrainScanner.transform.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem terrainScannerPS in terrainScannerPSs)
        {
            if (terrainScannerPS != null)
            {
                var main = terrainScannerPS.main;
                main.startLifetime = duration;
                // main.startSize = size;
            }

            else
            {
                Plugin.ExtendedLogging("The first child doesn't have a particle system.");
            }
        }*/

        Destroy(terrainScanner, 20f);
    }

    public void Update()
    {
        if (objectToFollow != null)
        {
            transform.position = objectToFollow.transform.position;
        }
    }
}
