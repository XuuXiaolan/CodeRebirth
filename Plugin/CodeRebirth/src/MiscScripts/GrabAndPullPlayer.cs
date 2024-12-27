using System;
using System.Collections;
using CodeRebirth.src.Content.Maps;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class GrabAndPullPlayer : MonoBehaviour
{
    public Transform pullTransform = null!;
    private Coroutine? pullRoutine = null;
    public AudioSource audioSource = null!;
    public AudioClip[] pullSounds = [];

    public void OnTriggerStay(Collider other)
    {
        if (!enabled || pullRoutine != null) return;
        if (other.gameObject.layer != 3 || !other.gameObject.TryGetComponent(out PlayerControllerB player)) return;
        pullRoutine = StartCoroutine(PullAndTrapPlayer(player));
    }

    private IEnumerator PullAndTrapPlayer(PlayerControllerB player)
    {
        audioSource.PlayOneShot(pullSounds[UnityEngine.Random.Range(0, pullSounds.Length)]);
        player.disableMoveInput = true;
        ItemCrate crate = this.transform.parent.GetComponent<ItemCrate>();
        crate.CloseCrateOnPlayerLocally(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        yield return new WaitForSeconds(0.3f);
        while (this.enabled)
        {
            player.transform.position = pullTransform.position;
            yield return new WaitForFixedUpdate();
        }
        player.disableMoveInput = false;
        pullRoutine = null;
    }
}