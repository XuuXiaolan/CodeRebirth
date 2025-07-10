using GameNetcodeStuff;

namespace CodeRebirth.src.Util.Extensions;
public static class PlayerControllerBExtensions
{
    public static bool IsLocalPlayer(this PlayerControllerB player)
    {
        return player.IsOwner;
    }
}