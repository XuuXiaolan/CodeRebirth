using System.Diagnostics;
using BepInEx.Logging;
using UnityEngine;

namespace CodeRebirth.Util.Extensions;

public static class ShovelExtensions {
	public static int CriticalHit(int force, System.Random random) {
		if (random.NextFloat(0f, 1f) < 0.5f) {
            Plugin.Logger.LogInfo("Critical Hit!");
            return force * 2;
        }
        return force;
	}
}