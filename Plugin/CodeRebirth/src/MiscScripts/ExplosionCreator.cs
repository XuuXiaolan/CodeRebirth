using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ExplosionCreator : MonoBehaviour
{
    [HideInInspector] public GameObject Explosion = null!;
    [HideInInspector] public AudioSource audio = null!;
    [HideInInspector] public ParticleSystem[] particleSystems = [];

    public void Start()
    {
        Explosion = StartOfRound.Instance.explosionPrefab;
        audio = Explosion.transform.Find("Audio").gameObject.GetComponent<AudioSource>();
        particleSystems = Explosion.GetComponentsInChildren<ParticleSystem>();
    }
}