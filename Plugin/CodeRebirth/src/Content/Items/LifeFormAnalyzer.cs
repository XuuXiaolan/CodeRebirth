using System.Collections.Generic;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.MiscScripts.CustomPasses;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class LifeFormAnalyzer : GrabbableObject
{
    public Animator animator = null!;
    public float scanInterval = 10f;
    public TerrainScanner terrainScanner = null!;
    public AudioSource _audioSource = null!;
    public AudioClip _hazardPingSound = null!;
    public AudioClip _activatedSound = null!;
    public AudioClip _deactivatedSound = null!;

    private List<Coroutine> customPassRoutines = new();
    private float scanTimer = 0f;
    private bool turnedOn = false;
    private static readonly int ActivatedAnimation = Animator.StringToHash("activated");

    public override void Start()
    {
        base.Start();
        useCooldown = scanInterval;
    }

    public override void Update()
    {
        base.Update();

        if (!turnedOn) return;

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            _audioSource.PlayOneShot(_hazardPingSound);
            DoRevealScan();
            // do scan
        }
    }

    public void DoRevealScan()
    {
        // plays the visual effect from gabriel
        // GalVoice.PlayOneShot(hazardPingSound);
        ParticleSystem particleSystem = SeamineGalAI.DoTerrainScan(terrainScanner, transform.position);
        particleSystem.gameObject.transform.parent.gameObject.SetActive(true);
        if (Vector3.Distance(this.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) > 10) return;
        if (customPassRoutines.Count <= 0)
        {
            customPassRoutines.Add(StartCoroutine(SeamineGalAI.DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughEnemies, 25)));
            customPassRoutines.Add(StartCoroutine(SeamineGalAI.DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughHazards, 25)));
        }
        else
        {
            foreach (Coroutine coroutine in customPassRoutines)
            {
                StopCoroutine(coroutine);
            }
            customPassRoutines.Add(StartCoroutine(SeamineGalAI.DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughEnemies, 25)));
            customPassRoutines.Add(StartCoroutine(SeamineGalAI.DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughHazards, 25)));
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        turnedOn = !turnedOn;
        animator.SetBool(ActivatedAnimation, turnedOn);
        scanTimer = scanInterval;
        if (turnedOn)
        {
            _audioSource.PlayOneShot(_activatedSound);
            _audioSource.PlayOneShot(_hazardPingSound);
            DoRevealScan();
        }
        else
        {
            _audioSource.PlayOneShot(_deactivatedSound);
            foreach (Coroutine coroutine in customPassRoutines)
            {
                StopCoroutine(coroutine);
            }
            customPassRoutines.Clear();
        }
    }
}