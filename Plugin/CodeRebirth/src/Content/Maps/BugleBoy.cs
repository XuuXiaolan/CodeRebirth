using System.Collections;
using CodeRebirth.src.Content.Unlockables;
using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class BugleBoy : MonoBehaviour
{
    [SerializeField]
    public int RerollPrice { get; private set; } = 2;
    [SerializeField]
    public Animator animator = null!;

    [SerializeField]
    public InteractTrigger rerollTrigger = null!;

    [SerializeField]
    public Merchant merchant = null!;

    [SerializeField]
    public AudioSource bugleSource = null!;

    [field: SerializeField]
    public AudioClip StartBugleSound { get; set; }

    [SerializeField]
    public AudioClip[] bugleClips = [];

    internal AudioClip chosenClip = null!;
    internal static readonly int ActivatedHash = Animator.StringToHash("MusicPlaying"); // Bool

    private bool disabled = false;

    public IEnumerator Start()
    {
        yield return null;
        yield return null;
        chosenClip = bugleClips[merchant.storeSeededRandom.Next(0, bugleClips.Length)];
        rerollTrigger.cooldownTime = chosenClip.length;
        rerollTrigger.onInteract.AddListener(Reroll);
        CoinDisplayUI.PointsOfInterest.Add(this.transform);

    }

    public void OnDestroy()
    {
        CoinDisplayUI.PointsOfInterest.Remove(this.transform);
        rerollTrigger.onInteract.RemoveListener(Reroll);
    }

    public void Update()
    {
        if (disabled)
        {
            return;
        }

        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() <= RerollPrice)
        {
            rerollTrigger.interactable = false;
        }
        else
        {
            rerollTrigger.interactable = true;
        }
    }

    public void DisableSelf()
    {
        rerollTrigger.onInteract.RemoveListener(Reroll);
        rerollTrigger.interactable = false;
        disabled = true;
    }

    public void Reroll(PlayerControllerB playerInteracting)
    {
        if (playerInteracting == null || !playerInteracting.IsLocalPlayer())
        {
            return;
        }

        merchant.RerollServerRpc();
    }

    public void PlayMusic()
    {
        bugleSource.clip = chosenClip;
        bugleSource.Play();
        StartCoroutine(StopHisSinging());
    }

    private IEnumerator StopHisSinging()
    {
        yield return new WaitUntil(() => !bugleSource.isPlaying);
        chosenClip = bugleClips[merchant.storeSeededRandom.Next(0, bugleClips.Length)];
        rerollTrigger.cooldownTime = chosenClip.length;
        animator.SetBool(ActivatedHash, false);
    }
}