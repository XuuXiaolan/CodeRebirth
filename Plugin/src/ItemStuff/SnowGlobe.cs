using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;
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

    public override void Start() {
        base.Start();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        originalAnimatorController = playerHeldBy.playerBodyAnimator.runtimeAnimatorController;
        playerHeldBy.playerBodyAnimator.runtimeAnimatorController = SnowGlobeOverride;

        //Coming from pocketing since this is also called when using inventory
        snowPSR.enabled = true;

    }


    public override void PocketItem()
    {
        playerHeldBy.playerBodyAnimator.runtimeAnimatorController = originalAnimatorController;
        base.PocketItem();
        
        //Disable Particles renderer
        snowPSR.enabled = false;
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
        snowPS.Play();
        musicAS.Play();
        shipAnimator.SetBool("doorsActivated", true);
        yield return new WaitForSeconds(0.2f);
        lightGameObject.SetActive(true);

        yield return new WaitForSeconds(17f);
        snowPS.Stop();
        musicAS.Stop();
        shipAnimator.SetBool("doorsActivated", false);
        yield return new WaitForSeconds(0.2f);
        lightGameObject.SetActive(false);
        yield return new WaitForSeconds(2f);
        activated = false;
    }

}