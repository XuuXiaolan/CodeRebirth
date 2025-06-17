using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;

namespace CodeRebirth.src.Content.Maps;
public class BiomeManager : CodeRebirthHazard
{
    public DecalProjector corruptionProjector = null!;
    public DecalProjector crimsonProjector = null!;
    public DecalProjector hallowProjector = null!;

    private ParticleSystem deathParticles = null!;
    private DecalProjector activeProjector = null!;
    private System.Random biomeRandom = new System.Random(69);
    private static int foliageLayer = 0;
    private static int terrainLayer = 0;
    private List<Collider> foliageOrTreeColliderList = new();
    private Collider[] cachedColliders = new Collider[10];

    public static List<BiomeManager> Instances = new();
    public static bool Active => Instances.Count > 0;

    public override void Start()
    {
        base.Start();
        foliageLayer = LayerMask.NameToLayer("Foliage");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        if (StartOfRound.Instance != null)
        {
            biomeRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        switch (biomeRandom.Next(3))
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
        Collider[] hitColliders = Physics.OverlapSphere(activeProjector.transform.position, 250 / 3.5f, MoreLayerMasks.terrainAndFoliageMask);
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
        int numHit = Physics.OverlapSphereNonAlloc(activeProjector.transform.position, activeProjector.size.y / 3.5f, cachedColliders, MoreLayerMasks.terrainAndFoliageMask);
        int foliageOrTreeCount = 0;
        for (int i = numHit - 1; i >= 0; i--)
        {
            if (foliageOrTreeColliderList.Contains(cachedColliders[i]))
            {
                foliageOrTreeCount++;
                foliageOrTreeColliderList.Remove(cachedColliders[i]);
                DestroyColliderObject(cachedColliders[i], foliageOrTreeCount);
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
        if ((colliderCount != 0 && biomeRandom.Next(1, colliderCount + 1) <= 5) || biomeRandom.Next(1, 101) <= 20)
        {
            ParticleSystem particles = Instantiate(deathParticles, hitPosition, hitRotation);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
    }
}