using DunGen.Graph;
using System;
using UnityEngine;

namespace DunGen
{
	/// <summary>
	/// A container for all of the information about a tile's posoitioning in the generated dungeon
	/// </summary>
	[Serializable]
	public sealed class TilePlacementData
	{
		/// <summary>
		/// Gets the depth of this tile in the dungeon along the main path
		/// </summary>
		public int PathDepth
		{
			get { return pathDepth; }
			internal set { pathDepth = value; }
		}
		/// <summary>
		/// Gets the normalized depth (0.0-1.0) of this tile in the dungeon along the main path
		/// </summary>
		public float NormalizedPathDepth
		{
			get { return normalizedPathDepth; }
			internal set { normalizedPathDepth = value; }
		}
		/// <summary>
		/// Gets the depth of this tile in the dungeon along the branch it's on
		/// </summary>
		public int BranchDepth
		{
			get { return branchDepth; }
			internal set { branchDepth = value; }
		}
		/// <summary>
		/// Gets the normalized depth (0.0-1.0) of this tile in the dungeon along the branch it's on
		/// </summary>
		public float NormalizedBranchDepth
		{
			get { return normalizedBranchDepth; }
			internal set { normalizedBranchDepth = value; }
		}
		/// <summary>
		/// Whether or not this tile lies on the dungeon's main path
		/// </summary>
		public bool IsOnMainPath
		{
			get { return isOnMainPath; }
			internal set { isOnMainPath = value; }
		}

		/// <summary>
		/// The boundaries of this tile
		/// </summary>
		public Bounds Bounds
		{
			get => worldBounds;
			private set => worldBounds = value;
		}

		/// <summary>
		/// The local boundaries of this tile
		/// </summary>
		public Bounds LocalBounds
		{
			get { return localBounds; }
			internal set
			{
				localBounds = value;
				RecalculateTransform();
			}
		}

		public GraphNode GraphNode
		{
			get { return graphNode; }
			internal set { graphNode = value; }
		}

		public GraphLine GraphLine
		{
			get { return graphLine; }
			internal set { graphLine = value; }
		}

		public DungeonArchetype Archetype
		{
			get { return archetype; }
			internal set { archetype = value; }
		}

		public TileSet TileSet
		{
			get { return tileSet; }
			internal set { tileSet = value; }
		}

		public Vector3 Position
		{
			get { return position; }
			set
			{
				position = value;
				RecalculateTransform();
			}
		}
		public Quaternion Rotation
		{
			get { return rotation; }
			set
			{
				rotation = value;
				RecalculateTransform();
			}
		}
		public Matrix4x4 Transform { get; private set; }

		/// <summary>
		/// Gets the depth of this tile. Returns the branch depth if on a branch path, otherwise, returns the main path depth
		/// </summary>
		public int Depth { get { return (isOnMainPath) ? pathDepth : branchDepth; } }

		/// <summary>
		/// Gets the normalized depth (0-1) of this tile. Returns the branch depth if on a branch path, otherwise, returns the main path depth
		/// </summary>
		public float NormalizedDepth { get { return (isOnMainPath) ? normalizedPathDepth : normalizedBranchDepth; } }

		/// <summary>
		/// Data about how this tile was injected, or null if it was not placed using tile injection
		/// </summary>
		public InjectedTile InjectionData { get; set; }


		[SerializeField]
		private int pathDepth;
		[SerializeField]
		private float normalizedPathDepth;
		[SerializeField]
		private int branchDepth;
		[SerializeField]
		private float normalizedBranchDepth;
		[SerializeField]
		private bool isOnMainPath;
		[SerializeField]
		private Bounds localBounds;
		[SerializeField]
		private Bounds worldBounds;
		[SerializeField]
		private GraphNode graphNode;
		[SerializeField]
		private GraphLine graphLine;
		[SerializeField]
		private DungeonArchetype archetype;
		[SerializeField]
		private TileSet tileSet;
		[SerializeField]
		private Vector3 position = Vector3.zero;
		[SerializeField]
		private Quaternion rotation = Quaternion.identity;


		public TilePlacementData()
		{
			RecalculateTransform();
		}

		public TilePlacementData(TilePlacementData copy)
		{
			PathDepth = copy.PathDepth;
			NormalizedPathDepth = copy.NormalizedPathDepth;
			BranchDepth = copy.BranchDepth;
			NormalizedBranchDepth = copy.NormalizedDepth;
			IsOnMainPath = copy.IsOnMainPath;
			LocalBounds = copy.LocalBounds;
			Transform = copy.Transform;
			GraphNode = copy.GraphNode;
			GraphLine = copy.GraphLine;
			Archetype = copy.Archetype;
			TileSet = copy.TileSet;
			InjectionData = copy.InjectionData;

			position = copy.position;
			rotation = copy.rotation;

			RecalculateTransform();
		}

		private void RecalculateTransform()
		{
			Transform = Matrix4x4.TRS(position, rotation, Vector3.one);

			Vector3 min = Transform.MultiplyPoint(localBounds.min);
			Vector3 max = Transform.MultiplyPoint(localBounds.max);

			Vector3 size = max - min;
			Vector3 center = min + size / 2f;

			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);
			size.z = Mathf.Abs(size.z);

			Bounds = new Bounds(center, size);
		}
	}
}

