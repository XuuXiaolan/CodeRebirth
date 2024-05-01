using UnityEngine;

namespace DunGen.Adapters
{
	public abstract class BaseAdapter : MonoBehaviour
	{
		public int Priority = 0;
		public virtual bool RunDuringAnalysis { get; set; }

		protected DungeonGenerator dungeonGenerator;


		protected virtual void OnEnable()
		{
			var runtimeDungeon = GetComponent<RuntimeDungeon>();

			if (runtimeDungeon != null)
			{
				dungeonGenerator = runtimeDungeon.Generator;
				dungeonGenerator.RegisterPostProcessStep(OnPostProcess, Priority);
				dungeonGenerator.Cleared += Clear;
			}
			else
				Debug.LogError("[DunGen Adapter] RuntimeDungeon component is missing on GameObject '" + gameObject.name + "'. Adapters must be attached to the same GameObject as your RuntimeDungeon component");
		}

		protected virtual void OnDisable()
		{
			if (dungeonGenerator != null)
			{
				dungeonGenerator.UnregisterPostProcessStep(OnPostProcess);
				dungeonGenerator.Cleared -= Clear;
			}
		}

		private void OnPostProcess(DungeonGenerator generator)
		{
			if (!generator.IsAnalysis || RunDuringAnalysis)
				Run(generator);
		}

		protected virtual void Clear() { }
		protected abstract void Run(DungeonGenerator generator);
	}
}
