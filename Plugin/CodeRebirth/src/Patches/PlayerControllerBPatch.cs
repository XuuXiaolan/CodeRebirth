using System;
using System.Collections;
using System.Linq;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;
using static CodeRebirth.src.Content.Enemies.Janitor;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch
{
    [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
    public static void PlayerHitGroundEffects(PlayerControllerB __instance)
    {
        if (!__instance.ContainsCRPlayerData()) return;
        if (__instance.HasFlung())
        {
            Plugin.ExtendedLogging($"{__instance.playerUsername} is flinging away with fallvalue: {__instance.fallValue} and fallvalueuncapped: {__instance.fallValueUncapped}!");
            if (__instance.fallValueUncapped <= -50)
            {
                __instance.DamagePlayer(10, true, true, CauseOfDeath.Gravity, 0, false, default(Vector3));
            }
            __instance.fallValue = 0f;
            __instance.fallValueUncapped = 0f;
        }
        if (!__instance.IsFlingingAway() && __instance.HasFlung())
        {
            __instance.SetFlung(false);
        }
    }

	[HarmonyPatch(nameof(PlayerControllerB.PlayFootstepSound)), HarmonyPrefix]
	public static bool PlayFootstepSound(PlayerControllerB __instance) {
        return !__instance.ContainsCRPlayerData() || !__instance.IsRidingHoverboard();
    }

    [HarmonyPatch(nameof(PlayerControllerB.Awake)), HarmonyPostfix]
    public static void Awake(PlayerControllerB __instance) {
        if (__instance.ContainsCRPlayerData()) return;

        __instance.AddCRPlayerData();
    }

    public static void Init()
    {
        On.GameNetcodeStuff.PlayerControllerB.ConnectClientToPlayerObject += PlayerControllerB_ConnectClientToPlayerObject;
        IL.GameNetcodeStuff.PlayerControllerB.CheckConditionsForSinkingInQuicksand += PlayerControllerB_CheckConditionsForSinkingInQuicksand;
        // IL.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject;
        On.GameNetcodeStuff.PlayerControllerB.Update += PlayerControllerB_Update;
        On.GameNetcodeStuff.PlayerControllerB.LateUpdate += PlayerControllerB_LateUpdate;
        On.GameNetcodeStuff.PlayerControllerB.IHittable_Hit += PlayerControllerB_IHittable_Hit;
        On.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += PlayerControllerB_DiscardHeldObject;
        // On.GameNetcodeStuff.PlayerControllerB.NearOtherPlayers += PlayerControllerB_NearOtherPlayers;
    }

    private static void PlayerControllerB_Update(On.GameNetcodeStuff.PlayerControllerB.orig_Update orig, PlayerControllerB self)
    {
        if (self != GameNetworkManager.Instance.localPlayerController && self.IsPseudoDead())
        {
            // Plugin.ExtendedLogging($"Setting player layer to 0.");
            self.gameObject.layer = 0;
        }
        orig(self);
    }

    /*private static bool PlayerControllerB_NearOtherPlayers(On.GameNetcodeStuff.PlayerControllerB.orig_NearOtherPlayers orig, PlayerControllerB self, PlayerControllerB playerScript, float checkRadius)
    {
        if (self == GameNetworkManager.Instance.localPlayerController && TalkingHead.talkingHeads.Count > 0 && TalkingHead.talkingHeads.Any(x => x.player == self)) return false;
        return orig(self, playerScript, checkRadius);
    }*/

    private static void PlayerControllerB_DiscardHeldObject(On.GameNetcodeStuff.PlayerControllerB.orig_DiscardHeldObject orig, PlayerControllerB self, bool placeObject, NetworkObject parentObjectTo, Vector3 placePosition, bool matchRotationOfParent)
    {
        orig(self, placeObject, parentObjectTo, placePosition, matchRotationOfParent);
        foreach (var janitor in Janitor.janitors)
        {
            if (janitor == null || janitor.isEnemyDead) continue;
            // If we’re still alive, chase that player if we’re not already
            if (self != null && NetworkManager.Singleton.IsServer && janitor.currentBehaviourStateIndex != (int)JanitorStates.FollowingPlayer && janitor.currentBehaviourStateIndex != (int)JanitorStates.ZoomingOff)
            {
                if (!janitor.currentlyGrabbingPlayer && !janitor.currentlyGrabbingScrap && !janitor.currentlyThrowingPlayer)
                {
                    janitor.DetectDroppedScrapServerRpc(self.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, self));
                }
                else
                {
                    janitor.StartCoroutine(janitor.WaitUntilNotDoingAnythingCurrently(self));
                }
            }
        }
    }

    private static bool PlayerControllerB_IHittable_Hit(On.GameNetcodeStuff.PlayerControllerB.orig_IHittable_Hit orig, PlayerControllerB self, int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
    {
        if (playerWhoHit != null && playerWhoHit.currentlyHeldObjectServer != null && playerWhoHit.currentlyHeldObjectServer is ScaryShrimp scaryShrimp)
        {
            scaryShrimp.PastHitPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, self));
            self.DamagePlayerFromOtherClientServerRpc(60, hitDirection, (int)playerWhoHit.playerClientId);
            return false;
        }
        return orig(self, force, hitDirection, playerWhoHit, playHitSFX, hitID);
    }

    private static void PlayerControllerB_ConnectClientToPlayerObject(On.GameNetcodeStuff.PlayerControllerB.orig_ConnectClientToPlayerObject orig, PlayerControllerB self)
    {
        orig(self);
        Plugin.ExtendedLogging("PlayerControllerB_ConnectClientToPlayerObject called");
        if (GameNetworkManager.Instance.localPlayerController == self)
        {
            self.StartCoroutine(WaitToLoadUnlockableData(self));
        }
    }

    private static IEnumerator WaitToLoadUnlockableData(PlayerControllerB self)
    {
        yield return new WaitUntil(() => CodeRebirthUtils.Instance != null);
        CodeRebirthUtils.Instance.RequestProgressiveUnlocksFromHostServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, self), true, true, "Requesting Data", "Requesting unlockable information from host...");
    }

    private static void PlayerControllerB_LateUpdate(On.GameNetcodeStuff.PlayerControllerB.orig_LateUpdate orig, PlayerControllerB self)
    {
        orig(self);
        if (self != GameNetworkManager.Instance.localPlayerController && self.IsPseudoDead())
        {
            // Plugin.ExtendedLogging($"Setting player layer to 0.");
            self.gameObject.layer = 0;
        }
        if (self.ContainsCRPlayerData() && ((self.currentlyHeldObjectServer != null && self.currentlyHeldObjectServer.itemProperties != null && !self.currentlyHeldObjectServer.itemProperties.requiresBattery) || (self.currentlyHeldObjectServer == null)))
        {
            Hoverboard? hoverboard = self.TryGetHoverboardRiding();
            if (hoverboard != null && hoverboard.playerControlling != null && hoverboard.playerControlling == self && self == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.batteryMeter.fillAmount = hoverboard.insertedBattery.charge / 1.3f;
                HUDManager.Instance.batteryMeter.gameObject.SetActive(true);
                HUDManager.Instance.batteryIcon.enabled = true;
                var num4 = HUDManager.Instance.batteryMeter.fillAmount;
                HUDManager.Instance.batteryBlinkUI.SetBool("blink", num4 < 0.2f && num4 > 0f);
            }
        }
    }

    private static void PlayerControllerB_CheckConditionsForSinkingInQuicksand(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<PlayerControllerB>(nameof(PlayerControllerB.thisController)),
            x => x.MatchCallvirt<CharacterController>("get_" + nameof(CharacterController.isGrounded)),
            x => x.MatchBrtrue(out _)
        ))
        {
            Plugin.Logger.LogError("[ILHook:PlayerControllerB.CheckConditionsForSinkingInQuicksand] Couldn't find thisController.isGrounded check!");
            return;
        }

        c.Index -= 1;
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((bool isGrounded, PlayerControllerB self) =>
        {
            // Pretend we are grounded when in a water tornado so we can drown.
            return isGrounded || self.HasEffectActive(CodeRebirthStatusEffects.Water);
        });
    }

    /// <summary>
    /// Modifies item dropping code to attempt to find a NetworkObject from collided object's parents
    /// in case the collided object itself doesn't have a NetworkObject, if the following is true:<br/>
    /// - The collided GameObject's name ends with <c>"_RedirectToRootNetworkObject"</c><br/>
    /// <br/>
    /// This is necessary for parenting items to enemies, because the raycast that collides with an object
    /// ignores the enemies layer.
    /// </summary>
    /*private static void ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(0),                                   // load transform to stack
            x => x.MatchCallvirt<Component>(nameof(Component.GetComponent)), // var component = transform.GetComponent<NetworkObject>();
            x => x.MatchStloc(3)
            // Context:
            // x => x.MatchLdloc(3),                                // if (component != null)
            // x => x.MatchLdnull(),
            // x => x.MatchCall<UnityEngine.Object>("op_Inequality"),
            // x => x.MatchBrfalse(out _)
        ))
        {
            // Couldn't match, let's figure out if we should worry
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchLdcI4(out _),   // Matching against EmitDelegate
                x => x.MatchCall("MonoMod.Cil.RuntimeILReferenceBag/InnerBag`1<System.Func`2<UnityEngine.Transform,UnityEngine.Transform>>", "Get"), // There exists some bug probably that typeof doesn't work here because of generics?
                x => x.MatchCall(typeof(RuntimeILReferenceBag.FastDelegateInvokers), "Invoke"),
                x => x.MatchCallvirt<Component>(nameof(Component.GetComponent)),
                x => x.MatchStloc(3)
            ))
                Plugin.Logger.LogInfo($"[{nameof(ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject)}] This ILHook has most likely already been applied by another mod.");
            else
                Plugin.Logger.LogError($"[{nameof(ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject)}] Could not match IL!!");
            return;
        }

        c.Index += 1;
        c.EmitDelegate<Func<Transform, Transform>>(transform =>
        {
            if (!transform.name.EndsWith("_RedirectToRootNetworkObject"))
                return transform;

            return TryFindRoot(transform) ?? transform;
        });

    }
    public static Transform? TryFindRoot(Transform child)
    {
        // iterate upwards until we find a NetworkObject
        Transform current = child;
        while (current != null)
        {
            if (current.GetComponent<NetworkObject>() != null)
            {
                return current;
            }
            current = current.transform.parent;
        }
        return null;
    }*/
}