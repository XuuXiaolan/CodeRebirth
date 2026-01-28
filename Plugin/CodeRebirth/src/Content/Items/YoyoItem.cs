using UnityEngine;

namespace CodeRebirth.src.Content.Items;

public class YoyoItem : GrabbableObject
{
    [field: SerializeField]
    public Animator YoyoAnimator { get; private set; } = null!;

    private System.Random random = new();

    private static readonly int Use1Hash = Animator.StringToHash("Use1"); // Trigger
    private static readonly int Use2Hash = Animator.StringToHash("Use2"); // Trigger
    private static readonly int Use3Hash = Animator.StringToHash("Use3"); // Trigger
    private static readonly int Use4Hash = Animator.StringToHash("Use4"); // Trigger

    private static readonly int IsHeldHash = Animator.StringToHash("IsHeld"); // Bool

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        int randomUse = random.Next(1, 5);
        switch (randomUse)
        {
            case 1:
                YoyoAnimator.SetTrigger(Use1Hash);
                break;
            case 2:
                YoyoAnimator.SetTrigger(Use2Hash);
                break;
            case 3:
                YoyoAnimator.SetTrigger(Use3Hash);
                break;
            case 4:
                YoyoAnimator.SetTrigger(Use4Hash);
                break;
        }
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        YoyoAnimator.SetBool(IsHeldHash, false);
    }

    public override void EquipItem()
    {
        base.EquipItem();
        YoyoAnimator.SetBool(IsHeldHash, true);
        random = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.allPlayerScripts.Length + 420 + 69);
    }
}