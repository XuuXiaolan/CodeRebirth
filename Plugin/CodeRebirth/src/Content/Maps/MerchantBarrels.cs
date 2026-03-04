using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using TMPro;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class MerchantBarrel : MonoBehaviour
{
    [SerializeField]
    public List<ItemWithRarityAndColor> itemNamesWithRarityAndColor = new();
    public TextMeshPro textMeshPro1 = null!;
    public TextMeshPro textMeshPro2 = null!;
    public Transform barrelSpawnPoint = null!;

    [HideInInspector]
    public List<RealItemWithRarityAndColor> validItemsWithRarityAndColor = new();
    [HideInInspector]
    public GrabbableObject? currentlySpawnedGrabbableObject = null;

    public void Start()
    {
        CoinDisplayUI.PointsOfInterest.Add(this.transform);
    }

    public void OnDestroy()
    {
        CoinDisplayUI.PointsOfInterest.Remove(this.transform);
    }
}