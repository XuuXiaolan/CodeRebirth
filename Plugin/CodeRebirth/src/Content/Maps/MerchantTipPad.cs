using CodeRebirth.src.Content.Unlockables;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class MerchantTipPad : NetworkBehaviour
{
    [field: SerializeField]
    public InteractTrigger[] TipTriggers { get; private set; } = [];
    [field: SerializeField]
    public InteractTrigger TipJarTrigger { get; private set; }
    [field: SerializeField]
    public GameObject[] ObjectsToEnableOnDonationAmounts { get; private set; } = [];
    [field: SerializeField]
    public AudioSource TippingSource { get; private set; }
    [field: SerializeField]
    public AudioClip TippingSound { get; private set; }

    private bool shopClosed = false;
    public static int _tippedAmount = 0;

    internal static void Init()
    {
        IL.TimeOfDay.SetNewProfitQuota += UseTippedAmountToInfluenceQuota;
        On.TimeOfDay.SetNewProfitQuota += ResetTippedAmount;
    }

    private static void ResetTippedAmount(On.TimeOfDay.orig_SetNewProfitQuota orig, TimeOfDay self)
    {
        _tippedAmount = Mathf.Clamp(_tippedAmount, 0, 100);
        orig(self);
        _tippedAmount = 0;
    }

    private static void UseTippedAmountToInfluenceQuota(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(MoveType.After,
            cursor => cursor.MatchLdstr("Randomizer amount after: {0}"),
            cursor => cursor.MatchLdloc(1),
            cursor => cursor.MatchBox<System.Single>(),
            cursor => cursor.MatchCall<System.String>("Format"),
            cursor => cursor.MatchCall<UnityEngine.Debug>("Log")
        ))
        {
            Plugin.Logger.LogWarning($"Couldn't find the original code for TimeOfDay.SetNewProfitQuota, skipping IL patching.");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_1);
        cursor.Emit(OpCodes.Ldc_R4, 1f);
        cursor.Emit(OpCodes.Ldc_R4, 1.3f);
        cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(MerchantTipPad), nameof(GetTippedAmount)));
        cursor.Emit(OpCodes.Ldc_R4, 100f);
        cursor.Emit(OpCodes.Div);
        cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), [typeof(float), typeof(float), typeof(float)]));
        cursor.Emit(OpCodes.Div);
        cursor.Emit(OpCodes.Stloc_1);
    }

    public void Awake()
    {
        foreach (InteractTrigger trigger in TipTriggers)
        {
            trigger.interactable = false;
        }
        ToggleDonationObjects();
    }

    public void CloseDonations()
    {
        shopClosed = true;
        foreach (InteractTrigger trigger in TipTriggers)
        {
            trigger.interactable = false;
            trigger.disabledHoverTip = "Shop closed";
        }
    }

    public void Update()
    {
        if (MoneyCounter.Instance == null || shopClosed)
        {
            return;
        }

        TipTriggers[0].interactable = MoneyCounter.Instance.MoneyStored() >= 1;
        TipTriggers[1].interactable = MoneyCounter.Instance.MoneyStored() >= 5;
        TipTriggers[2].interactable = MoneyCounter.Instance.MoneyStored() >= 10;
    }

    public void ToggleDonationObjects()
    {
        TippingSource.PlayOneShot(TippingSound);
        if (_tippedAmount <= 24)
        {
            ObjectsToEnableOnDonationAmounts[0].SetActive(true);
        }

        if (_tippedAmount >= 25)
        {
            ObjectsToEnableOnDonationAmounts[1].SetActive(true);
        }

        if (_tippedAmount >= 50)
        {
            ObjectsToEnableOnDonationAmounts[2].SetActive(true);
        }

        if (_tippedAmount >= 75)
        {
            ObjectsToEnableOnDonationAmounts[3].SetActive(true);
        }

        TipJarTrigger.disabledHoverTip = $"Coins Tipped: {_tippedAmount}";
    }

    public void Tip1Coin()
    {
        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() < 1)
        {
            return;
        }

        if (IsServer)
        {
            MoneyCounter.Instance.RemoveMoney(1);
        }
        _tippedAmount += 1;
        ToggleDonationObjects();
    }

    public void Tip5Coins()
    {
        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() < 5)
        {
            return;
        }

        if (IsServer)
        {
            MoneyCounter.Instance.RemoveMoney(5);
        }
        _tippedAmount += 5;
        ToggleDonationObjects();
    }

    public void Tip10Coins()
    {
        if (MoneyCounter.Instance == null || MoneyCounter.Instance.MoneyStored() < 10)
        {
            return;
        }

        if (IsServer)
        {
            MoneyCounter.Instance.RemoveMoney(10);
        }
        _tippedAmount += 10;
        ToggleDonationObjects();
    }

    public static float GetTippedAmount()
    {
        return _tippedAmount;
    }
}