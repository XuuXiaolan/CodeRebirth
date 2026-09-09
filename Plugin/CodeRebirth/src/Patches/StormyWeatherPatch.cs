using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Dawn.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace CodeRebirth.src.Patches;

static class StormyWeatherPatch
{
    private static Transform? _lastTarget = null;
    internal static void Init()
    {
        IL.StormyWeather.LightningStrikeRandom += AddLightningTargetsToPossibleNodes;
        On.StormyWeather.BeginDay += AddLightningTargetsToPossibleNodes;
        On.StormyWeather.PlayThunderEffects += InvokeLightningStrike;
    }

    private static void InvokeLightningStrike(On.StormyWeather.orig_PlayThunderEffects orig, StormyWeather self, Vector3 strikePosition, AudioSource audio)
    {
        if (_lastTarget != null && _lastTarget.TryGetComponent(out LightningTarget lightningTarget))
        {
            lightningTarget.OnLightningStrike.Invoke();
        }
        orig(self, strikePosition, audio);
    }

    private static void AddLightningTargetsToPossibleNodes(On.StormyWeather.orig_BeginDay orig, StormyWeather self)
    {
        orig(self);
        List<GameObject> newList = self.outsideNodes.ToList();
        newList.AddRange(GameObject.FindObjectsOfType<LightningTarget>().Select(x => x.gameObject));
        newList.ToArray();
    }

    private static void AddLightningTargetsToPossibleNodes(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(
            MoveType.Before,
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.seed)),
            il => il.MatchLdcI4(0),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes)),
            il => il.MatchLdlen(),
            il => il.MatchConvI4(),
            il => il.MatchCallvirt(out _),
            il => il.MatchStloc(1)
        ))
        {
            Plugin.Logger.LogWarning($"Could not match StormyWeather.LightningStrikeRandom (1), Meaning bear traps will not be targetted for lightning strikes.");
            return;
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes));
        cursor.EmitDelegate((GameObject[] outsideNodes) =>
        {
            List<GameObject> newList = outsideNodes.ToList();
            newList.AddRange(GameObject.FindObjectsOfType<LightningTarget>().Select(x => x.gameObject));
            newList.ToArray();
        });

        if (!cursor.TryGotoNext(
            MoveType.After,
            il => il.MatchCall(out _),
            il => il.MatchLdloc(0),
            il => il.MatchLdcR4(15),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.navHit)),
            il => il.MatchLdarg(0),
            il => il.MatchLdfld<StormyWeather>(nameof(StormyWeather.seed)),
            il => il.MatchLdcI4(out _),
            il => il.MatchLdcR4(1),
            il => il.MatchCallvirt(out _),
            il => il.MatchStloc(0)
        ))
        {
            Plugin.Logger.LogWarning($"Could not match StormyWeather.LightningStrikeRandom (2), Meaning bear traps will not be properly targetted for lightning strikes.");
            return;
        }

        cursor.Emit(OpCodes.Ldloc_1);
        cursor.Emit(OpCodes.Ldloc_0);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitLdfld<StormyWeather>(nameof(StormyWeather.outsideNodes));
        cursor.EmitDelegate((int index, Vector3 position, GameObject[] outsideNodes) =>
        {
            _lastTarget = outsideNodes[index].transform;
            if (!outsideNodes[index].TryGetComponent(out LightningTarget lightningTarget))
            {
                return;
            }

            position = lightningTarget.LightningStrikePoint.position;

            Plugin.ExtendedLogging($"Lightning strike redirected to hit {outsideNodes[index].name} at {position}");
        });
    }
}