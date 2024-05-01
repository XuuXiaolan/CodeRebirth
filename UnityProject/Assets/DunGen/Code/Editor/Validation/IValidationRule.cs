using DunGen.Graph;

namespace DunGen.Editor.Validation
{
	public interface IValidationRule
	{
		void Validate(DungeonFlow flow, DungeonValidator validator);
	}
}
