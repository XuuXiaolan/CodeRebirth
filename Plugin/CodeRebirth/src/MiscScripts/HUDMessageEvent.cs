using Dawn.Internal;
using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class HUDMessageEvent : MonoBehaviour
{
    public HUDDisplayTip tip;
    public bool playOnAwake = false;

    private void Awake()
    {
        if (playOnAwake)
        {
            PlayTip();
        }
    }

    public void PlayTip()
    {
        HUDManager.Instance.DisplayTip(tip);
    }

    public void PlayNetworkedTip()
    {
        DawnNetworker.Instance.BroadcastDisplayTipServerRpc(tip);
    }
}