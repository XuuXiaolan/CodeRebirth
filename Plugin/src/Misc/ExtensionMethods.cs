﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CodeRebirth.EnemyStuff;
using MonoMod.Cil;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.Misc;

public static class ExtensionMethods
{
    [Conditional("DEBUG")]
    static void LogIfDebugBuild(string text)
    {
        Plugin.Logger.LogInfo(text);
    }
    public static IEnumerator WaitUntilSpawned(this NetworkObject networkObject) {
		yield return new WaitUntil(() => networkObject.IsSpawned);
	}

	static IEnumerator RunActionAfterSpawned(NetworkObject networkObject, Action action) {
		yield return networkObject.WaitUntilSpawned();
		action();
	}
	
	public static void OnSpawn(this NetworkObject networkObject, Action action) {
		networkObject.StartCoroutine(RunActionAfterSpawned(networkObject, action));
	}
	
	internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly) {
		if(assembly == null) {
			throw new ArgumentNullException(nameof(assembly));
		}

		try {
			return assembly.GetTypes();
		} catch(ReflectionTypeLoadException ex) {
			return ex.Types.Where(t => t != null);
		}
	}
	
	public static T NextEnum<T>(this Random random) where T : struct, Enum
	{
		Array values = Enum.GetValues(typeof(T));
		return (T)values.GetValue(random.Next(values.Length));
	}

	public static T NextItem<T>(this Random random, List<T> collection) {
		int index = random.Next(collection.Count);
		Plugin.Logger.LogDebug($"indexing randomly at {index}");
		return collection[index];
	}

	public static double NextDouble(this Random random, double min, double max) {
		return (random.NextDouble() * (max - min)) + min;
	}

	public static float NextFloat(this Random random, float min, float max) {
		return (float)random.NextDouble(min, max);
	}

	public static bool NextBool(this Random random) {
		return random.Next(0, 2) == 0;
	}

	public static int NextSign(this Random random) {
		return random.NextBool() ? 1 : -1;
	}

	public static Quaternion NextQuaternion(this Random random) {
		return quaternion.Euler(random.NextFloat(0f, 360f),random.NextFloat(0f, 360f),random.NextFloat(0f, 360f));
	}

	public static string ToAnimationName(this Duck.Animations animation)
	{
		return nameof(animation);
	}

    public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, SnailCatAI.State state)
    {
        enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
        LogIfDebugBuild($"Switching to {state} State.");
    }

    public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, CutieFlyAI.State state)
	{
		enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
        LogIfDebugBuild($"Switching to {state} State.");
    }

    public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, Duck.State state)
    {
        enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
        LogIfDebugBuild($"Switching to {state} State.");
    }
    public static void SwitchToBehaviourClientRpc(this EnemyAI enemyAI, Duck.State state)
    {
        enemyAI.SwitchToBehaviourClientRpc((int)state);
        LogIfDebugBuild($"Switching to {state} State.");
    }

    public static SnailCatAI.State ToSnailState(this int index)
	{
		return (SnailCatAI.State)index;
	}

	public static CutieFlyAI.State ToCutieState(this int index)
	{
		return (CutieFlyAI.State)index;
	}

	public static Duck.State ToDuckState(this int index)
	{
		return (Duck.State)index;
	}
}