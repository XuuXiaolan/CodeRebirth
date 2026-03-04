using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class AttachToPlayerHUD : MonoBehaviour
{
    [field: SerializeField]
    public Vector3 Position { get; private set; }
    [field: SerializeField]
    public Vector2 WidthHeight { get; private set; }

    private void Start()
    {
        ((RectTransform)transform).SetParent(GameNetworkManager.Instance.localPlayerController.playerHudUIContainer, false);
        ((RectTransform)transform).anchoredPosition3D = Position;
        ((RectTransform)transform).sizeDelta = WidthHeight;
    }
}