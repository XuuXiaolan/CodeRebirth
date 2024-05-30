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
using UnityEngine.AI;
using System.Collections.Generic;

namespace CodeRebirth.WeatherStuff;
public class Tornados : NetworkBehaviour {
    #pragma warning disable CS0649
    private Animator tornadoAnimater;
    private ParticleSystem tornadoTypeParticles;
    private float walkingSpeed = 1f;
	private float runningSpeed = 8f;
	private float timeBeforeNextMove = 1f;
	private AudioClip[] noiseSFX;
	private NavMeshAgent agent;
	private Vector3 destination;
	private GameObject[] allAINodes;
	private NavMeshPath navmeshPath;
	private float velX;
	private float velY;
	private Vector3 previousPosition;
	private Vector3 agentLocalVelocity;

    [Header("Properties")]
    [SerializeField] 
    private float initialSpeed = 15f;
    [Space(5f)]
    [Header("Audio")]
    [SerializeField]
    AudioSource NormalTravelAudio, CloseTravelAudio;
    [Space(5f)]
    [Header("Graphics")]
    [SerializeField]
    ParticleSystem Trail;

    private List<GameObject> outsideNodes = new List<GameObject>();
    private Vector3 origin;
    public enum TornadoType
    {
		Random,
        Fire,
        Electric,
        Windy,
    }
    public TornadoType tornadoType = TornadoType.Random;

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex, List<GameObject> outsideNodes) {
        this.origin = origin;
        this.outsideNodes = outsideNodes;
        this.tornadoType = (TornadoType) typeIndex; // I'm like pretty sure this doesn't do what i think it does but im pretty tired rn
        // Need to make the fields editor friendly too later.
        // Might need to add more stuff in relation to agents and moving around but doubt it?
        // Also make some client rpc's for animations and whatnot
        
        UpdateAudio(); // Make sure audio works correctly on the first frame.
        Trail.Play();

        tornadoTypeParticles = this.transform.Find("TornadoMain").Find("RandomTornado").GetComponent<ParticleSystem>();
        tornadoTypeParticles = this.transform.Find("TornadoMain").Find("FireTornado").GetComponent<ParticleSystem>();
        tornadoTypeParticles = this.transform.Find("TornadoMain").Find("ElectricTornado").GetComponent<ParticleSystem>();
        tornadoTypeParticles = this.transform.Find("TornadoMain").Find("WindyTornado").GetComponent<ParticleSystem>(); // placeholder stuff mostly, just setting it up to not forget for later
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
        }
    }
}