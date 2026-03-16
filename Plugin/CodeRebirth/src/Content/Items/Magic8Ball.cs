using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class Magic8Ball : GrabbableObject
{
    [field: SerializeField]
    public Animator Animator { get; private set; }

    [field: SerializeField]
    public SpriteRenderer SpriteRenderer { get; private set; }
    [field: SerializeField]
    public Sprite DefaultSprite { get; private set; }
    [field: SerializeField]
    public Sprite[] PossibleSprites { get; private set; } = [];

    private System.Random _eightBallRandom = null!;

    private static readonly int ShakeAnimationHash = Animator.StringToHash("shake"); // Trigger

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
        Animator.SetTrigger(ShakeAnimationHash);
    }

    public void DisplayMagic8BallResult()
    {
        Sprite chosenSprite = PossibleSprites[_eightBallRandom.Next(PossibleSprites.Length)];
        SpriteRenderer.sprite = chosenSprite;
    }
}