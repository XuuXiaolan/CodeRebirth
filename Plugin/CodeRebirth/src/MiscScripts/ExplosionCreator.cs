using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ExplosionCreator : MonoBehaviour
{
    [HideInInspector] public GameObject Explosion = null!;
    [HideInInspector] public AudioSource audio = null!;
    [HideInInspector] public ParticleSystem[] particleSystems = [];

    public void Start()
    {
        Explosion = GameObject.Instantiate(StartOfRound.Instance.explosionPrefab, this.transform); // seems to be missing materials and audio?
        audio = Explosion.GetComponentInChildren<AudioSource>();
        particleSystems = Explosion.GetComponentsInChildren<ParticleSystem>();
        Explosion.SetActive(false);
    }
}