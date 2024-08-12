using CodeRebirth.src.Content.Enemies;

namespace CodeRebirth.src.Util.Extensions;

// these really should just be handled by the enemy ai 
public static class EnemyAIExtensions {
	public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, SnailCatAI.State state)
	{
		enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
		Plugin.Logger.LogVerbose($"Switching to {state} State.");
	}

	public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, CutieFlyAI.State state)
	{
		enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
		Plugin.Logger.LogVerbose($"Switching to {state} State.");
	}

	public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, QuestMasterAI.State state)
	{
		enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
		Plugin.Logger.LogVerbose($"Switching to {state} State.");
	}
	
	public static void SwitchToBehaviourStateOnLocalClient(this EnemyAI enemyAI, PjonkGooseAI.State state)
	{
		enemyAI.SwitchToBehaviourStateOnLocalClient((int)state);
		Plugin.Logger.LogVerbose($"Switching to {state} State.");
	}
}