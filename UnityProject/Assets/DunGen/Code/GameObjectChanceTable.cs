using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace DunGen
{
	/**
	 * Lots of code rewriting since Unity doesn't support serializing generics
	 */

	#region Helper Class

	[Serializable]
	public sealed class GameObjectChance
	{
		public GameObject Value = null;
		public float MainPathWeight = 1f;
		public float BranchPathWeight = 1f;
		public AnimationCurve DepthWeightScale = AnimationCurve.Linear(0, 1, 1, 1);

		public TileSet TileSet; // Only used at runtime - should probably move this elsewhere


		public GameObjectChance()
			: this(null, 1, 1, null)
		{
		}

		public GameObjectChance(GameObject value)
			: this(value, 1, 1, null)
		{
		}

		public GameObjectChance(GameObject value, float mainPathWeight, float branchPathWeight, TileSet tileSet)
		{
			Value = value;
			MainPathWeight = mainPathWeight;
			BranchPathWeight = branchPathWeight;
			TileSet = tileSet;
		}

		public float GetWeight(bool isOnMainPath, float normalizedDepth)
		{
			float weight = (isOnMainPath) ? MainPathWeight : BranchPathWeight;
			weight *= DepthWeightScale.Evaluate(normalizedDepth);

			return weight;
		}
	}

	#endregion

	/// <summary>
	/// A table containing weighted values to be picked at random
	/// </summary>
	[Serializable]
	public class GameObjectChanceTable
	{
		public List<GameObjectChance> Weights = new List<GameObjectChance>();


		public GameObjectChanceTable Clone()
		{
			GameObjectChanceTable newTable = new GameObjectChanceTable();

			foreach (var w in Weights)
				newTable.Weights.Add(new GameObjectChance(w.Value, w.MainPathWeight, w.BranchPathWeight, w.TileSet) { DepthWeightScale = w.DepthWeightScale });

			return newTable;
		}

		/// <summary>
		/// Is there at least one non-null value in the table?
		/// </summary>
		public bool HasAnyValidEntries()
		{
			bool hasValidEntries = false;

			foreach (var entry in Weights)
			{
				if (entry.Value != null)
				{
					hasValidEntries = true;
					break;
				}
			}

			return hasValidEntries;
		}

		/// <summary>
		/// Is there at least one non-null value in the table, given the appropriate input settings?
		/// </summary>
		/// <param name="isOnMainPath"></param>
		/// <param name="normalizedDepth"></param>
		/// <param name="previouslyChosen"></param>
		/// <param name="allowImmediateRepeats"></param>
		/// <param name="allowNullSelection"></param>
		/// <returns></returns>
		public bool HasAnyValidEntries(bool isOnMainPath, float normalizedDepth, GameObject previouslyChosen, bool allowImmediateRepeats, bool allowNullSelection = false)
		{
			if (allowNullSelection)
				return true;

			bool hasValidEntries = false;

			foreach (var entry in Weights)
			{
				if (entry.Value != null)
				{
					float weight = entry.GetWeight(isOnMainPath, normalizedDepth);

					if (weight > 0f && (allowImmediateRepeats || previouslyChosen != entry.Value))
					{
						hasValidEntries = true;
						break;
					}
				}
			}

			return hasValidEntries;
		}

		/// <summary>
		/// Does this chance table contain the specified GameObject?
		/// </summary>
		/// <param name="obj">The object to check</param>
		/// <returns>True if the GameObject is included in the chance table</returns>
		public bool ContainsGameObject(GameObject obj)
		{
			foreach (var weight in Weights)
				if (weight.Value == obj)
					return true;

			return false;
		}

		/// <summary>
		/// Picks an object from the table at random, taking weights into account
		/// </summary>
		/// <param name="random">The random number generator to use</param>
		/// <param name="isOnMainPath">Is this object to be spawn on the main path</param>
		/// <param name="normalizedDepth">The normalized depth (0-1) that this object is to be spawned at in the dungeon</param>
		/// <returns>A random value</returns>
		public GameObjectChance GetRandom(RandomStream random, bool isOnMainPath, float normalizedDepth, GameObject previouslyChosen, bool allowImmediateRepeats, bool removeFromTable = false, bool allowNullSelection = false)
		{
			float totalWeight = 0;
			foreach (var w in Weights)
			{
				if (w == null)
					continue;

				if (!allowNullSelection && w.Value == null)
					continue;

				if (!(allowImmediateRepeats || previouslyChosen == null || w.Value != previouslyChosen))
					continue;

				totalWeight += w.GetWeight(isOnMainPath, normalizedDepth);
			}

			float randomNumber = (float)(random.NextDouble() * totalWeight);

			foreach (var w in Weights)
			{
				if (w == null)
					continue;

				if (!allowNullSelection && w.Value == null)
					continue;

				if (w.Value == previouslyChosen && Weights.Count > 1 && !allowImmediateRepeats)
					continue;

				float weight = w.GetWeight(isOnMainPath, normalizedDepth);

				if (randomNumber < weight)
				{
					if(removeFromTable)
						Weights.Remove(w);

					return w;
				}

				randomNumber -= weight;
			}

			return null;
		}

		/// <summary>
		/// Picks an object at random from a collection of tables, taking weights into account
		/// </summary>
		/// <param name="random">The random number generator to use</param>
		/// <param name="isOnMainPath">Is this object to be spawn on the main path</param>
		/// <param name="normalizedDepth">The normalized depth (0-1) that this object is to be spawned at in the dungeon</param>
		/// <param name="tables">A list of chance tables to pick from</param>
		/// <returns>A random value</returns>
		public static GameObject GetCombinedRandom(RandomStream random, bool isOnMainPath, float normalizedDepth, params GameObjectChanceTable[] tables)
		{
			float totalWeight = tables.SelectMany(x => x.Weights.Select(y => y.GetWeight(isOnMainPath, normalizedDepth))).Sum();
			float randomNumber = (float)(random.NextDouble() * totalWeight);

			foreach(var w in tables.SelectMany(x => x.Weights))
			{
				float weight = w.GetWeight(isOnMainPath, normalizedDepth);

				if (randomNumber < weight)
					return w.Value;

				randomNumber -= weight;
			}

			return null;
		}

		public static GameObjectChanceTable Combine(params GameObjectChanceTable[] tables)
		{
			GameObjectChanceTable combined = new GameObjectChanceTable();

			foreach(var t in tables)
				foreach(var w in t.Weights)
					combined.Weights.Add(new GameObjectChance(w.Value, w.MainPathWeight, w.BranchPathWeight, w.TileSet) { DepthWeightScale = w.DepthWeightScale });

			return combined;
		}
	}
}