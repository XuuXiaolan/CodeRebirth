using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using CodeRebirth.src;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using CodeRebirth.Collisions;
using CodeRebirth.Misc;
using UnityEngine.Serialization;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;
public class Tornados : NetworkBehaviour {
    #pragma warning disable CS0649    
    [Header("Properties")]
    [SerializeField] private float initialSpeed = 15f;
    [Header("Audio")]
    [SerializeField]
    AudioSource NormalTravelAudio, CloseTravelAudio;

    [Header("Graphics")]
    [SerializeField]
    ParticleSystem Trail;
    [SerializeField]
    AnimationCurve animationCurve = AnimationCurve.Linear(0,0,1,1);
    
    Vector3 origin;
    

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin) {
        this.origin = origin;
        UpdateAudio(); // Make sure audio works correctly on the first frame.
        Trail.Play();
    }
    
    private void Awake() {
        TornadoWeather.Instance.AddTornado(this);
    }

    private void Update() {
        UpdateAudio();
        MoveMeteor();
    }

    private void MoveMeteor() {
        // Gonna give them some sort of navmesh agent
    }

    private void UpdateAudio() {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
            NormalTravelAudio.volume = 0;
            CloseTravelAudio.volume = 0;
        } else {
            NormalTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            CloseTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
        }
    }
}