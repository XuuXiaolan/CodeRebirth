using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class PlayEffectsOnAnimation : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem[] _particleSystems = [];

    public void PlayEffect()
    {
        foreach (var effect in _particleSystems)
        {
            effect.Play();
        }
    }

    public void PlayEffect(int effectIndex)
    {
        _particleSystems[effectIndex].Play(true);
    }
}