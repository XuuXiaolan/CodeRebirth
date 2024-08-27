using Unity.Netcode;

namespace CodeRebirth.src.Content.Unlockables;
public class ShockwaveGalAI : NetworkBehaviour
{
    public void Start() {
        Plugin.Logger.LogInfo("Hi creator");
    }
}