using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class PlayEffectOnAnimation : MonoBehaviour
{
    public List<ParticleSystem> particleSystems = new();

    public void PlayEffectAnimEvent(int particleIndexToPlay)
    {
        particleSystems[particleIndexToPlay].Play(true);
    }
}