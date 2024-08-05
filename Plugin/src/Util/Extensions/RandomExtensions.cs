using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.Util.Extensions;

public static class RandomExtensions {
	public static T NextEnum<T>(this Random random) where T : struct, Enum
	{
		Array values = Enum.GetValues(typeof(T));
		return (T)values.GetValue(random.Next(values.Length));
	}

	public static T NextItem<T>(this Random random, List<T> collection) {
		int index = random.Next(collection.Count);
		return collection[index];
	}

	public static double NextDouble(this Random random, double min, double max) {
		return (random.NextDouble() * (max - min)) + min;
	}

	public static float NextFloat(this Random random, float min, float max) {
		return (float)random.NextDouble(min, max);
	}

	public static int NextInt(this Random random, int min, int max) {
		return random.Next(min, max+1);
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
}