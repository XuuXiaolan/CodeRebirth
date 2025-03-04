
using System.Collections;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ShipAnimator : MonoBehaviour // Some of this code is from Kite, so thanks to them
{
    [HideInInspector] public AnimationClip shipLandAnimation = null!;
    [HideInInspector] public AnimationClip shipNormalLeaveAnimation = null!;
    private Animator hangarShipAnimator = null!;
    [HideInInspector] public AnimationClip originalShipLandClip = null!;
    [HideInInspector] public AnimationClip originalShipLeaveClip = null!; // todo: set a thing so that when merchant is firing it crashes the ship, turns on a darkening volume, shakes the camera, explodes the ship
    private RuntimeAnimatorController animatorController = null!;
    [HideInInspector] public AnimatorOverrideController overrideController = null!;

    private void Start()
    {
        hangarShipAnimator = StartOfRound.Instance.shipAnimator;
        animatorController = hangarShipAnimator.runtimeAnimatorController;

        foreach (var animClip in animatorController.animationClips)
        {
            if (animClip.name == "HangarShipLandB")
            {
                originalShipLandClip = animClip;
                continue;
            }

            if (animClip.name == "ShipLeave")
            {
                originalShipLeaveClip = animClip;
                continue;
            }
        }
        StartCoroutine(WaitToReplaceClip());
    }

    private IEnumerator WaitToReplaceClip()
    {
        while (true)
        {
            yield return new WaitUntil(() => RoundManager.Instance.currentLevel.sceneName == "Oxyde" && !StartOfRound.Instance.inShipPhase);
            ReplaceAnimationClip();
            yield return new WaitUntil(() => RoundManager.Instance.currentLevel.sceneName != "Oxyde" || StartOfRound.Instance.inShipPhase);
            UnReplaceAnimationClip();
        }
    }

    private void ReplaceAnimationClip()
    {
        overrideController = new AnimatorOverrideController(animatorController);
        overrideController[originalShipLandClip] = shipLandAnimation;
        overrideController[originalShipLeaveClip] = shipNormalLeaveAnimation;
        hangarShipAnimator.runtimeAnimatorController = overrideController;
        Plugin.ExtendedLogging("Replaced HangarShipLand with the custom animation clip.");
    }

    public void TurnOnDarkenedVolumeAnimEvent()
    {
        Plugin.ExtendedLogging($"Animation Event worked");
        StartOfRound.Instance.shipDoorAudioSource.PlayOneShot(StartOfRound.Instance.alarmSFX);
        SoundManager.Instance.earsRingingTimer = 0.6f;
        StartCoroutine(MessWithEyeVolume());
        CRUtilities.CreateExplosion(StartOfRound.Instance.shipAnimatorObject.transform.position + Vector3.back * 3f, true, 0, 0, 10, 0, null, null, 0);
    }

    public void KillAllPlayersAnimEvent()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead) continue;
            CRUtilities.CreateExplosion(player.transform.position, true, 999, 0, 5, 50, null, null, 100f);
        }
        CRUtilities.CreateExplosion(StartOfRound.Instance.shipAnimatorObject.transform.position + Vector3.forward * 3f + Vector3.left * 3f, true, 0, 0, 10, 0, null, null, 0);
        CRUtilities.CreateExplosion(StartOfRound.Instance.shipAnimatorObject.transform.position + Vector3.forward * 3f + Vector3.right * 3f, true, 0, 0, 10, 0, null, null, 0);
    }

    private IEnumerator MessWithEyeVolume()
    {
        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight < 0.8f)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 0.8f, 4 * Time.deltaTime);
        }

        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight > 0.3f)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 0.3f, 0.5f * Time.deltaTime);
        }
    }

    private void UnReplaceAnimationClip()
    {
        hangarShipAnimator.runtimeAnimatorController = animatorController;
        if (CodeRebirthUtils.Instance.CloseEyeVolume.weight >= 0.29f) CodeRebirthUtils.Instance.CloseEyeVolume.weight = 0;

        Plugin.ExtendedLogging("Reverted to the original HangarShipLand animation clip.");
    }
}