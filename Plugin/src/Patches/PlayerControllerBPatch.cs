using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch {
    [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
    public static void PlayerHitGroundEffects(PlayerControllerB __instance) {
        if (!__instance.ContainsCRPlayerData()) return;
        if (!__instance.HasFlung()) return;
        __instance.SetFlung(false);
        __instance.playerRigidbody.isKinematic = true;
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

    public static void Init() {
        On.GameNetcodeStuff.PlayerControllerB.TeleportPlayer += PlayerControllerB_TeleportPlayer;
        On.GameNetcodeStuff.PlayerControllerB.DamagePlayer += PlayerControllerB_DamagePlayer;
        IL.GameNetcodeStuff.PlayerControllerB.CheckConditionsForSinkingInQuicksand += PlayerControllerB_CheckConditionsForSinkingInQuicksand;
        // IL.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += ILHookAllowParentingOnEnemy_PlayerControllerB_DiscardHeldObject;
        On.GameNetcodeStuff.PlayerControllerB.LateUpdate += PlayerControllerB_LateUpdate;
    }

    private static void PlayerControllerB_DamagePlayer(On.GameNetcodeStuff.PlayerControllerB.orig_DamagePlayer orig, PlayerControllerB self, int damageNumber, bool hasDamageSFX, bool callRPC, CauseOfDeath causeOfDeath, int deathAnimation, bool fallDamage, Vector3 force)
    {
        orig(self, damageNumber, hasDamageSFX, callRPC, causeOfDeath, deathAnimation, fallDamage, force);
        if (self.currentlyHeldObjectServer is ChildEnemyAI childEnemyAI)
        {
            self.StartCoroutine(self.waitToEndOfFrameToDiscard());
        }
    }

    private static void PlayerControllerB_TeleportPlayer(On.GameNetcodeStuff.PlayerControllerB.orig_TeleportPlayer orig, PlayerControllerB self, Vector3 pos, bool withRotation, float rot, bool allowInteractTrigger, bool enableController)
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is CodeRebirthEnemyAI codeRebirthEnemyAI)
            {
                Plugin.ExtendedLogging($"Setting codeRebirthEnemyAI.positionsOfPlayersBeforeTeleport[self] to {self.transform.position}");
                codeRebirthEnemyAI.positionsOfPlayersBeforeTeleport[self] = self.transform.position;
            }
        }
        foreach (ShockwaveGalAI gal in UnityEngine.Object.FindObjectsOfType<ShockwaveGalAI>())
        {
            if (self == gal.ownerPlayer)
            {
                Plugin.ExtendedLogging($"Setting gal.positionOfPlayerBeforeTeleport to {self.transform.position}");
                gal.positionOfPlayerBeforeTeleport = self.transform.position;
            }
        }
        orig(self, pos, withRotation, rot, allowInteractTrigger, enableController);
    }

    private static void PlayerControllerB_LateUpdate(On.GameNetcodeStuff.PlayerControllerB.orig_LateUpdate orig, PlayerControllerB self)
    {
        orig(self);
        if (self.ContainsCRPlayerData() && ((self.currentlyHeldObjectServer != null && self.currentlyHeldObjectServer.itemProperties != null && !self.currentlyHeldObjectServer.itemProperties.requiresBattery) || (self.currentlyHeldObjectServer == null)))
        {
            Hoverboard? hoverboard = self.TryGetHoverboardRiding();
            if (hoverboard != null && hoverboard.playerControlling != null && hoverboard.playerControlling == self && self == GameNetworkManager.Instance.localPlayerController) {
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