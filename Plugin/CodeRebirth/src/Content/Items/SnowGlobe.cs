using UnityEngine;
using System.Collections;
using GameNetcodeStuff;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Items;
public class SnowGlobe : GrabbableObject
{
    [Tooltip("Lights")]
    [SerializeField]
    private GameObject mainLightGameObject = null!;
    [SerializeField]
    private GameObject[] redLightsGameObject = null!;
    [SerializeField]
    private GameObject[] blueLightsGameObject = null!;
    [SerializeField]
    private GameObject[] greenLightsGameObject = null!;
    [Space(5)]

    [SerializeField]
    private Animator shipAnimator = null!;

    [SerializeField]
    private ParticleSystem snowPS = null!;

    [SerializeField]
    private ParticleSystemRenderer snowPSR = null!;

    [SerializeField]
    private AudioSource musicAS = null!;
    private PlayerControllerB? previouslyHeldBy;
    private bool activated;
    private System.Random snow = new();

    public override void Start()
    {
        base.Start();
        snow = new System.Random(StartOfRound.Instance.randomMapSeed + 32);
    }

    public override void Update()
    {
        base.Update();
        if (previouslyHeldBy == null) return;
    }

    public override void GrabItem()
    {
        base.GrabItem();
        previouslyHeldBy = playerHeldBy;
        if (previouslyHeldBy == null)
        {
            Plugin.Logger.LogDebug("previouslyHeldBy is null");
        }
    }

    public override void EquipItem()
    {
        base.EquipItem();
        previouslyHeldBy = playerHeldBy;
        if (previouslyHeldBy == null)
        {
            Plugin.Logger.LogDebug("previouslyHeldBy is null");
        }
        // Coming from pocketing since this is also called when using inventory
        ToggleParticleRenderer(true);
    }

    public override void PocketItem()
    {
        base.PocketItem();
        // Disable Particles renderer#
        ToggleParticleRenderer(false);
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!activated)
        {
            /*if (snow.Next(100) < 5)
            {
                SlowDownEffect.DoSlowdownEffect(10, 0.2f);
            }*/
            if (Plugin.ModConfig.ConfigSnowGlobeMusic.Value)
            {
                musicAS.volume = 1;
            }
            else
            {
                musicAS.volume = 0;
            }
            StartCoroutine(ActivateSnowGlobeCoroutine());
            activated = true;
        }
    }

    public IEnumerator ActivateSnowGlobeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        RoundManager.Instance.PlayAudibleNoise(this.transform.position, 7, 1, 0, isInShipRoom);
        yield return ToggleSnowGlobeCoroutine(true);
        yield return new WaitUntil(() => !musicAS.isPlaying);
        RoundManager.Instance.PlayAudibleNoise(this.transform.position, 7, 1, 0, isInShipRoom);
        yield return ToggleSnowGlobeCoroutine(false);
        yield return new WaitForSeconds(2f);
        activated = false;
    }

    private IEnumerator ToggleSnowGlobeCoroutine(bool toggle, float delay = 0.2f)
    {
        ToggleParticles(toggle);
        ToggleMusic(toggle);
        //if (toggle) StartCoroutine(RotateLights());
        shipAnimator.SetBool("doorsActivated", toggle);
        yield return new WaitForSeconds(delay);
        mainLightGameObject.SetActive(toggle);
    }

    public IEnumerator RotateLights()
    {
        int currentLightIndex = 0;
        int totalSeconds = 17;

        while (totalSeconds > 0)
        {
            // Deactivate all lights before activating the next set
            ToggleLights(false);
            // Activate lights based on current index
            switch (currentLightIndex % 3)
            {
                case 0: // Red lights
                    foreach (GameObject light in redLightsGameObject)
                    {
                        light.SetActive(true);
                    }
                    break;
                case 1: // Green lights
                    foreach (GameObject light in greenLightsGameObject)
                    {
                        light.SetActive(true);
                    }
                    break;
                case 2: // Blue lights
                    foreach (GameObject light in blueLightsGameObject)
                    {
                        light.SetActive(true);
                    }
                    break;
            }
            if (totalSeconds <= 2)
            {
                ToggleLights(true);
            }
            // Wait for 1 second
            yield return new WaitForSeconds(1);

            // Increment and decrement counters
            currentLightIndex++;
            totalSeconds--;
        }
        ToggleLights(false);
    }

    private void ToggleLights(bool toggle)
    {
        foreach (GameObject light in redLightsGameObject)
        {
            light.SetActive(toggle);
        }
        foreach (GameObject light in greenLightsGameObject)
        {
            light.SetActive(toggle);
        }
        foreach (GameObject light in blueLightsGameObject)
        {
            light.SetActive(toggle);
        }
    }

    private void ToggleParticles(bool toggle)
    {
        if (toggle)
            snowPS.Play();
        else
            snowPS.Stop();
    }

    private void ToggleMusic(bool toggle)
    {
        if (toggle)
            musicAS.Play();
        else
            musicAS.Stop();
    }

    private void ToggleParticleRenderer(bool toggle)
    {
        snowPSR.enabled = toggle;
    }
}