using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeRebirth.src.MiscScripts;
public class ItemUI : MonoBehaviour
{
    public RectTransform uiElement = null!;
    public float defaultScale = 0.2f;
    [SerializeField] private TextMeshProUGUI itemName = null!;
    [SerializeField] private Image itemImage = null!;
    [SerializeField] private CanvasGroup canvasGroup = null!;

    [HideInInspector] public Camera mainCamera = null!;
    private PhysicsProp? parentItem = null;

    public void StartUIforItem(PhysicsProp item)
    {
        parentItem = item;

        itemImage.sprite = item.itemProperties.itemIcon;
        itemName.text = item.itemProperties.itemName;
        mainCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;

    }

    public void Update()
    {
        if (parentItem == null) 
        {
            canvasGroup.alpha = 0f;
            return; 
        }

        itemImage.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.45f, 0.5f, Mathf.Sin(Time.time * 4.0f) * 0.5f + 0.5f);

        float distance = Vector3.Distance(mainCamera.transform.position, parentItem.transform.position);
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(parentItem.transform.position);
        if (screenPosition.z > 0 && distance > 1f)
        {
            RectTransform canvasRect = uiElement.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            Vector2 viewportPosition = mainCamera.ScreenToViewportPoint(screenPosition);
            Vector2 canvasSize = canvasRect.sizeDelta;

            Vector2 anchoredPosition = new(
                viewportPosition.x * canvasSize.x - canvasSize.x * 0.5f,
                viewportPosition.y * canvasSize.y - canvasSize.y * 0.5f
            );

            uiElement.anchoredPosition = anchoredPosition;

            float scale = Mathf.Clamp01(distance * defaultScale);
            uiElement.localScale = new Vector3(scale, scale, scale);
            canvasGroup.alpha = 1f;
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }
}