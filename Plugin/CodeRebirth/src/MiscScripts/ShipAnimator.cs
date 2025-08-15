
using System.Collections;
using CodeRebirth.src.Util;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ShipAnimator : MonoBehaviour // Some of this code is from Kite, so thanks to them
{
    [HideInInspector] public AnimationClip shipLandAnimation = null!;
    [HideInInspector] public AnimationClip shipNormalLeaveAnimation = null!;
    private Animator hangarShipAnimator = null!;
    [HideInInspector] public AnimationClip originalShipLandClip = null!;
    [HideInInspector] public AnimationClip originalShipLeaveClip = null!;
    private RuntimeAnimatorController animatorController = null!; // turn off animator after finishing
    [HideInInspector] public AnimatorOverrideController overrideController = null!;
    private Coroutine? _messWithEyeVolumeRoutine = null;

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
            yield return new WaitUntil(() => StartOfRound.Instance.shipHasLanded);
            StartOfRound.Instance.shipAnimator.enabled = false;
            // turn off animator
            yield return new WaitUntil(() => StartOfRound.Instance.shipIsLeaving);
            StartOfRound.Instance.shipAnimator.enabled = true;
            int playersDead = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
            if (playersDead == 0)
            {
                var achievementKey = NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "greatest_asset");
                CRModContent.Achievements.TryTriggerAchievement(achievementKey);
            }
            // re-enable animator
            yield return new WaitUntil(() => RoundManager.Instance.currentLevel.sceneName != "Oxyde" || StartOfRound.Instance.inShipPhase);
            StartCoroutine(UnReplaceAnimationClip());
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
        _messWithEyeVolumeRoutine = StartCoroutine(MessWithEyeVolume());
        CRUtilities.CreateExplosion(StartOfRound.Instance.shipAnimatorObject.transform.position + Vector3.back * 3f, true, 0, 0, 10, 0, null, null, 0);
    }

    public void KillAllPlayersAnimEvent()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.isPlayerControlled || player.isPlayerDead)
                continue;
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

        yield return new WaitForSeconds(3f);
        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight < 1)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 1, 0.4f * Time.deltaTime);
        }
        _messWithEyeVolumeRoutine = null;
    }

    private IEnumerator UnReplaceAnimationClip()
    {
        hangarShipAnimator.runtimeAnimatorController = animatorController;
        yield return new WaitUntil(() => _messWithEyeVolumeRoutine == null);
        while (CodeRebirthUtils.Instance.CloseEyeVolume.weight > 0)
        {
            yield return null;
            CodeRebirthUtils.Instance.CloseEyeVolume.weight = Mathf.MoveTowards(CodeRebirthUtils.Instance.CloseEyeVolume.weight, 0, Time.deltaTime);
        }

        Plugin.ExtendedLogging("Reverted to the original HangarShipLand animation clip.");
    }
}