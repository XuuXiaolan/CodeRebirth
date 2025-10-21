using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using UnityEngine;

namespace CodeRebirth.src.Content.Skins;
public class WalletCandyReplacement : MonoBehaviour
{
    [field: SerializeField]
    public List<GameObject> CandyPrefabs { get; set; }

    private Wallet wallet = null!;

    public void Awake()
    {
        wallet = this.transform.parent.GetComponent<Wallet>();
        wallet.coinsStored.OnValueChanged += Refresh;
    }

    public void Refresh(int previousValue, int newValue)
    {
        int stepAmount = 100 / CandyPrefabs.Count;
        int amountToActivate = (newValue / stepAmount) * 5;
        foreach (GameObject candyPrefab in CandyPrefabs)
        {
            candyPrefab.SetActive(false);
        }

        for (int i = 0; i < amountToActivate; i++)
        {
            CandyPrefabs[i].SetActive(true);
        }
    }
}