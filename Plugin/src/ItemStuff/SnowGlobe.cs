using UnityEngine;
using System.Collections;

namespace CodeRebirth.ItemStuff;
public class SnowGlobe : GrabbableObject {
    [SerializeField]
    private ScanNodeProperties scanNode;

    [SerializeField]
    private GameObject lightGameObject;

    [SerializeField]
    private Animator shipAnimator;

    [SerializeField]
    private ParticleSystem snowPS;

    [SerializeField]
    private ParticleSystemRenderer snowPSR;

    [SerializeField]
    private AudioSource musicAS;

    private bool activated;

    private RuntimeAnimatorController originalAnimatorController;

    public AnimatorOverrideController SnowGlobeOverride;

    public override void EquipItem()
    {
        base.EquipItem();
        originalAnimatorController = playerHeldBy.playerBodyAnimator.runtimeAnimatorController;
        playerHeldBy.playerBodyAnimator.runtimeAnimatorController = SnowGlobeOverride;

        //Coming from pocketing since this is also called when using inventory
        ToggleParticleRenderer(true);
    }
    public override void PocketItem()
    {
        base.PocketItem();
        playerHeldBy.playerBodyAnimator.runtimeAnimatorController = originalAnimatorController;

        //Disable Particles renderer
        ToggleParticleRenderer(false);
    }
    public override void DiscardItem()
    {
        playerHeldBy.playerBodyAnimator.runtimeAnimatorController = originalAnimatorController;
        base.DiscardItem();
    }
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if(!activated)
        {
            StartCoroutine(ActivateSnowGlobeCoroutine());
            activated = true;
        }
        
    }
    public IEnumerator ActivateSnowGlobeCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return ToggleSnowGlobeCoroutine(true);

        yield return new WaitForSeconds(17f);
        yield return ToggleSnowGlobeCoroutine(false);
        yield return new WaitForSeconds(2f);
        activated = false;
    }
    IEnumerator ToggleSnowGlobeCoroutine(bool toggle, float delay = 0.2f)
    {
        ToggleParticles(toggle);
        ToggleMusic(toggle);
        shipAnimator.SetBool("doorsActivated", toggle);
        yield return new WaitForSeconds(delay);
        lightGameObject.SetActive(toggle);
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