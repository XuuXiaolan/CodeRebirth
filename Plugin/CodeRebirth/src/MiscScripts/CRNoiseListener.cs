using System;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
[RequireComponent(typeof(Collider))]
public class CRNoiseListener : MonoBehaviour, INoiseListener
{
    [SerializeField]
    internal UnityEvent<NoiseParams> _onNoiseDetected = new();

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        _onNoiseDetected.Invoke(new NoiseParams(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID));
    }
}

[Serializable]
public class NoiseParams(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
{
    internal Vector3 noisePosition = noisePosition;
    internal float noiseLoudness = noiseLoudness;
    internal int timesPlayedInOneSpot = timesPlayedInOneSpot;
    internal int noiseID = noiseID;
}