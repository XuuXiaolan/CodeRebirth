using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class GlitchedPlushie : NetworkBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public AudioSource plushieSource = null!;
    public List<AudioClip> audioClips = new();
    public InteractTrigger interactTrigger = null!;

    System.Random plushieRandom = new System.Random();
    public void Start()
    {
        plushieRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
        interactTrigger.onInteract.AddListener(ItemActivateTrigger);
    }

    public void ItemActivateTrigger(PlayerControllerB player)
    {
        if (player == null || player != GameNetworkManager.Instance.localPlayerController) return;
        PlayStuffServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayStuffServerRpc()
    {
        PlayStuffClientRpc();
    }

    [ClientRpc]
    private void PlayStuffClientRpc()
    {
        plushieSource.PlayOneShot(audioClips[plushieRandom.Next(0, audioClips.Count)]);
        StartCoroutine(ActivatePlushieCoroutine());
    }

    private IEnumerator ActivatePlushieCoroutine()
    {
        float totalDuration = 1f;
        float halfDuration = totalDuration / 2f;
        
        // First half: 0 -> 100
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(0f, 100f, t));
            yield return null;
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, 100f); // Ensure it hits exactly 100 at the midpoint
        
        // Second half: 100 -> 0
        for (float elapsed = 0f; elapsed < halfDuration; elapsed += Time.deltaTime)
        {
            float t = elapsed / halfDuration;
            skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Lerp(100f, 0f, t));
            yield return null;
        }
        skinnedMeshRenderer.SetBlendShapeWeight(0, 0f); // Ensure it returns to 0 at the end
    }
}