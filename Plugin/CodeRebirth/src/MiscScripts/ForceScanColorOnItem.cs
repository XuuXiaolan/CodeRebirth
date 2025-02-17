using UnityEngine;
using UnityEngine.UI;

namespace CodeRebirth.src.MiscScripts;
public class ForceScanColorOnItem : MonoBehaviour
{
    public GrabbableObject grabbableObject = null!;
    public Color color = Color.green;

    private ScanNodeProperties? scanNodeProperties = null;

    public void Start()
    {
        FindGrabbableObjectsScanObject();
    }

    public void FindGrabbableObjectsScanObject()
    {
        if (grabbableObject == null) return;
        scanNodeProperties = grabbableObject.GetComponentInChildren<ScanNodeProperties>();
    }

    public void LateUpdate()
    {
        if (scanNodeProperties == null) return;
        RectTransform? rectTransformOfImportance = null;
        if (!HUDManager.Instance.scanNodes.ContainsValue(scanNodeProperties)) return;
        foreach (var (key, value) in HUDManager.Instance.scanNodes)
        {
            if (value == scanNodeProperties)
            {
                rectTransformOfImportance = key;
                Plugin.ExtendedLogging($"Found scan node's gameobject: {key}");
            }
        }
        if (rectTransformOfImportance == null) return;
        Image? image1 = rectTransformOfImportance.transform.GetChild(1).GetChild(0).GetComponent<Image>();
        Image? image2 = rectTransformOfImportance.transform.GetChild(1).GetChild(2).GetComponent<Image>();
        if (image1 == null || image2 == null) return;
        image1.color = color;
        image2.color = color;
    }
}