using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;

namespace CodeRebirth.src.Content.Maps;
public class BiomeManager : NetworkBehaviour
{
    public DecalProjector corruptionProjector = null!;
    public DecalProjector crimsonProjector = null!;
    public DecalProjector hallowProjector = null!;
    
    private ParticleSystem deathParticles = null!;
    private DecalProjector activeProjector = null!;
    private System.Random biomeRandom = new System.Random(69);
    private static int foliageLayer = 0;
    private static int terrainLayer = 0;
    private static int combinedLayerMask = 0;
    private List<Collider> foliageOrTreeColliderList = new();

	public static List<BiomeManager> Instances = new();
	public static bool Active => Instances.Count > 0;

    public void Start()
    {
        foliageLayer = LayerMask.NameToLayer("Foliage");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        combinedLayerMask = (1 << foliageLayer) | (1 << terrainLayer);
        if (StartOfRound.Instance != null)
        {
            biomeRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        switch (biomeRandom.NextInt(0, 2))
        {
            case 0:
                corruptionProjector.gameObject.SetActive(true);
                activeProjector = corruptionProjector;
                break;
            case 1:
                crimsonProjector.gameObject.SetActive(true);
                activeProjector = crimsonProjector;
                break;
            case 2:
                hallowProjector.gameObject.SetActive(true);
                activeProjector = hallowProjector;
                break;
        }

        deathParticles = activeProjector.gameObject.GetComponentInChildren<ParticleSystem>();
        StartCoroutine(CheckAndDestroyFoliage());
    }

    public void Update()
    {
        if (activeProjector == null) return;
        if (activeProjector.size.x >= 250 || activeProjector.size.y >= 250) return;
        activeProjector.size += new Vector3(Time.deltaTime * 0.34f, Time.deltaTime * 0.34f, 0f);
    }

    private IEnumerator CheckAndDestroyFoliage()
    {
        yield return new WaitForSeconds(40f);
        Collider[] hitColliders = Physics.OverlapSphere(activeProjector.transform.position, 250 / 3.5f, combinedLayerMask);
        foreach (var hitCollider in hitColliders) 
        {
            if (IsTree(hitCollider) || IsFoliage(hitCollider)) 
            {
                foliageOrTreeColliderList.Add(hitCollider);
            }
        }
        while (true)
        {
            yield return new WaitForSeconds(2f);
            PerformSphereCast();
        }
    }

    private void PerformSphereCast()
    {
        //Stopwatch timer = new Stopwatch();
        //timer.Start();
        // Perform sphere cast
        Collider[] hitColliders = Physics.OverlapSphere(activeProjector.transform.position, activeProjector.size.y / 3.5f, combinedLayerMask);
        int foliageOrTreeCount = 0;
        foreach (var hitCollider in hitColliders) {
            // Check if the collider belongs to foliage or a tree
            if (foliageOrTreeColliderList.Contains(hitCollider))
            {
                foliageOrTreeCount++;
            }
        }

        foreach (var hitCollider in hitColliders)
        {
            // Check if the collider belongs to foliage or a tree
            if (foliageOrTreeColliderList.Contains(hitCollider))
            {
                foliageOrTreeColliderList.Remove(hitCollider);
                DestroyColliderObject(hitCollider, foliageOrTreeCount);
            }
        }

        //timer.Stop();
        //Plugin.ExtendedLogging($"Run completed in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms");
    }

    private bool IsFoliage(Collider collider)
    {
        var meshRenderer = collider.GetComponent<MeshRenderer>();
        var meshFilter = collider.GetComponent<MeshFilter>();
        return meshRenderer != null && meshFilter != null && collider.gameObject.layer == foliageLayer;
    }

    private bool IsTree(Collider collider)
    {
        var meshRenderer = collider.GetComponent<MeshRenderer>();
        var meshFilter = collider.GetComponent<MeshFilter>();
        return meshRenderer != null && meshFilter != null &&
            collider.CompareTag("Wood") && collider.gameObject.layer == terrainLayer && !collider.isTrigger;
    }

    private void DestroyColliderObject(Collider collider, int colliderCount)
    {
        Vector3 hitPosition = collider.transform.position;
        Quaternion hitRotation = collider.transform.rotation;

        if (collider.GetComponent<NetworkObject>() != null)
        {
            // Despawn if network spawned
            if (IsServer)
            {
                collider.GetComponent<NetworkObject>().Despawn();
            }
        }
        else
        {
            // Destroy if locally spawned
            Destroy(collider.gameObject);
        }

        // Instantiate dead particles at the position of the destroyed object
        if ((colliderCount != 0 && biomeRandom.NextInt(1, colliderCount) <= 5) || biomeRandom.NextInt(1, 100) <= 20) {
            ParticleSystem particles = Instantiate(deathParticles, hitPosition, hitRotation);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
   }
}