using CodeRebirth.src.Content.Items;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class SoccerBallPropPatch
{
    public static void Init()
    {
        On.SoccerBallProp.KickBallLocalClient += SoccerBallProp_KickBallLocalClient;
    }

    private static void SoccerBallProp_KickBallLocalClient(On.SoccerBallProp.orig_KickBallLocalClient orig, SoccerBallProp self, Vector3 destinationPos, bool setInElevator, bool setInShipRoom)
    {
        orig(self, destinationPos, setInElevator, setInShipRoom);
        if (self is not JimBall jimBall || !self.IsOwner)
            return;

        jimBall._animator.SetBool(JimBall.KickingAnimation, true);
    }
}