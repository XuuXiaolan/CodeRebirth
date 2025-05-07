using System.Collections;
using System.Linq;
using LethalLevelLoader;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class PlanetUnlocker : GrabbableObject
{
    public string moonSceneName = "Oxyde";
    public float timeBeforeDespawning = 5f;
    public AudioSource audioPlayer = null!;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        audioPlayer.Play();
        // playerHeldBy.inSpecialInteractAnimation = true;
        LevelManager.TryGetExtendedLevel(StartOfRound.Instance.levels.Where(x => x.sceneName == moonSceneName).FirstOrDefault(), out ExtendedLevel? extendedLevel);
        if (extendedLevel != null)
        {
            HUDManager.Instance.DisplayTip("Success", $"Coordinates to {moonSceneName} found", false);
            extendedLevel.IsRouteHidden = false;
            extendedLevel.IsRouteLocked = false;
        }
        else
        {
            HUDManager.Instance.DisplayTip("Error", $"Coordinates to {moonSceneName} could not be verified, Cancelling operation.", true);
        }
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(timeBeforeDespawning);
        // playerHeldBy.inSpecialInteractAnimation = false;
        // playerHeldBy.DespawnHeldObject();
    }
}