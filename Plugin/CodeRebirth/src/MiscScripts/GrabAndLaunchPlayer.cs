using System.Collections;
using CodeRebirth.src.Content.Maps;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;
using CodeRebirthLib.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class GrabAndLaunchPlayer : MonoBehaviour
{
    public Transform pullTransform = null!;
    private Coroutine? pullRoutine = null;
    private PlayerControllerB? playerToLaunch = null;
    public AudioSource audioSource = null!;
    public AudioClip[] launchSounds = [];

    public void OnTriggerStay(Collider other)
    {
        if (!enabled || pullRoutine != null) return;
        if (!other.gameObject.TryGetComponent(out PlayerControllerB player)) return;
        pullRoutine = StartCoroutine(PullAndTrapPlayer(player));
    }

    private IEnumerator PullAndTrapPlayer(PlayerControllerB player)
    {
        audioSource.PlayOneShot(launchSounds[Random.Range(0, launchSounds.Length)]);
        player.disableMoveInput = true;
        playerToLaunch = player;
        ItemCrate crate = this.transform.parent.GetComponent<ItemCrate>();
        yield return new WaitForSeconds(1.375f);
        playerToLaunch = null;
        player.disableMoveInput = false;
        player.externalForceAutoFade += crate.transform.up * 1000f;
        yield return new WaitForSeconds(0.916f / 2);
        crate.ResetWoodenCrate();
        yield return new WaitForSeconds(0.916f / 2);
        StartCoroutine(CheckIfPlayerIsDead(player));
        pullRoutine = null;
        this.enabled = false;
    }

    private IEnumerator CheckIfPlayerIsDead(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer())
            yield break;

        yield return new WaitUntil(() => player.isPlayerDead || player.thisController.isGrounded);
        if (player.isPlayerDead)
        {
            yield break;
        }

        CRModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.Crateapult);
    }

    public void Update()
    {
        if (pullRoutine != null && playerToLaunch != null)
        {
            playerToLaunch.transform.position = pullTransform.position;
        }
    }
}