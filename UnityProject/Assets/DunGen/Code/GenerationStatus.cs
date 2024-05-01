using System;

namespace DunGen
{
	public delegate void GenerationStatusDelegate(DungeonGenerator generator, GenerationStatus status);
	
	public enum GenerationStatus
	{
		NotStarted = 0,
		PreProcessing,
		TileInjection,
		MainPath,
		Branching,
		BranchPruning,
		InstantiatingTiles,
		PostProcessing,
		Complete,
		Failed,
	}
}
