using CodeRebirth.Util;
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
	}

	public EnemyAssets Assets { get; private set; }

	public EnemyHandler() {
		Assets = new EnemyAssets("coderebirthasset");
		
		Enemies.RegisterEnemy(Assets.ButterflyEnemyType, 100, Levels.LevelTypes.All, Assets.ButterflyTerminalNode, Assets.ButterflyTerminalKeyword);
		Enemies.RegisterEnemy(Assets.SnailCatEnemyType, 100, Levels.LevelTypes.All, Assets.SnailCatTerminalNode, Assets.SnailCatTerminalKeyword);
	}
}