using System.Collections;
using CodeRebirth.src.Content.Maps;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace CodeRebirth.src.MiscScripts;
public class GrabAndLaunchPlayer : MonoBehaviour
{
    public Transform pullTransform = null!;
    private Coroutine? pullRoutine = null;
    private PlayerControllerB? player = null;

    public void OnTriggerStay(Collider other)
    {
        if (!enabled || pullRoutine != null) return;
        if (other.gameObject.layer != 3 || !other.gameObject.TryGetComponent(out PlayerControllerB player)) return;
        pullRoutine = StartCoroutine(PullAndTrapPlayer(player));
    }

    private IEnumerator PullAndTrapPlayer(PlayerControllerB player)
    {
        player.disableMoveInput = true;
        this.player = player;
        ItemCrate crate = this.transform.parent.GetComponent<ItemCrate>();
        yield return new WaitForSeconds(1.375f);
        this.player = null;
        player.disableMoveInput = false;
        player.externalForceAutoFade += crate.transform.up * 1000f;
        yield return new WaitForSeconds(0.916f/2);
        crate.ResetWoodenCrate();
        yield return new WaitForSeconds(0.916f/2);
        pullRoutine = null;
        this.enabled = false;
    }

    public void Update()
    {
        if (pullRoutine != null && player != null)
        {
            player.transform.position = pullTransform.position;
        }
    }
}