﻿using System.Diagnostics;
using BepInEx.Logging;
using Debug = UnityEngine.Debug;

namespace CodeRebirth.Util.Extensions;

public static class ManualLogSourceExtensions {
	[Conditional("DEBUG")]
	public static void LogVerbose(this ManualLogSource logger, object data) {
		logger.LogInfo(data);
	}
}