using System;

namespace CodeRebirth.Util.Extensions;

public static class ShovelExtensions {
	public static int CriticalHit(int force, System.Random random, int critChance) {
		if (random.Next(0, 100) < Math.Clamp(critChance, 0, 99)) {
            Plugin.Logger.LogInfo("Critical Hit!");
            return force * 2;
        }
        return force;
	}
}