using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class TerrainScanner : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public float duration = 10;
    public float size = 500;

    public ParticleSystem SpawnTerrainScanner(Vector3 spawnPosition)
    {
        GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);

        Destroy(terrainScanner, 20f);
        return terrainScanner.transform.GetChild(0).GetComponent<ParticleSystem>();
    }
}
