using System.Collections;
using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    public Animator animator = null!;
    public AudioClip rechargedSound = null!;
    public AudioClip timeStopEndSound = null!;
    public AudioClip watchUseSound = null!;
    public AudioSource watchSource = null!;
    public AudioSource watchIdleSource = null!;

    private static readonly int activateWatch = Animator.StringToHash("activateWatch");

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        watchIdleSource.Play();
        watchSource.PlayOneShot(watchUseSound);
        animator.SetTrigger(activateWatch);
        SlowDownEffect.DoSlowdownEffect(30, 0.2f);
        StartCoroutine(WaitUntilEffectEnds(0.2f * 30));
    }

    private IEnumerator WaitUntilEffectEnds(float length)
    {
        yield return new WaitForSeconds(length);
        watchIdleSource.Stop();
        watchSource.PlayOneShot(timeStopEndSound);
        yield return new WaitUntil(() => currentUseCooldown == 0);
        watchSource.PlayOneShot(rechargedSound);
    }
}