using DunGen.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen.Graph
{
	/// <summary>
	/// A graph representing the flow of a dungeon
	/// </summary>
	[Serializable]
	[CreateAssetMenu(fileName = "New Dungeon", menuName = "DunGen/Dungeon Flow", order = 700)]
	public class DungeonFlow : ScriptableObject, ISerializationCallbackReceiver
	{
		public const int FileVersion = 1;

		#region Nested Types

		[Serializable]
		public sealed class GlobalPropSettings
		{
			public int ID;
			public IntRange Count;


			public GlobalPropSettings()
			{
				ID = 0;
				Count = new IntRange(0, 1);
			}

			public GlobalPropSettings(int id, IntRange count)
			{
				ID = id;
				Count = count;
			}
		}

		/// <summary>
		/// Defines how tile connection rules are applied.
		/// </summary>
		public enum TagConnectionMode
		{
			/// <summary>
			/// Tiles will only connect if they have tags that appear in the TileConnectionTags list
			/// </summary>
			Accept,
			/// <summary>
			/// Tiles will always connect unless they have tags that appear in the TileConnectionTags list
			/// </summary>
			Reject,
		}

		/// <summary>
		/// Defines how tags are used to determine which branch tip tiles to prune
		/// </summary>
		public enum BranchPruneMode
		{
			/// <summary>
			/// Removes tiles at the end of a branch when they have any of the specified tags
			/// </summary>
			AnyTagPresent,
			/// <summary>
			/// Removes tiles at the end of a branch when they don't have any of the specified tags
			/// </summary>
			AllTagsMissing,
		}

		#endregion

		#region Legacy Properties

		[SerializeField]
		[FormerlySerializedAs("GlobalPropGroupIDs")]
		private List<int> globalPropGroupID_obsolete = new List<int>();

		[SerializeField]
		[FormerlySerializedAs("GlobalPropRanges")]
		private List<IntRange> globalPropRanges_obsolete = new List<IntRange>();

		#endregion

		/// <summary>
		/// The minimum and maximum length of the dungeon
		/// </summary>
		public IntRange Length = new IntRange(5, 10);
		/// <summary>
		/// Determines how the number of branches from the main path is calculated
		/// </summary>
		public BranchMode BranchMode = BranchMode.Local;
		/// <summary>
		/// The number of branches to appear across the entire dungeon
		/// Only used if <see cref="BranchMode"/> is set to <see cref="BranchMode.Global"/>
		/// </summary>
		public IntRange BranchCount = new IntRange(1, 5);
		/// <summary>
		/// Information about which (and how many) global props should appear throughout the dungeon
		/// </summary>
		public List<GlobalPropSettings> GlobalProps = new List<GlobalPropSettings>();
		/// <summary>
		/// The asset that handles all of the keys that this dungeon needs to know about
		/// </summary>
		public KeyManager KeyManager = null;
		/// <summary>
		/// The percentage chance of two unconnected but overlapping doorways being connected (0-1)
		/// </summary>
		[Range(0f, 1f)]
		public float DoorwayConnectionChance = 0f;
		/// <summary>
		/// If true, only doorways belonging to tiles on the same section of the dungeon can be connected
		/// This will prevent some unexpected shortcuts from opening up through the dungeon
		/// </summary>
		public bool RestrictConnectionToSameSection = false;
		/// <summary>
		/// Simple rules for injecting special tiles into the dungeon generation process
		/// </summary>
		public List<TileInjectionRule> TileInjectionRules = new List<TileInjectionRule>();
		/// <summary>
		/// Defined how tile connection rules are applied see <see cref="TagConnectionMode"/>
		/// </summary>
		public TagConnectionMode TileTagConnectionMode;
		/// <summary>
		/// A list of tag pairs that define how tiles are allowed to connect. If empty, all
		/// tiles can connect to oneanother, otherwise the tiles must have a pair of
		/// matching tags from this list (omni-directional)
		/// </summary>
		public List<TagPair> TileConnectionTags = new List<TagPair>();
		/// <summary>
		/// Determines how tags are used to prune tiles at the end of branches
		/// </summary>
		public BranchPruneMode BranchTagPruneMode = BranchPruneMode.AllTagsMissing;
		/// <summary>
		/// A list of tags used to decide if a tile at the end of a branch should be deleted
		/// </summary>
		public List<Tag> BranchPruneTags = new List<Tag>();

		public List<GraphNode> Nodes = new List<GraphNode>();
		public List<GraphLine> Lines = new List<GraphLine>();

		[SerializeField]
		private int currentFileVersion;


		/// <summary>
		/// Creates the default graph
		/// </summary>
		public void Reset()
		{
			var emptyTileSet = new TileSet[0];
			var emptyArchetype = new DungeonArchetype[0];

			var builder = new DungeonFlowBuilder(this)
				.AddNode(emptyTileSet, "Start")
				.AddLine(emptyArchetype, 1.0f)
				.AddNode(emptyTileSet, "Goal");

			builder.Complete();
		}

		public GraphLine GetLineAtDepth(float normalizedDepth)
		{
			normalizedDepth = Mathf.Clamp(normalizedDepth, 0, 1);

			if (normalizedDepth == 0)
				return Lines[0];
			else if (normalizedDepth == 1)
				return Lines[Lines.Count - 1];

			foreach (var line in Lines)
				if (normalizedDepth >= line.Position && normalizedDepth < line.Position + line.Length)
					return line;

			Debug.LogError("GetLineAtDepth was unable to find a line at depth " + normalizedDepth + ". This shouldn't happen.");
			return null;
		}

		public DungeonArchetype[] GetUsedArchetypes()
		{
			return Lines.SelectMany(x => x.DungeonArchetypes).ToArray();
		}

		public TileSet[] GetUsedTileSets()
		{
			List<TileSet> tileSets = new List<TileSet>();

			foreach (var node in Nodes)
				tileSets.AddRange(node.TileSets);

			foreach(var line in Lines)
				foreach (var archetype in line.DungeonArchetypes)
				{
					tileSets.AddRange(archetype.TileSets);
					tileSets.AddRange(archetype.BranchCapTileSets);
				}

			return tileSets.ToArray();
		}

		public bool ShouldPruneTileWithTags(TagContainer tileTags)
		{
			switch (BranchTagPruneMode)
			{
				case BranchPruneMode.AnyTagPresent:
					return tileTags.HasAnyTag(BranchPruneTags.ToArray());

				case BranchPruneMode.AllTagsMissing:
					return !tileTags.HasAnyTag(BranchPruneTags.ToArray());

				default:
					throw new NotImplementedException(string.Format("BranchPruneMode {0} is not implemented", BranchTagPruneMode));
			}
		}

		public void OnBeforeSerialize()
		{
			currentFileVersion = FileVersion;
		}

		public void OnAfterDeserialize()
		{
			// Convert to new format for Global Props
			if(currentFileVersion < 1)
			{
				for (int i = 0; i < globalPropGroupID_obsolete.Count; i++)
				{
					int id = globalPropGroupID_obsolete[i];
					var count = globalPropRanges_obsolete[i];
					GlobalProps.Add(new GlobalPropSettings(id, count));
				}

				globalPropGroupID_obsolete.Clear();
				globalPropRanges_obsolete.Clear();
			}
		}

		/// <summary>
		/// Checks the connection rules (if any) to see if two tiles are allowed
		/// to connect by checking their tags
		/// </summary>
		/// <param name="tileA">The first tile</param>
		/// <param name="tileB">The second tile</param>
		/// <returns>True if the tiles are allowed to connect</returns>
		public bool CanTilesConnect(Tile tileA, Tile tileB)
		{
			if (tileA == null || tileB == null)
				return false;

			if (TileConnectionTags.Count == 0)
				return true;


			switch (TileTagConnectionMode)
			{
				case TagConnectionMode.Accept:
					return HasMatchingTagPair(tileA, tileB);

				case TagConnectionMode.Reject:
					return !HasMatchingTagPair(tileA, tileB);

				default:
					throw new NotImplementedException(string.Format("{0}.{1} is not implemented", typeof(TagConnectionMode).Name, TileTagConnectionMode));
			}
		}

		public bool CanDoorwaysConnect(ProposedConnection connection)
		{
			foreach (var rule in DoorwayPairFinder.CustomConnectionRules.OrderByDescending(r => r.Priority))
			{
				TileConnectionRule.ConnectionResult result = TileConnectionRule.ConnectionResult.Passthrough;

				if (rule.ConnectionDelegate != null)
					result = rule.ConnectionDelegate(connection);
#pragma warning disable CS0618 // For now, we allow rules that haven't been updated to the new delegate format
				else if (rule.Delegate != null)
					result = rule.Delegate(connection.PreviousTile.PrefabTile, connection.NextTile.PrefabTile, connection.PreviousDoorway.DoorwayComponent, connection.NextDoorway.DoorwayComponent);
#pragma warning restore

				if (result == TileConnectionRule.ConnectionResult.Passthrough)
					continue;
				else
					return result == TileConnectionRule.ConnectionResult.Allow;
			}

			// No custom rules handled this connection, use default behaviour
			return DoorwaySocket.CanSocketsConnect(connection.PreviousDoorway.DoorwayComponent.Socket, connection.NextDoorway.DoorwayComponent.Socket) && CanTilesConnect(connection.PreviousTile.PrefabTile, connection.NextTile.PrefabTile);
		}

		private bool HasMatchingTagPair(Tile tileA, Tile tileB)
		{
			foreach(var tagPair in TileConnectionTags)
			{
				if ((tileA.Tags.HasTag(tagPair.TagA) && tileB.Tags.HasTag(tagPair.TagB)) ||
					(tileB.Tags.HasTag(tagPair.TagA) && tileA.Tags.HasTag(tagPair.TagB)))
					return true;
			}

			return false;
		}
	}
}
