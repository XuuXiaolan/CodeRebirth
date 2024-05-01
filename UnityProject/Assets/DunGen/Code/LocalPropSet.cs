using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	public enum LocalPropSetCountMode
	{
		Random,
		DepthBased,
		DepthMultiply,
	}

	public delegate int GetPropCountDelegate(LocalPropSet propSet, RandomStream randomStream, Tile tile);

	[AddComponentMenu("DunGen/Random Props/Local Prop Set")]
	public class LocalPropSet : RandomProp
	{
		private static readonly Dictionary<LocalPropSetCountMode, GetPropCountDelegate> GetCountMethods = new Dictionary<LocalPropSetCountMode, GetPropCountDelegate>();

		[AcceptGameObjectTypes(GameObjectFilter.Scene)]
		public GameObjectChanceTable Props = new GameObjectChanceTable();
		public IntRange PropCount = new IntRange(1, 1);
		public LocalPropSetCountMode CountMode;
		public AnimationCurve CountDepthCurve = AnimationCurve.Linear(0, 0, 1, 1);


		public override void Process(RandomStream randomStream, Tile tile)
		{
			var propTable = Props.Clone();

			GetPropCountDelegate getCountDelegate;

			if (!GetCountMethods.TryGetValue(CountMode, out getCountDelegate))
				throw new NotImplementedException("LocalPropSet count mode \"" + CountMode + "\" is not yet implemented");

			int count = getCountDelegate(this, randomStream, tile);
			List<GameObject> toKeep = new List<GameObject>(count);

			for (int i = 0; i < count; i++)
			{
				var chosenEntry = propTable.GetRandom(randomStream, tile.Placement.IsOnMainPath, tile.Placement.NormalizedDepth, null, true, true, true);

				if (chosenEntry != null && chosenEntry.Value != null)
					toKeep.Add(chosenEntry.Value);
			}

			foreach (var prop in Props.Weights)
				if (!toKeep.Contains(prop.Value))
					UnityUtil.Destroy(prop.Value);
		}

		#region GetCount Methods

		static LocalPropSet()
		{
			GetCountMethods[LocalPropSetCountMode.Random] = GetCountRandom;
			GetCountMethods[LocalPropSetCountMode.DepthBased] = GetCountDepthBased;
			GetCountMethods[LocalPropSetCountMode.DepthMultiply] = GetCountDepthMultiply;
		}

		private static int GetCountRandom(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			int count = propSet.PropCount.GetRandom(randomStream);
			count = Mathf.Clamp(count, 0, propSet.Props.Weights.Count);

			return count;
		}

		private static int GetCountDepthBased(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			float curveValue = Mathf.Clamp(propSet.CountDepthCurve.Evaluate(tile.Placement.NormalizedPathDepth), 0, 1);
			int count = Mathf.RoundToInt(Mathf.Lerp(propSet.PropCount.Min, propSet.PropCount.Max, curveValue));

			return count;
		}

		private static int GetCountDepthMultiply(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			float curveValue = Mathf.Clamp(propSet.CountDepthCurve.Evaluate(tile.Placement.NormalizedPathDepth), 0, 1);
			int count = GetCountRandom(propSet, randomStream, tile);
			count = Mathf.RoundToInt(count * curveValue);

			return count;
		}

		#endregion
	}
}
