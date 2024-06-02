using CodeRebirth.EnemyStuff;

namespace CodeRebirth.Util.Extensions;

// this is cooked :sob:
public static class intExtensionMethods {
	public static SnailCatAI.State ToSnailState(this int index)
	{
		return (SnailCatAI.State)index;
	}

	public static CutieFlyAI.State ToCutieState(this int index)
	{
		return (CutieFlyAI.State)index;
	}

	public static QuestMasterAI.State ToQuestMasterAIState(this int index)
	{
		return (QuestMasterAI.State)index;
	} 
}