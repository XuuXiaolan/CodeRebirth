using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Magic8Ball : GrabbableObject
{
    [field: SerializeField]
    public float TimeToDisplayResult { get; private set; } = 1.167f;
    [field: SerializeField]
    public AudioSource AudioSource { get; private set; }
    [field: SerializeField]
    public AudioClip ShakeSound { get; private set; }
    [field: SerializeField]
    public SpriteRenderer SpriteRenderer { get; private set; }
    [field: SerializeField]
    public Sprite DefaultSprite { get; private set; }
    [field: SerializeField]
    public Sprite[] PossibleSprites { get; private set; } = [];

    private System.Random _eightBallRandom = null!;
    private Coroutine? _displayRoutine = null;

    private static readonly int ShakeItemAnimationHash = Animator.StringToHash("shakeItem"); // Trigger

    public override void Start()
    {
        base.Start();
        _eightBallRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 323);
        SpriteRenderer.sprite = DefaultSprite;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        SpriteRenderer.sprite = DefaultSprite;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        SpriteRenderer.sprite = DefaultSprite;
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (_displayRoutine != null)
        {
            return;
        }

        AudioSource.PlayOneShot(ShakeSound);
        playerHeldBy.playerBodyAnimator.SetTrigger(ShakeItemAnimationHash);
        _displayRoutine = StartCoroutine(WaitForAnimationEnd());
    }

    private IEnumerator WaitForAnimationEnd()
    {
        SpriteRenderer.sprite = DefaultSprite;
        yield return new WaitForSeconds(TimeToDisplayResult);
        SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, 0f);
        DisplayMagic8BallResult();

        float duration = 0;
        while (duration < 1)
        {
            SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, duration);
            duration += Time.deltaTime * 4f;
            yield return null;
        }

        SpriteRenderer.color = new Color(SpriteRenderer.color.r, SpriteRenderer.color.g, SpriteRenderer.color.b, 1f);
        _displayRoutine = null;
    }

    public void DisplayMagic8BallResult()
    {
        Sprite chosenSprite = PossibleSprites[_eightBallRandom.Next(PossibleSprites.Length)];
        SpriteRenderer.sprite = chosenSprite;
    }
}