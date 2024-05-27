using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;
using System.Collections;

namespace CodeRebirth.ItemStuff;
public class SnowGlobe : GrabbableObject
{
    [Tooltip("Lights")]
    [SerializeField]
    private GameObject mainLightGameObject;
    [SerializeField]
    private GameObject[] redLightsGameObject;
    [SerializeField]
    private GameObject[] blueLightsGameObject;
    [SerializeField]
    private GameObject[] greenLightsGameObject;
    [Space(5)]
    
    [SerializeField]
    private ScanNodeProperties scanNode;

    [SerializeField]
    private Animator shipAnimator;

    [SerializeField]
    private ParticleSystem snowPS;

    [SerializeField]
    private ParticleSystemRenderer snowPSR;

    [SerializeField]
    private AudioSource musicAS;

    private bool activated;
    public AnimatorOverrideController SnowGlobeOverride;

    private PlayerAnimatorStateHelper animatorStateHelper;

    public override void EquipItem()
    {
        base.EquipItem();

        if (animatorStateHelper == null && playerHeldBy != null && playerHeldBy.playerBodyAnimator != null)
        {
            animatorStateHelper = new PlayerAnimatorStateHelper(playerHeldBy.playerBodyAnimator);
        }

        if (animatorStateHelper != null)
        {
            animatorStateHelper.SaveAnimatorStates();
            animatorStateHelper.SetAnimatorOverrideController(SnowGlobeOverride);
        }

        // Coming from pocketing since this is also called when using inventory
        ToggleParticleRenderer(true);
    }

    public override void PocketItem()
    {
        if (animatorStateHelper != null)
        {
            animatorStateHelper.SaveAnimatorStates();
            animatorStateHelper.RestoreOriginalAnimatorController();
        }
        base.PocketItem();

        // Disable Particles renderer
        ToggleParticleRenderer(false);
    }

    public override void DiscardItem()
    {
        if (animatorStateHelper != null)
        {
            animatorStateHelper.SaveAnimatorStates();
            animatorStateHelper.RestoreOriginalAnimatorController();
        }
        base.DiscardItem();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!activated)
        {
            StartCoroutine(ActivateSnowGlobeCoroutine());
            activated = true;
        }
    }

    public IEnumerator ActivateSnowGlobeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return ToggleSnowGlobeCoroutine(true);
        yield return new WaitUntil(() => musicAS.isPlaying == false);
        yield return ToggleSnowGlobeCoroutine(false);
        yield return new WaitForSeconds(2f);
        activated = false;
    }
    IEnumerator ToggleSnowGlobeCoroutine(bool toggle, float delay = 0.2f)
    {
        ToggleParticles(toggle);
        ToggleMusic(toggle);
        //if (toggle) StartCoroutine(RotateLights());
        shipAnimator.SetBool("doorsActivated", toggle);
        yield return new WaitForSeconds(delay);
        mainLightGameObject.SetActive(toggle);
    }
    public IEnumerator RotateLights() {
        int currentLightIndex = 0;
        int totalSeconds = 17;

        while (totalSeconds > 0) {
            // Deactivate all lights before activating the next set
            ToggleLights(false);
            // Activate lights based on current index
            switch (currentLightIndex % 3) {
                case 0: // Red lights
                    foreach (GameObject light in redLightsGameObject) {
                        light.SetActive(true);
                    }
                    break;
                case 1: // Green lights
                    foreach (GameObject light in greenLightsGameObject) {
                        light.SetActive(true);
                    }
                    break;
                case 2: // Blue lights
                    foreach (GameObject light in blueLightsGameObject) {
                        light.SetActive(true);
                    }
                    break;
            }
            if (totalSeconds <= 2) {
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
    void ToggleLights(bool toggle) {
        foreach (GameObject light in redLightsGameObject) {
            light.SetActive(toggle);
        }
        foreach (GameObject light in greenLightsGameObject) {
            light.SetActive(toggle);
        }
        foreach (GameObject light in blueLightsGameObject) {
            light.SetActive(toggle);
        }
    }
    void ToggleParticles(bool toggle)
    {
        if (toggle)
            snowPS.Play();
        else
            snowPS.Stop();
    }
    void ToggleMusic(bool toggle)
    {
        if (toggle)
            musicAS.Play();
        else
            musicAS.Stop();
    }
    void ToggleParticleRenderer(bool toggle)
    {
        snowPSR.enabled = toggle;
    }
}