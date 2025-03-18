namespace CodeRebirth.src.Util.AssetLoading;

public interface IEnemyAssets {
	EnemyType EnemyType { get; }
	TerminalNode EnemyTerminalNode { get; }
	TerminalKeyword EnemyTerminalKeyword { get; }
}