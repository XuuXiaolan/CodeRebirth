using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.Util.PlayerManager;
using GameNetcodeStuff;

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

    [SerializeField]
    private AnimationClip overrideClip = null!;
    private PlayerControllerB? previouslyHeldBy;

    private bool activated;

    public void ReplaceOrPutBackAnimationClip(PlayerControllerB? player, bool _override) {
        if (player == null) {
            Plugin.Logger.LogDebug("player is null");
            return;
        }
        var playerOverrideThing = CodeRebirthPlayerManager.dataForPlayer[player].playerOverrideController;
        if (playerOverrideThing == null) {
            Plugin.Logger.LogDebug("playerOverrideThing is null");
            return;
        }
        if (_override) {
            playerOverrideThing["HoldClipboard"] = overrideClip;
        } else {
            playerOverrideThing["HoldClipboard"] = null;
        }

    }

    public override void GrabItem()
    {
        base.GrabItem();
        previouslyHeldBy = playerHeldBy;
        if (previouslyHeldBy == null) {
            Plugin.Logger.LogDebug("previouslyHeldBy is null");
        }
        ReplaceOrPutBackAnimationClip(previouslyHeldBy, true);
    }
    public override void EquipItem()
    {
        base.EquipItem();
        previouslyHeldBy = playerHeldBy;
        if (previouslyHeldBy == null) {
            Plugin.Logger.LogDebug("previouslyHeldBy is null");
        }
        ReplaceOrPutBackAnimationClip(previouslyHeldBy, true);
        // Coming from pocketing since this is also called when using inventory
        ToggleParticleRenderer(true);
    }

    public override void PocketItem()
    {
        base.PocketItem();
        // Disable Particles renderer#
        if (previouslyHeldBy != null) ReplaceOrPutBackAnimationClip(previouslyHeldBy, false);
        ToggleParticleRenderer(false);
    }

    public override void DiscardItem()
    {
        if (previouslyHeldBy != null) ReplaceOrPutBackAnimationClip(previouslyHeldBy, false);
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