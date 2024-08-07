using CodeRebirth.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Diagnostics;

namespace CodeRebirth.MapStuff
{
    public class BiomeManager : NetworkBehaviour
    {
        public DecalProjector corruptionProjector = null!;
        public DecalProjector crimsonProjector = null!;
        public DecalProjector hallowProjector = null!;
        
        private ParticleSystem deathParticles = null!;
        private DecalProjector activeProjector = null!;
        private System.Random random = new System.Random(69);
        private int foliageLayer;
        private int terrainLayer;

        public void Start()
        {
            if (StartOfRound.Instance != null)
            {
                random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
            }

            switch (random.NextInt(0, 2))
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
            foliageLayer = LayerMask.NameToLayer("Foliage");
            terrainLayer = LayerMask.NameToLayer("Terrain");
            StartCoroutine(CheckAndDestroyFoliage());
        }

        public void Update()
        {
            if (activeProjector == null) return;
            if (activeProjector.size.x >= 350 || activeProjector.size.y >= 350) return;
            activeProjector.size += new Vector3(Time.deltaTime * 0.34f, Time.deltaTime * 0.34f, 0f);
        }

        private IEnumerator CheckAndDestroyFoliage()
        {
            while (true)
            {
                yield return new WaitForSeconds(10f);
                PerformSphereCast();
            }
        }

        private void PerformSphereCast()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            // Combine the foliage layer with the terrain layer to perform sphere cast on both
            int combinedLayerMask = (1 << foliageLayer) | (1 << terrainLayer);
            
            // Perform sphere cast
            Collider[] hitColliders = Physics.OverlapSphere(activeProjector.transform.position, activeProjector.size.y / 4f, combinedLayerMask);
            foreach (var hitCollider in hitColliders)
            {
                // Check if the collider belongs to foliage or a tree
                if (IsFoliage(hitCollider) || IsTree(hitCollider))
                {
                    DestroyColliderObject(hitCollider);
                }
            }

            timer.Stop();
            Plugin.ExtendedLogging($"Run completed in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms");
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
                collider.CompareTag("Wood") &&
                collider.gameObject.layer == terrainLayer &&
                collider != null && !collider.isTrigger;
        }

        private void DestroyColliderObject(Collider collider)
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
            ParticleSystem particles = Instantiate(deathParticles, hitPosition, hitRotation);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }
    }
}