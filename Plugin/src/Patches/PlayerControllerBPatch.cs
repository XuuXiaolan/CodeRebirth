using System;
using System.Collections.Generic;
using CodeRebirth.Util.PlayerManager;
using CodeRebirth.WeatherStuff;
using GameNetcodeStuff;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB))]
static class PlayerControllerBPatch {
    public static bool TornadoKinematicPatch = false;
    public static PlayerControllerB? TornadoKinematicPlayer = null;

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
    public static bool PlayerHitGroundEffects(PlayerControllerB __instance) {
        if (TornadoKinematicPatch && TornadoKinematicPlayer == __instance) {
            CodeRebirthPlayerManager.dataForPlayer[__instance].flingingAway = false;
            TornadoKinematicPatch = false;
            __instance.playerRigidbody.isKinematic = true;
            TornadoKinematicPlayer = null;
        }
        return true;
    }

	[HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayFootstepSound)), HarmonyPrefix]
	public static bool PlayFootstepSound(PlayerControllerB __instance) {
        if (__instance == GameNetworkManager.Instance.localPlayerController && CodeRebirthPlayerManager.dataForPlayer.ContainsKey(__instance) && CodeRebirthPlayerManager.dataForPlayer[__instance].ridingHoverboard) {
            return false;
        } else {
            return true;
        }
	}

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.Awake)), HarmonyPostfix]
    public static void Awake(PlayerControllerB __instance) {
        if (!CodeRebirthPlayerManager.dataForPlayer.ContainsKey(__instance)) {
            CodeRebirthPlayerManager.dataForPlayer.Add(__instance, new CRPlayerData());
            List<Collider> colliders = new List<Collider>(__instance.GetComponentsInChildren<Collider>());
            CodeRebirthPlayerManager.dataForPlayer[__instance].playerColliders = colliders;
        }
    }

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.Update)), HarmonyPrefix]
    public static void Update(PlayerControllerB __instance) {
        if (GameNetworkManager.Instance.localPlayerController == null) return;
        if (CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController != null) return;
        CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController = new AnimatorOverrideController(__instance.playerBodyAnimator.runtimeAnimatorController);
        __instance.playerBodyAnimator.runtimeAnimatorController = CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController; 
    }

    public static void Init() {
        IL.GameNetcodeStuff.PlayerControllerB.CheckConditionsForSinkingInQuicksand += PlayerControllerB_CheckConditionsForSinkingInQuicksand;
        IL.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject;
    }

    private static void PlayerControllerB_CheckConditionsForSinkingInQuicksand(ILContext il)
    {
        ILCursor c = new(il);
        ILLabel inSpecialInteractionLabel = null!;

        if (!c.TryGotoNext(MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<PlayerControllerB>(nameof(PlayerControllerB.thisController)),
            x => x.MatchCallvirt<CharacterController>("get_" + nameof(CharacterController.isGrounded)),
            x => x.MatchBrtrue(out inSpecialInteractionLabel)
        ))
        {
            Plugin.Logger.LogError("[ILHook:PlayerControllerB.CheckConditionsForSinkingInQuicksand] Couldn't find thisController.isGrounded check!");
            return;
        }

        c.EmitDelegate<Func<bool>>(() =>
        {
            bool skipIsGroundedCheck = true;
            return skipIsGroundedCheck;
        });
        c.Emit(OpCodes.Brtrue_S, inSpecialInteractionLabel);
    }

    private static void ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(0),                                   // load transform to stack
            x => x.MatchCallvirt<Component>(nameof(Component.GetComponent)),  // var component = transform.GetComponent<NetworkObject>();
            x => x.MatchStloc(3),                                   // if (component != null)
            x => x.MatchLdloc(3),
            x => x.MatchLdnull(),
            x => x.MatchCall<UnityEngine.Object>("op_Inequality"),
            x => x.MatchBrfalse(out _)
        ))
        {
            Plugin.Logger.LogError($"[{nameof(ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject)}] Could not match IL!");
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
    }
}