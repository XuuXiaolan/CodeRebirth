using System.Collections;
using System.Linq;
using CodeRebirth.src.ModCompats;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class PlanetUnlocker : GrabbableObject
{
    public string moonSceneName = "Oxyde";
    public string extraText = string.Empty;
    public AudioSource audioPlayer = null!;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        playerHeldBy.inSpecialInteractAnimation = true;
        audioPlayer.Play();
        // todo
        /*LevelManager.TryGetExtendedLevel(StartOfRound.Instance.levels.Where(x => x.sceneName == moonSceneName).FirstOrDefault(), out ExtendedLevel? extendedLevel);
        if (extendedLevel != null)
        {
            HUDManager.Instance.DisplayTip("Success", $"Coordinates to {moonSceneName} found.\n{extraText}", false);
            if (LethalMoonUnlocksCompat.LethalMoonUnlocksExists)
            {
                LethalMoonUnlocksCompat.ReleaseOxydeStoryLock(extendedLevel);
            }

            extendedLevel.IsRouteHidden = false;
            extendedLevel.IsRouteLocked = false;
        }
        else
        {
            HUDManager.Instance.DisplayTip("Error", $"Coordinates to {moonSceneName} could not be verified, Cancelling operation.\n{extraText}", true);
        }*/
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        if (audioPlayer.clip != null)
        {
            yield return new WaitForSeconds(audioPlayer.clip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
        playerHeldBy.inSpecialInteractAnimation = false;
        if (!playerHeldBy.IsLocalPlayer())
            yield break;

        playerHeldBy.DespawnHeldObject();
    }
}