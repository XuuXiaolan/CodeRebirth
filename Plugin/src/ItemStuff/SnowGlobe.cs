using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CodeRebirth.ItemStuff;
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

    private bool activated;
    public AnimatorOverrideController SnowGlobeOverride = null!;

    // Dictionary to hold animator state helpers for each player
    private Dictionary<ulong, PlayerAnimatorStateHelper> animatorStateHelpers = new Dictionary<ulong, PlayerAnimatorStateHelper>();

    public override void EquipItem()
    {
        base.EquipItem();

        ulong playerId = playerHeldBy.actualClientId;
        if (!animatorStateHelpers.ContainsKey(playerId))
        {
            animatorStateHelpers[playerId] = new PlayerAnimatorStateHelper(playerHeldBy.playerBodyAnimator);
        }

        var animatorStateHelper = animatorStateHelpers[playerId];
        animatorStateHelper.SaveAnimatorStates();
        animatorStateHelper.SetAnimatorOverrideController(SnowGlobeOverride);
        Debug.Log("Animator override set for player: " + playerHeldBy.playerBodyAnimator.gameObject.name);

        // Coming from pocketing since this is also called when using inventory
        ToggleParticleRenderer(true);
    }

    public override void PocketItem()
    {
        if (isHeld)
        {
            ulong playerId = playerHeldBy.actualClientId;
            if (animatorStateHelpers.ContainsKey(playerId))
            {
                var animatorStateHelper = animatorStateHelpers[playerId];
                animatorStateHelper.SaveAnimatorStates();
                animatorStateHelper.RestoreOriginalAnimatorController();
                Debug.Log("Animator restored for player: " + playerHeldBy.playerBodyAnimator.gameObject.name);
            }
        }
        base.PocketItem();

        // Disable Particles renderer
        ToggleParticleRenderer(false);
    }

    public override void DiscardItem()
    {
        ulong playerId = playerHeldBy.actualClientId;
        if (animatorStateHelpers.ContainsKey(playerId))
        {
            var animatorStateHelper = animatorStateHelpers[playerId];
            animatorStateHelper.SaveAnimatorStates();
            animatorStateHelper.RestoreOriginalAnimatorController();
            Debug.Log("Animator restored for player: " + playerHeldBy.playerBodyAnimator.gameObject.name);
        }
        base.DiscardItem();
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!activated)
        {
            if (Plugin.ModConfig.ConfigSnowGlobeMusic.Value) {
                musicAS.volume = 1;
            } else {
                musicAS.volume = 0;
            }
            StartCoroutine(ActivateSnowGlobeCoroutine());
            activated = true;
        }
    }

    public IEnumerator ActivateSnowGlobeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return ToggleSnowGlobeCoroutine(true);
        yield return new WaitUntil(() => !musicAS.isPlaying);
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

    void ToggleLights(bool toggle)
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