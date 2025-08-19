using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Weapons;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;
using CodeRebirthLib.Utils;

using GameNetcodeStuff;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public static bool PlayFootstepSound(PlayerControllerB __instance)
    {
        return !__instance.ContainsCRPlayerData() || !__instance.IsRidingHoverboard();
    }

    [HarmonyPatch(nameof(PlayerControllerB.Awake)), HarmonyPostfix]
    public static void Awake(PlayerControllerB __instance)
    {
        if (__instance.ContainsCRPlayerData()) return;

        __instance.AddCRPlayerData();
    }

    [HarmonyPatch(nameof(PlayerControllerB.PlayerJump), MethodType.Enumerator), HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RemoveJumpDelay(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        while (matcher.IsValid)
        {
            matcher.MatchForward(false,
                new CodeMatch(System.Reflection.Emit.OpCodes.Ldarg_0),
                new CodeMatch(System.Reflection.Emit.OpCodes.Ldc_R4),
                new CodeMatch(System.Reflection.Emit.OpCodes.Newobj, typeof(WaitForSeconds).GetConstructor([typeof(float)]))
            );
            if (matcher.IsInvalid)
                break;
            matcher.Advance(1);
            matcher.Set(System.Reflection.Emit.OpCodes.Nop, null);
            matcher.Insert(
                new CodeInstruction(System.Reflection.Emit.OpCodes.Call, typeof(PlayerControllerBPatch).GetMethod(nameof(JumpDelay), BindingFlags.Static | BindingFlags.NonPublic))
            );
        }
        return matcher.InstructionEnumeration();
    }

    static float JumpDelay()
    {
        if (SlowDownEffect.isSlowDownEffectActive)
        {
            return 0;
        }
        else
        {
            return 0.15f;
        }
    }

    public static void Init()
    {
        IL.GameNetcodeStuff.PlayerControllerB.CheckConditionsForSinkingInQuicksand += PlayerControllerB_CheckConditionsForSinkingInQuicksand;
        On.GameNetcodeStuff.PlayerControllerB.SetItemInElevator += PlayerControllerB_SetItemInElevator;
        On.GameNetcodeStuff.PlayerControllerB.Update += PlayerControllerB_Update;
        On.GameNetcodeStuff.PlayerControllerB.LateUpdate += PlayerControllerB_LateUpdate;
        On.GameNetcodeStuff.PlayerControllerB.IHittable_Hit += PlayerControllerB_IHittable_Hit;
        On.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += PlayerControllerB_DiscardHeldObject;
        On.GameNetcodeStuff.PlayerControllerB.Jump_performed += PlayerControllerB_Jump_performed;
        On.GameNetcodeStuff.PlayerControllerB.Interact_performed += PlayerControllerB_Interact_performed;
        On.GameNetcodeStuff.PlayerControllerB.StopHoldInteractionOnTrigger += PlayerControllerB_StopHoldInteractionOnTrigger;
    }

    private static void PlayerControllerB_SetItemInElevator(On.GameNetcodeStuff.PlayerControllerB.orig_SetItemInElevator orig, PlayerControllerB self, bool droppedInShipRoom, bool droppedInElevator, GrabbableObject gObject)
    {
        orig(self, droppedInElevator, droppedInElevator, gObject);
        if (gObject is WrittenDocument)
        {
            CRModContent.Achievements.TryDiscoverMoreProgressAchievement(NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "mu_miaolan"), gObject.itemProperties.itemName);
            return;
        }

        if (gObject is PlushieItem || gObject is Xui || gObject is GoldRigo)
        {
            CRModContent.Achievements.TryDiscoverMoreProgressAchievement(CodeRebirthAchievementKeys.HappyFamily, gObject.itemProperties.itemName);
            CRModContent.Achievements.TryDiscoverMoreProgressAchievement(CodeRebirthAchievementKeys.TheUprooted, gObject.itemProperties.itemName);
            CRModContent.Achievements.TryDiscoverMoreProgressAchievement(CodeRebirthAchievementKeys.HoardingBug, gObject.itemProperties.itemName);

            RoundManagerPatch.plushiesCollectedToday++;
            if (RoundManagerPatch.plushiesCollectedToday >= 3)
            {
                RoundManagerPatch.plushiesCollectedToday = 0;
                CRModContent.Achievements.TryTriggerAchievement(NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "scalper"));
            }
            return;
        }
    }

    private static void PlayerControllerB_Interact_performed(On.GameNetcodeStuff.PlayerControllerB.orig_Interact_performed orig, PlayerControllerB self, InputAction.CallbackContext context)
    {
        orig(self, context);
        if (!self.IsLocalPlayer())
            return;

        Plugin.ExtendedLogging($"{self.playerUsername} pressed interact.");
        CodeRebirthUtils.Instance.PlayerPressedInteract(self);
    }

    private static void PlayerControllerB_Jump_performed(On.GameNetcodeStuff.PlayerControllerB.orig_Jump_performed orig, PlayerControllerB self, InputAction.CallbackContext context)
    {
        orig(self, context);
        if (!self.IsLocalPlayer())
            return;

        Plugin.ExtendedLogging($"{self.playerUsername} pressed jump.");
        CodeRebirthUtils.Instance.PlayerPressedJump(self);
    }

    private static void PlayerControllerB_Update(On.GameNetcodeStuff.PlayerControllerB.orig_Update orig, PlayerControllerB self)
    {
        if (!self.IsLocalPlayer() && self.IsPseudoDead())
        {
            // Plugin.ExtendedLogging($"Setting player layer to 0.");
            self.gameObject.layer = 0;
        }

        orig(self);
        SlowDownEffect.SlowTrigger(self.hoveringOverTrigger);
    }

    private static void PlayerControllerB_StopHoldInteractionOnTrigger(On.GameNetcodeStuff.PlayerControllerB.orig_StopHoldInteractionOnTrigger orig, PlayerControllerB self)
    {
        if (SlowDownEffect.isSlowDownEffectActive)
        {
            if (self.previousHoveringOverTrigger != null)
                SlowDownEffect.ResetSlowTrigger(self.previousHoveringOverTrigger);

            if (self.hoveringOverTrigger != null)
                SlowDownEffect.ResetSlowTrigger(self.hoveringOverTrigger);
        }
        orig(self);
    }

    /*private static bool PlayerControllerB_NearOtherPlayers(On.GameNetcodeStuff.PlayerControllerB.orig_NearOtherPlayers orig, PlayerControllerB self, PlayerControllerB playerScript, float checkRadius)
    {
        if (self.IsLocalPlayer() && TalkingHead.talkingHeads.Count > 0 && TalkingHead.talkingHeads.Any(x => x.player == self)) return false;
        return orig(self, playerScript, checkRadius);
    }*/

    private static void PlayerControllerB_DiscardHeldObject(On.GameNetcodeStuff.PlayerControllerB.orig_DiscardHeldObject orig, PlayerControllerB self, bool placeObject, NetworkObject parentObjectTo, Vector3 placePosition, bool matchRotationOfParent)
    {
        orig(self, placeObject, parentObjectTo, placePosition, matchRotationOfParent);
        foreach (var janitor in Janitor.janitors)
        {
            if (janitor == null || janitor.isEnemyDead)
                continue;
            // If we’re still alive, chase that player if we’re not already
            if (self != null && NetworkManager.Singleton.IsServer && janitor.currentBehaviourStateIndex != (int)Janitor.JanitorStates.FollowingPlayer && janitor.currentBehaviourStateIndex != (int)Janitor.JanitorStates.ZoomingOff)
            {
                if (!janitor.currentlyGrabbingPlayer && !janitor.currentlyGrabbingScrap && !janitor.currentlyThrowingPlayer)
                {
                    janitor.DetectDroppedScrapServerRpc(self.transform.position, self);
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

        if (playerWhoHit != null)
        {
            foreach (var peaceKeeper in PeaceKeeper.Instances)
            {
                peaceKeeper.AlertPeaceKeeperToLocalPlayer(playerWhoHit);
            }
        }
        return orig(self, force, hitDirection, playerWhoHit, playHitSFX, hitID);
    }

    private static void PlayerControllerB_LateUpdate(On.GameNetcodeStuff.PlayerControllerB.orig_LateUpdate orig, PlayerControllerB self)
    {
        orig(self);
        if (!self.IsLocalPlayer() && self.IsPseudoDead())
        {
            // Plugin.ExtendedLogging($"Setting player layer to 0.");
            self.gameObject.layer = 0;
        }
        if (self.ContainsCRPlayerData() && ((self.currentlyHeldObjectServer != null && self.currentlyHeldObjectServer.itemProperties != null && !self.currentlyHeldObjectServer.itemProperties.requiresBattery) || (self.currentlyHeldObjectServer == null)))
        {
            Hoverboard? hoverboard = self.TryGetHoverboardRiding();
            if (hoverboard != null && hoverboard.playerControlling != null && hoverboard.playerControlling == self && self.IsLocalPlayer())
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
}