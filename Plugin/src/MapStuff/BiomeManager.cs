using CodeRebirth.Util.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;

namespace CodeRebirth.MapStuff
{
    public class BiomeManager : NetworkBehaviour
    {
        public DecalProjector corruptionProjector = null!;
        public DecalProjector crimsonProjector = null!;
        public DecalProjector hallowProjector = null!;
        
        private ParticleSystem deadParticles = null!;
        private DecalProjector activeProjector = null!;
        private System.Random random = new System.Random(69);
        private int foliageLayer;

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

            deadParticles = activeProjector.gameObject.GetComponentInChildren<ParticleSystem>();
            foliageLayer = LayerMask.NameToLayer("Foliage");
            StartCoroutine(CheckAndDestroyFoliage());
        }

        public void Update()
        {
            if (activeProjector == null) return;

            activeProjector.gameObject.transform.localScale += new Vector3(Time.deltaTime * 0.34f, Time.deltaTime * 0.34f, 0f);
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
        { // todo: make it destroy destroyable trees too.
            Collider[] hitColliders = Physics.OverlapSphere(activeProjector.transform.position, activeProjector.gameObject.transform.localScale.y/4f, 1 << foliageLayer);
            foreach (var hitCollider in hitColliders)
            {
                var meshRenderer = hitCollider.GetComponent<MeshRenderer>();
                var meshFilter = hitCollider.GetComponent<MeshFilter>();

                if (meshRenderer != null && meshFilter != null)
                {
                    Vector3 hitPosition = hitCollider.transform.position;
                    Quaternion hitRotation = hitCollider.transform.rotation;

                    if (hitCollider.GetComponent<NetworkObject>() != null)
                    {
                        // Despawn if network spawned
                        if (IsServer)
                        {
                            hitCollider.GetComponent<NetworkObject>().Despawn();
                        }
                    }
                    else
                    {
                        // Destroy if locally spawned
                        Destroy(hitCollider.gameObject);
                    }

                    // Instantiate dead particles at the position of the destroyed foliage
                    ParticleSystem particles = Instantiate(deadParticles, hitPosition, hitRotation);
                    particles.Play();
                    Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
                }
            }
        }
    }
}