using System.Collections;
using Dawn;
using Dawn.Utils;
using Dusk;
using UnityEngine;
using UnityEngine.Video;

namespace CodeRebirth.src.Content.Items;
public class CRPlanetUnlocker : GrabbableObject
{
    [Header("Planet Unlocker Settings")]
    [SerializeReference]
    private DuskMoonReference _moonReference;

    [SerializeField]
    private bool _consumeOnUnlock = true;

    [SerializeField]
    [Tooltip("Leave empty to have no audio")]
    private AudioSource _unlockAudio;

    [SerializeField]
    [Tooltip("Leave empty to have no videoPlayer")]
    private VideoPlayer _unlockVideoPlayer;

    [Header("Notification Settings")]
    [SerializeField]
    private bool _showDisplayTip;

    [SerializeField]
    private HUDDisplayTip _displayTip;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        playerHeldBy.inSpecialInteractAnimation = true;

        if (!TryUnlock()) // failed to unlock
        {
            HUDManager.Instance.DisplayTip(new HUDDisplayTip(
                "Error",
                $"Coordinates to {_moonReference.Key.Key} could not be verified, Cancelling.",
                HUDDisplayTip.AlertType.Warning
            ));
        }

        if (_unlockAudio)
        {
            _unlockAudio.Play();
        }

        if (_unlockVideoPlayer)
        {
            _unlockVideoPlayer.gameObject.SetActive(true);
            if (!_unlockVideoPlayer.playOnAwake)
            {
                _unlockVideoPlayer.Play();
            }
        }

        StartCoroutine(WaitToDespawn());
    }

    private bool TryUnlock()
    {
        if (_moonReference.TryResolve(out DawnMoonInfo moonInfo))
        {
            if (moonInfo.DawnPurchaseInfo.PurchasePredicate is not ProgressivePredicate progressive)
            {
                HUDManager.Instance.DisplayTip(_displayTip);
                return false;
            }

            progressive.Unlock(_showDisplayTip ? _displayTip : null);
            return true;
        }
        else
        {
            HUDManager.Instance.DisplayTip(_displayTip);
            return false;
        }
    }

    private IEnumerator WaitToDespawn()
    {
        if (_unlockVideoPlayer && _unlockVideoPlayer.clip != null)
        {
            yield return new WaitForSeconds((float)_unlockVideoPlayer.clip.length);
        }
        else if (_unlockAudio && _unlockAudio.clip != null)
        {
            yield return new WaitForSeconds(_unlockAudio.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        playerHeldBy.inSpecialInteractAnimation = false;
        if (!playerHeldBy.IsLocalPlayer())
        {
            yield break;
        }

        if (_consumeOnUnlock)
        {
            playerHeldBy.DespawnHeldObject();
        }
    }
}