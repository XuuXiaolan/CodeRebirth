namespace DunGen.Adapters
{
	public abstract class NavMeshAdapter : BaseAdapter
	{
		#region Helpers

		public struct NavMeshGenerationProgress
		{
			public float Percentage;
			public string Description;
		}

		public delegate void OnNavMeshGenerationProgress(NavMeshGenerationProgress progress);

		#endregion

		public OnNavMeshGenerationProgress OnProgress;


		protected override void Run(DungeonGenerator generator)
		{
			Generate(generator.CurrentDungeon);
		}

		public abstract void Generate(Dungeon dungeon);
	}
}