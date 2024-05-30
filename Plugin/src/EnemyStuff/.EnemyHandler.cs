﻿using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;

public class EnemyHandler : ContentHandler<EnemyHandler> {
	public class EnemyAssets(string bundleName) : AssetBundleLoader<EnemyAssets>(bundleName) {
		[LoadFromBundle("ButterflyObj.asset")]
		public EnemyType ButterflyEnemyType { get; private set; }
		
		[LoadFromBundle("CutieFlyTN.asset")]
		public TerminalNode ButterflyTerminalNode { get; private set; }
		
		[LoadFromBundle("CutieFlyTK.asset")]
		public TerminalKeyword ButterflyTerminalKeyword { get; private set; }
		
		[LoadFromBundle("SnailCatObj.asset")]
		public EnemyType SnailCatEnemyType { get; private set; }
		
		[LoadFromBundle("SnailCatTN.asset")]
		public TerminalNode SnailCatTerminalNode { get; private set; }
		
		[LoadFromBundle("SnailCatTK.asset")]
		public TerminalKeyword SnailCatTerminalKeyword { get; private set; }
		[LoadFromBundle("ScrapMasterObj.asset")]
		public EnemyType ScrapMasterEnemyType { get; private set; }
		
		[LoadFromBundle("ScrapMasterTN.asset")]
		public TerminalNode ScrapMasterTerminalNode { get; private set; }
		
		[LoadFromBundle("ScrapMasterTK.asset")]
		public TerminalKeyword ScrapMasterTerminalKeyword { get; private set; }
		[LoadFromBundle("GrapeObj")]
		public Item GrapeItem { get; private set; }
	}

	public EnemyAssets Assets { get; private set; }

	public EnemyHandler() {
		Assets = new EnemyAssets("coderebirthasset");
		Plugin.samplePrefabs.Add("QuestGrape", Assets.GrapeItem);

		// RegisterEnemyWithConfig(Plugin.ModConfig.ConfigScrapMasterEnabled.Value, Plugin.ModConfig.ConfigScrapMasterSpawnWeights.Value, Assets.ScrapMasterEnemyType, Assets.ScrapMasterTerminalNode, Assets.ScrapMasterTerminalKeyword, Plugin.ModConfig.ConfigScrapMasterPowerLevel.Value, Plugin.ModConfig.ConfigScrapMasterMaxSpawnCount.Value);
        RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlyEnabled.Value, Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Assets.ButterflyEnemyType, Assets.ButterflyTerminalNode, Assets.ButterflyTerminalKeyword, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatEnabled.Value, Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, Assets.SnailCatEnemyType, Assets.SnailCatTerminalNode, Assets.SnailCatTerminalKeyword, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
	}
}