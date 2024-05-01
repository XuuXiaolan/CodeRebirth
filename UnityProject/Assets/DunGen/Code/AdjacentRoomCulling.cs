using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Culling/Adjacent Room Culling")]
	public class AdjacentRoomCulling : MonoBehaviour
	{
		public delegate void VisibilityChangedDelegate(Tile tile, bool visible);

		/// <summary>
		/// How deep from the current room should tiles be considered visibile
		/// 0 = Only the current tile
		/// 1 = The current tile and all its neighbours
		/// 2 = The current tile, all its neighbours, and all THEIR neighbours
		/// etc...
		/// </summary>
		public int AdjacentTileDepth = 1;

		/// <summary>
		/// If true, tiles behind a closed door will be culled, even if they're within <see cref="AdjacentTileDepth"/>
		/// </summary>
		public bool CullBehindClosedDoors = true;

		/// <summary>
		/// If set, this transform will be used as the vantage point that rooms should be culled from.
		/// Useful for third person games where you want to cull from the character's position, not the camera
		/// </summary>
		public Transform TargetOverride = null;

		/// <summary>
		/// Whether culling should handle any components that start disabled
		/// </summary>
		public bool IncludeDisabledComponents = false;

		/// <summary>
		/// A set of override values for specific renderers.
		/// By default, this script will overwrite any renderer.enabled values we might set in
		/// gameplay code. This property lets us tell the culling that we want to override the
		/// visibility values its setting
		/// </summary>
		[NonSerialized]
		public Dictionary<Renderer, bool> OverrideRendererVisibilities = new Dictionary<Renderer, bool>();

		/// <summary>
		/// A set of override values for specific lights.
		/// By default, this script will overwrite any light.enabled values we might set in
		/// gameplay code. This property lets us tell the culling that we want to override the
		/// visibility values its setting
		/// </summary>
		[NonSerialized]
		public Dictionary<Light, bool> OverrideLightVisibilities = new Dictionary<Light, bool>();

		/// <summary>
		/// True when a dungeon has been assigned and we're ready to start culling
		/// </summary>
		public bool Ready { get; protected set; }

		public event VisibilityChangedDelegate TileVisibilityChanged;

		protected List<Tile> allTiles;
		protected List<Door> allDoors;
		protected List<Tile> oldVisibleTiles;
		protected List<Tile> visibleTiles;
		protected Dictionary<Tile, bool> tileVisibilities;
		protected Dictionary<Tile, List<Renderer>> tileRenderers;
		protected Dictionary<Tile, List<Light>> lightSources;
		protected Dictionary<Tile, List<ReflectionProbe>> reflectionProbes;
		protected Dictionary<Door, List<Renderer>> doorRenderers;

		protected Transform targetTransform { get { return (TargetOverride != null) ? TargetOverride : transform; } }
		private bool dirty;
		private DungeonGenerator generator;
		private Tile currentTile;
		private Queue<Tile> tilesToSearch;
		private List<Tile> searchedTiles;


		protected virtual void OnEnable()
		{
			var runtimeDungeon = FindObjectOfType<RuntimeDungeon>();

			if (runtimeDungeon != null)
			{
				generator = runtimeDungeon.Generator;
				generator.OnGenerationStatusChanged += OnDungeonGenerationStatusChanged; ;

				if (generator.Status == GenerationStatus.Complete)
					SetDungeon(generator.CurrentDungeon);
			}
		}

		protected virtual void OnDisable()
		{
			if (generator != null)
				generator.OnGenerationStatusChanged -= OnDungeonGenerationStatusChanged;

			ClearDungeon();
		}

		public virtual void SetDungeon(Dungeon dungeon)
		{
			if (Ready)
				ClearDungeon();

			if (dungeon == null)
				return;

			allTiles = new List<Tile>(dungeon.AllTiles);
			allDoors = new List<Door>(GetAllDoorsInDungeon(dungeon));
			oldVisibleTiles = new List<Tile>(allTiles.Count);
			visibleTiles = new List<Tile>(allTiles.Count);
			tileVisibilities = new Dictionary<Tile, bool>();
			tileRenderers = new Dictionary<Tile, List<Renderer>>();
			lightSources = new Dictionary<Tile, List<Light>>();
			reflectionProbes = new Dictionary<Tile, List<ReflectionProbe>>();
			doorRenderers = new Dictionary<Door, List<Renderer>>();

			UpdateRendererLists();

			foreach (var tile in allTiles)
				SetTileVisibility(tile, false);

			foreach (var door in allDoors)
			{
				door.OnDoorStateChanged += OnDoorStateChanged;
				SetDoorVisibility(door, false);
			}

			Ready = true;
			dirty = true;
		}

		public virtual bool IsTileVisible(Tile tile)
		{
			bool visibility;

			if (tileVisibilities.TryGetValue(tile, out visibility))
				return visibility;
			else
				return false;
		}

		protected IEnumerable<Door> GetAllDoorsInDungeon(Dungeon dungeon)
		{
			foreach (var doorObj in dungeon.Doors)
			{
				if (doorObj == null)
					continue;

				var door = doorObj.GetComponent<Door>();

				if (door != null)
					yield return door;
			}
		}

		protected virtual void ClearDungeon()
		{
			if (!Ready)
				return;

			foreach (var door in allDoors)
			{
				SetDoorVisibility(door, true);
				door.OnDoorStateChanged -= OnDoorStateChanged;
			}

			foreach (var tile in allTiles)
				SetTileVisibility(tile, true);

			Ready = false;
		}

		protected virtual void OnDoorStateChanged(Door door, bool isOpen)
		{
			dirty = true;
		}

		protected virtual void OnDungeonGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status == GenerationStatus.Complete)
				SetDungeon(generator.CurrentDungeon);
			else if (status == GenerationStatus.Failed)
				ClearDungeon();
		}

		protected virtual void LateUpdate()
		{
			if (!Ready)
				return;

			var oldTile = currentTile;

			// If currentTile doesn't exist, we need to first look for a dungeon,
			// then search every tile to find one that encompasses this GameObject
			if (currentTile == null)
				currentTile = FindCurrentTile();
			// If currentTile does exist, but we're not in it, we can perform a
			// breadth-first search radiating from currentTile. Assuming the player
			// is likely to be in an adjacent room, this should be much quicker than
			// testing every tile in the dungeon
			else if (!currentTile.Bounds.Contains(targetTransform.position))
				currentTile = SearchForNewCurrentTile();

			if (currentTile != oldTile)
				dirty = true;

			if (dirty)
				RefreshVisibility();

			dirty = false;
		}

		protected virtual void RefreshVisibility()
		{
			var temp = visibleTiles;
			visibleTiles = oldVisibleTiles;
			oldVisibleTiles = temp;

			UpdateVisibleTiles();

			// Hide any tiles that are no longer visible
			foreach (var tile in oldVisibleTiles)
				if(!visibleTiles.Contains(tile))
					SetTileVisibility(tile, false);

			// Show tiles that are newly visible
			foreach (var tile in visibleTiles)
				if(!oldVisibleTiles.Contains(tile))
					SetTileVisibility(tile, true);

			oldVisibleTiles.Clear();
			RefreshDoorVisibilities();
		}

		protected virtual void RefreshDoorVisibilities()
		{
			foreach (var door in allDoors)
			{
				bool visible = visibleTiles.Contains(door.DoorwayA.Tile) || visibleTiles.Contains(door.DoorwayB.Tile);
				SetDoorVisibility(door, visible);
			}
		}

		protected virtual void SetDoorVisibility(Door door, bool visible)
		{
			List<Renderer> renderers;

			if (doorRenderers.TryGetValue(door, out renderers))
			{
				for (int i = renderers.Count - 1; i >= 0; i--)
				{
					var renderer = renderers[i];

					if (renderer == null)
					{
						renderers.RemoveAt(i);
						continue;
					}

					// Check for overridden renderer visibility
					bool visibleOverride;
					if (OverrideRendererVisibilities.TryGetValue(renderer, out visibleOverride))
						renderer.enabled = visibleOverride;
					else
						renderer.enabled = visible;
				}
			}
		}

		protected virtual void UpdateVisibleTiles()
		{
			visibleTiles.Clear();

			if (currentTile != null)
				visibleTiles.Add(currentTile);

			int processTileStart = 0;

			// Add neighbours down to RoomDepth (0 = just tiles containing characters, 1 = plus adjacent tiles, etc)
			for (int i = 0; i < AdjacentTileDepth; i++)
			{
				int processTileEnd = visibleTiles.Count;

				for (int t = processTileStart; t < processTileEnd; t++)
				{
					var tile = visibleTiles[t];

					// Get all connections to adjacent tiles
					foreach (var doorway in tile.UsedDoorways)
					{
						var adjacentTile = doorway.ConnectedDoorway.Tile;

						// Skip the tile if it's already visible
						if (visibleTiles.Contains(adjacentTile))
							continue;

						// No need to add adjacent rooms to the visible list when the door between them is closed
						if (CullBehindClosedDoors)
						{
							var door = doorway.DoorComponent;

							if (door != null && door.ShouldCullBehind)
								continue;
						}

						visibleTiles.Add(adjacentTile);
					}
				}

				processTileStart = processTileEnd;
			}
		}

		protected virtual void SetTileVisibility(Tile tile, bool visible)
		{
			tileVisibilities[tile] = visible;

			// Renderers
			List<Renderer> renderers;

			if (tileRenderers.TryGetValue(tile, out renderers))
			{
				for (int i = renderers.Count - 1; i >= 0; i--)
				{
					var renderer = renderers[i];

					if(renderer == null)
					{
						renderers.RemoveAt(i);
						continue;
					}

					// Check for overridden renderer visibility
					bool visibleOverride;
					if (OverrideRendererVisibilities.TryGetValue(renderer, out visibleOverride))
						renderer.enabled = visibleOverride;
					else
						renderer.enabled = visible;
				}
			}

			// Lights
			List<Light> lights;

			if (lightSources.TryGetValue(tile, out lights))
			{
				for (int i = lights.Count - 1; i >= 0; i--)
				{
					var light = lights[i];

					if (light == null)
					{
						lights.RemoveAt(i);
						continue;
					}

					// Check for overridden renderer visibility
					bool visibleOverride;
					if (OverrideLightVisibilities.TryGetValue(light, out visibleOverride))
						light.enabled = visibleOverride;
					else
						light.enabled = visible;
				}
			}


			// Reflection Probes
			List<ReflectionProbe> probes;

			if (reflectionProbes.TryGetValue(tile, out probes))
			{
				for (int i = probes.Count - 1; i >= 0; i--)
				{
					var probe = probes[i];

					if (probe == null)
					{
						probes.RemoveAt(i);
						continue;
					}
					
					probe.enabled = visible;
				}
			}

			if (TileVisibilityChanged != null)
				TileVisibilityChanged(tile, visible);
		}

		public virtual void UpdateRendererLists()
		{
			foreach (var tile in allTiles)
			{
				// Renderers
				List<Renderer> renderers;

				if (!tileRenderers.TryGetValue(tile, out renderers))
					tileRenderers[tile] = renderers = new List<Renderer>();

				foreach (var renderer in tile.GetComponentsInChildren<Renderer>())
					if(IncludeDisabledComponents || (renderer.enabled && renderer.gameObject.activeInHierarchy))
						renderers.Add(renderer);

				// Lights
				List<Light> lights;

				if (!lightSources.TryGetValue(tile, out lights))
					lightSources[tile] = lights = new List<Light>();

				foreach (var light in tile.GetComponentsInChildren<Light>())
					if(IncludeDisabledComponents || (light.enabled && light.gameObject.activeInHierarchy))
						lights.Add(light);

				// Reflection Probes
				List<ReflectionProbe> probes;

				if (!reflectionProbes.TryGetValue(tile, out probes))
					reflectionProbes[tile] = probes = new List<ReflectionProbe>();

				foreach (var probe in tile.GetComponentsInChildren<ReflectionProbe>())
					if (IncludeDisabledComponents || (probe.enabled && probe.gameObject.activeInHierarchy))
						probes.Add(probe);
			}

			foreach(var door in allDoors)
			{
				List<Renderer> renderers = new List<Renderer>();
				doorRenderers[door] = renderers;

				foreach(var r in door.GetComponentsInChildren<Renderer>(true))
					if(IncludeDisabledComponents || (r.enabled && r.gameObject.activeInHierarchy))
						renderers.Add(r);
			}
		}

		protected Tile FindCurrentTile()
		{
			var dungeon = FindObjectOfType<Dungeon>();

			if (dungeon == null)
				return null;

			foreach (var tile in dungeon.AllTiles)
			{
				if (tile.Bounds.Contains(targetTransform.position))
					return tile;
			}

			return null;
		}

		protected Tile SearchForNewCurrentTile()
		{
			if (tilesToSearch == null)
				tilesToSearch = new Queue<Tile>();
			if (searchedTiles == null)
				searchedTiles = new List<Tile>();

			// Add all tiles adjacent to currentTile to the search queue
			foreach (var door in currentTile.UsedDoorways)
			{
				var adjacentTile = door.ConnectedDoorway.Tile;

				if (!tilesToSearch.Contains(adjacentTile))
					tilesToSearch.Enqueue(adjacentTile);
			}

			// Breadth-first search to find the tile which contains the player
			while (tilesToSearch.Count > 0)
			{
				var tile = tilesToSearch.Dequeue();

				if (tile.Bounds.Contains(targetTransform.position))
				{
					tilesToSearch.Clear();
					searchedTiles.Clear();
					return tile;
				}
				else
				{
					searchedTiles.Add(tile);

					foreach (var door in tile.UsedDoorways)
					{
						var adjacentTile = door.ConnectedDoorway.Tile;

						if (!tilesToSearch.Contains(adjacentTile) &&
							!searchedTiles.Contains(adjacentTile))
							tilesToSearch.Enqueue(adjacentTile);
					}
				}
			}

			searchedTiles.Clear();
			return null;
		}
	}
}
