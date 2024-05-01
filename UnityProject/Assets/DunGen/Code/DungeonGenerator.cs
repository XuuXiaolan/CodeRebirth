using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DunGen.Graph;
using System.Collections;
using UnityEngine.Serialization;

using Debug = UnityEngine.Debug;

namespace DunGen
{
	public delegate void TileInjectionDelegate(RandomStream randomStream, ref List<InjectedTile> tilesToInject);

	public enum AxisDirection
	{
#if UNITY_2019_2_OR_NEWER
		[InspectorName("+X")]
		PosX,
		[InspectorName("-X")]
		NegX,
		[InspectorName("+Y")]
		PosY,
		[InspectorName("-Y")]
		NegY,
		[InspectorName("+Z")]
		PosZ,
		[InspectorName("-Z")]
		NegZ,
#else
		PosX,
		NegX,
		PosY,
		NegY,
		PosZ,
		NegZ,
#endif
	}

	[Serializable]
	public class DungeonGenerator : ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 1;

		#region Legacy Properties

		// Legacy properties only exist to avoid breaking existing projects
		// Converting old data structures over to the new ones

		[SerializeField]
		[FormerlySerializedAs("AllowImmediateRepeats")]
		private bool allowImmediateRepeats = false;

		#endregion

		#region Helper Struct

		struct PropProcessingData
		{
			public RandomProp PropComponent;
			public int HierarchyDepth;
			public Tile OwningTile;
		}

		#endregion


		public int Seed;
		public bool ShouldRandomizeSeed = true;
		public RandomStream RandomStream { get; protected set; }
		public int MaxAttemptCount = 20;
		public bool UseMaximumPairingAttempts = false;
		public int MaxPairingAttempts = 5;
		public bool IgnoreSpriteBounds = false;
		public AxisDirection UpDirection = AxisDirection.PosY;
		[FormerlySerializedAs("OverrideAllowImmediateRepeats")]
		public bool OverrideRepeatMode = false;
		public TileRepeatMode RepeatMode = TileRepeatMode.Allow;
		public bool OverrideAllowTileRotation = false;
		public bool AllowTileRotation = false;
		public bool DebugRender = false;
		public float LengthMultiplier = 1.0f;
		public bool PlaceTileTriggers = true;
		public int TileTriggerLayer = 2;
		public bool GenerateAsynchronously = false;
		public float MaxAsyncFrameMilliseconds = 50;
		public float PauseBetweenRooms = 0;
		public bool RestrictDungeonToBounds = false;
		public Bounds TilePlacementBounds = new Bounds(Vector3.zero, Vector3.one * 10f);
		public float OverlapThreshold = 0.01f;
		public float Padding = 0f;
		public bool DisallowOverhangs = false;

		public Vector3 UpVector
		{
			get
			{
				switch (UpDirection)
				{
					case AxisDirection.PosX:
						return new Vector3(+1, 0, 0);
					case AxisDirection.NegX:
						return new Vector3(-1, 0, 0);
					case AxisDirection.PosY:
						return new Vector3(0, +1, 0);
					case AxisDirection.NegY:
						return new Vector3(0, -1, 0);
					case AxisDirection.PosZ:
						return new Vector3(0, 0, +1);
					case AxisDirection.NegZ:
						return new Vector3(0, 0, -1);

					default:
						throw new NotImplementedException("AxisDirection '" + UpDirection + "' not implemented");
				}
			}
		}

		public event GenerationStatusDelegate OnGenerationStatusChanged;
		public static event GenerationStatusDelegate OnAnyDungeonGenerationStatusChanged;
		public event TileInjectionDelegate TileInjectionMethods;
		public event Action Cleared;
		public event Action Retrying;

		public GameObject Root;
		public DungeonFlow DungeonFlow;
		public GenerationStatus Status { get; private set; }
		public GenerationStats GenerationStats { get; private set; }
		public int ChosenSeed { get; protected set; }
		public Dungeon CurrentDungeon { get; private set; }
		public bool IsGenerating { get; private set; }
		public bool IsAnalysis { get; set; }

		protected int retryCount;
		protected DungeonProxy proxyDungeon;
		protected readonly Dictionary<TilePlacementResult, int> tilePlacementResultCounters = new Dictionary<TilePlacementResult, int>();
		protected readonly List<GameObject> useableTiles = new List<GameObject>();
		protected int targetLength;
		protected List<InjectedTile> tilesPendingInjection;
		protected List<DungeonGeneratorPostProcessStep> postProcessSteps = new List<DungeonGeneratorPostProcessStep>();

		[SerializeField]
		private int fileVersion;
		private int nextNodeIndex;
		private DungeonArchetype currentArchetype;
		private GraphLine previousLineSegment;
		private List<TileProxy> preProcessData = new List<TileProxy>();
		private Stopwatch yieldTimer = new Stopwatch();
		private Dictionary<TileProxy, InjectedTile> injectedTiles = new Dictionary<TileProxy, InjectedTile>();


		public DungeonGenerator()
		{
			GenerationStats = new GenerationStats();
		}

		public DungeonGenerator(GameObject root)
			: this()
		{
			Root = root;
		}

		public void Generate()
		{
			if (IsGenerating)
				return;

			IsAnalysis = false;
			IsGenerating = true;
			Wait(OuterGenerate());
		}

		public void Cancel()
		{
			if (!IsGenerating)
				return;

			Clear(true);
			IsGenerating = false;
		}

		public Dungeon DetachDungeon()
		{
			if (CurrentDungeon == null)
				return null;

			Dungeon dungeon = CurrentDungeon;
			CurrentDungeon = null;
			Root = null;
			Clear(true);

			return dungeon;
		}

		protected virtual IEnumerator OuterGenerate()
		{
			Clear(false);

			yieldTimer.Restart();

			Status = GenerationStatus.NotStarted;

#if UNITY_EDITOR
			// Validate the dungeon archetype if we're running in the editor
			DungeonArchetypeValidator validator = new DungeonArchetypeValidator(DungeonFlow);

			if (!validator.IsValid())
			{
				ChangeStatus(GenerationStatus.Failed);
				IsGenerating = false;
				yield break;
			}
#endif

			ChosenSeed = (ShouldRandomizeSeed) ? new RandomStream().Next() : Seed;
			RandomStream = new RandomStream(ChosenSeed);

			if (Root == null)
				Root = new GameObject(Constants.DefaultDungeonRootName);


			yield return Wait(InnerGenerate(false));

			IsGenerating = false;
		}

		private Coroutine Wait(IEnumerator routine)
		{
			if (GenerateAsynchronously)
				return CoroutineHelper.Start(routine);
			else
			{
				while (routine.MoveNext()) { }
				return null;
			}
		}

		public void RandomizeSeed()
		{
			Seed = new RandomStream().Next();
		}

		protected virtual IEnumerator InnerGenerate(bool isRetry)
		{
			if (isRetry)
			{
				ChosenSeed = RandomStream.Next();
				RandomStream = new RandomStream(ChosenSeed);


				if (retryCount >= MaxAttemptCount && Application.isEditor)
				{
					string errorText = "Failed to generate the dungeon " + MaxAttemptCount + " times.\n" +
										"This could indicate a problem with the way the tiles are set up. Try to make sure most rooms have more than one doorway and that all doorways are easily accessible.\n" +
										"Here are a list of all reasons a tile placement had to be retried:";

					foreach (var pair in tilePlacementResultCounters)
						if (pair.Value > 0)
							errorText += "\n" + pair.Key + " (x" + pair.Value + ")";

					Debug.LogError(errorText);
					ChangeStatus(GenerationStatus.Failed);
					yield break;
				}

				retryCount++;
				GenerationStats.IncrementRetryCount();

				if (Retrying != null)
					Retrying();
			}
			else
			{
				retryCount = 0;
				GenerationStats.Clear();
			}

			CurrentDungeon = Root.GetComponent<Dungeon>();
			if (CurrentDungeon == null)
				CurrentDungeon = Root.AddComponent<Dungeon>();

			CurrentDungeon.DebugRender = DebugRender;
			CurrentDungeon.PreGenerateDungeon(this);

			Clear(false);
			targetLength = Mathf.RoundToInt(DungeonFlow.Length.GetRandom(RandomStream) * LengthMultiplier);
			targetLength = Mathf.Max(targetLength, 2);

			Transform debugVisualsRoot = (PauseBetweenRooms > 0f) ? Root.transform : null;
			proxyDungeon = new DungeonProxy(debugVisualsRoot);

			// Tile Injection
			GenerationStats.BeginTime(GenerationStatus.TileInjection);

			if (tilesPendingInjection == null)
				tilesPendingInjection = new List<InjectedTile>();
			else
				tilesPendingInjection.Clear();

			injectedTiles.Clear();
			GatherTilesToInject();

			// Pre-Processing
			GenerationStats.BeginTime(GenerationStatus.PreProcessing);
			PreProcess();

			// Main Path Generation
			GenerationStats.BeginTime(GenerationStatus.MainPath);
			yield return Wait(GenerateMainPath());

			// We may have had to retry when generating the main path, if so, the status will be either Complete or Failed and we should exit here
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
				yield break;

			// Branch Paths Generation
			GenerationStats.BeginTime(GenerationStatus.Branching);
			yield return Wait(GenerateBranchPaths());

			// If there are any required tiles missing from the tile injection stage, the generation process should fail
			foreach (var tileInjection in tilesPendingInjection)
				if (tileInjection.IsRequired)
				{
					yield return Wait(InnerGenerate(true));
					yield break;
				}

			// We may have missed some required injected tiles and have had to retry, if so, the status will be either Complete or Failed and we should exit here
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
				yield break;

			GenerationStats.BeginTime(GenerationStatus.BranchPruning);

			// Prune branches if we have any tags set up
			if (DungeonFlow.BranchPruneTags.Count > 0)
				PruneBranches();

			GenerationStats.BeginTime(GenerationStatus.InstantiatingTiles);

			proxyDungeon.ConnectOverlappingDoorways(DungeonFlow.DoorwayConnectionChance, DungeonFlow, RandomStream);
			CurrentDungeon.FromProxy(proxyDungeon, this);

			// Post-Processing
			yield return Wait(PostProcess());

			// Waiting one frame so objects are in their expected state
			yield return null;

			// Inform objects in the dungeon that generation is complete
			foreach (var callbackReceiver in CurrentDungeon.gameObject.GetComponentsInChildren<IDungeonCompleteReceiver>(false))
				callbackReceiver.OnDungeonComplete(CurrentDungeon);

			ChangeStatus(GenerationStatus.Complete);

			bool charactersShouldRecheckTile = true;

#if UNITY_EDITOR
			charactersShouldRecheckTile = UnityEditor.EditorApplication.isPlaying;
#endif

			// Let DungenCharacters know that they should re-check the Tile they're in
			if (charactersShouldRecheckTile)
			{
				foreach (var character in Component.FindObjectsOfType<DungenCharacter>())
					character.ForceRecheckTile();
			}
		}

		private void PruneBranches()
		{
			var branchTips = new Stack<TileProxy>();

			foreach (var tile in proxyDungeon.BranchPathTiles)
			{
				var connectedTiles = tile.UsedDoorways.Select(d => d.ConnectedDoorway.TileProxy);

				// If we're not connected to another tile with a higher branch depth, this is a branch tip
				if (!connectedTiles.Any(t => t.Placement.BranchDepth > tile.Placement.BranchDepth))
					branchTips.Push(tile);
			}

			while (branchTips.Count > 0)
			{
				var tile = branchTips.Pop();

				bool isRequiredTile = tile.Placement.InjectionData != null && tile.Placement.InjectionData.IsRequired;
				bool shouldPruneTile = !isRequiredTile && DungeonFlow.ShouldPruneTileWithTags(tile.PrefabTile.Tags);

				if (shouldPruneTile)
				{
					// Find that tile that came before this one
					var precedingTileConnection = tile.UsedDoorways
						.Select(d => d.ConnectedDoorway)
						.Where(d => d.TileProxy.Placement.IsOnMainPath || d.TileProxy.Placement.BranchDepth < tile.Placement.BranchDepth)
						.Select(d => new ProxyDoorwayConnection(d, d.ConnectedDoorway))
						.First();

					// Remove tile and connection
					proxyDungeon.RemoveTile(tile);
					proxyDungeon.RemoveConnection(precedingTileConnection);
					GenerationStats.PrunedBranchTileCount++;

					var precedingTile = precedingTileConnection.A.TileProxy;

					// The preceding tile is the new tip of this branch
					if (!precedingTile.Placement.IsOnMainPath)
						branchTips.Push(precedingTile);
				}
			}
		}

		public virtual void Clear(bool stopCoroutines)
		{
			if (stopCoroutines)
				CoroutineHelper.StopAll();

			if (proxyDungeon != null)
				proxyDungeon.ClearDebugVisuals();

			proxyDungeon = null;

			if (CurrentDungeon != null)
				CurrentDungeon.Clear();

			useableTiles.Clear();
			preProcessData.Clear();

			previousLineSegment = null;
			tilePlacementResultCounters.Clear();

			if (Cleared != null)
				Cleared();
		}

		private void ChangeStatus(GenerationStatus status)
		{
			var previousStatus = Status;
			Status = status;

			if (status == GenerationStatus.Complete || status == GenerationStatus.Failed)
				IsGenerating = false;

			if (status == GenerationStatus.Failed)
				Clear(true);

			if (previousStatus != status)
			{
				OnGenerationStatusChanged?.Invoke(this, status);
				OnAnyDungeonGenerationStatusChanged?.Invoke(this, status);
			}
		}

		protected virtual void PreProcess()
		{
			if (preProcessData.Count > 0)
				return;

			ChangeStatus(GenerationStatus.PreProcessing);

			var usedTileSets = DungeonFlow.GetUsedTileSets().Concat(tilesPendingInjection.Select(x => x.TileSet)).Distinct();

			foreach (var tileSet in usedTileSets)
				foreach (var tile in tileSet.TileWeights.Weights)
				{
					if (tile.Value != null)
					{
						useableTiles.Add(tile.Value);
						tile.TileSet = tileSet;
					}
				}
		}

		protected virtual void GatherTilesToInject()
		{
			var injectionRandomStream = new RandomStream(ChosenSeed);

			// Gather from DungeonFlow
			foreach (var rule in DungeonFlow.TileInjectionRules)
			{
				// Ignore invalid rules
				if (rule.TileSet == null || (!rule.CanAppearOnMainPath && !rule.CanAppearOnBranchPath))
					continue;

				bool isOnMainPath = (!rule.CanAppearOnBranchPath) ? true : (!rule.CanAppearOnMainPath) ? false : injectionRandomStream.NextDouble() > 0.5;
				var injectedTile = new InjectedTile(rule, isOnMainPath, injectionRandomStream);

				tilesPendingInjection.Add(injectedTile);
			}

			// Gather from external delegates
			if (TileInjectionMethods != null)
				TileInjectionMethods(injectionRandomStream, ref tilesPendingInjection);
		}

		protected virtual IEnumerator GenerateMainPath()
		{
			ChangeStatus(GenerationStatus.MainPath);
			nextNodeIndex = 0;
			var handledNodes = new List<GraphNode>(DungeonFlow.Nodes.Count);
			bool isDone = false;
			int i = 0;

			// Keep track of these now, we'll need them later when we know the actual length of the dungeon
			var tileSets = new List<List<TileSet>>(targetLength);
			var archetypes = new List<DungeonArchetype>(targetLength);
			var nodes = new List<GraphNode>(targetLength);
			var lines = new List<GraphLine>(targetLength);

			// We can't rigidly stick to the target length since we need at least one room for each node and that might be more than targetLength
			while (!isDone)
			{
				float depth = Mathf.Clamp(i / (float)(targetLength - 1), 0, 1);
				GraphLine lineSegment = DungeonFlow.GetLineAtDepth(depth);

				// This should never happen
				if (lineSegment == null)
				{
					yield return Wait(InnerGenerate(true));
					yield break;
				}

				// We're on a new line segment, change the current archetype
				if (lineSegment != previousLineSegment)
				{
					currentArchetype = lineSegment.GetRandomArchetype(RandomStream, archetypes);
					previousLineSegment = lineSegment;
				}

				List<TileSet> useableTileSets = null;
				GraphNode nextNode = null;
				var orderedNodes = DungeonFlow.Nodes.OrderBy(x => x.Position).ToArray();

				// Determine which node comes next
				foreach (var node in orderedNodes)
				{
					if (depth >= node.Position && !handledNodes.Contains(node))
					{
						nextNode = node;
						handledNodes.Add(node);
						break;
					}
				}

				// Assign the TileSets to use based on whether we're on a node or a line segment
				if (nextNode != null)
				{
					useableTileSets = nextNode.TileSets;
					nextNodeIndex = (nextNodeIndex >= orderedNodes.Length - 1) ? -1 : nextNodeIndex + 1;
					archetypes.Add(null);
					lines.Add(null);
					nodes.Add(nextNode);

					if (nextNode == orderedNodes[orderedNodes.Length - 1])
						isDone = true;
				}
				else
				{
					useableTileSets = currentArchetype.TileSets;
					archetypes.Add(currentArchetype);
					lines.Add(lineSegment);
					nodes.Add(null);
				}

				tileSets.Add(useableTileSets);

				i++;
			}

			int tileRetryCount = 0;
			int totalForLoopRetryCount = 0;

			for (int j = 0; j < tileSets.Count; j++)
			{
				var attachTo = (j == 0) ? null : proxyDungeon.MainPathTiles[proxyDungeon.MainPathTiles.Count - 1];
				var tile = AddTile(attachTo, tileSets[j], j / (float)(tileSets.Count - 1), archetypes[j]);

				// if no tile could be generated delete last successful tile and retry from previous index
				// else return false
				if (j > 5 && tile == null && tileRetryCount < 5 && totalForLoopRetryCount < 20)
				{
					TileProxy previousTile = proxyDungeon.MainPathTiles[j - 1];

					// If the tile we're removing was placed by tile injection, be sure to place the injected tile back on the pending list
					InjectedTile previousInjectedTile;
					if (injectedTiles.TryGetValue(previousTile, out previousInjectedTile))
					{
						tilesPendingInjection.Add(previousInjectedTile);
						injectedTiles.Remove(previousTile);
					}

					proxyDungeon.RemoveLastConnection();
					proxyDungeon.RemoveTile(previousTile);

					j -= 2; // -2 because loop adds 1
					tileRetryCount++;
					totalForLoopRetryCount++;
				}
				else if (tile == null)
				{
					yield return Wait(InnerGenerate(true));
					yield break;
				}
				else
				{
					tile.Placement.GraphNode = nodes[j];
					tile.Placement.GraphLine = lines[j];
					tileRetryCount = 0;


					// Wait for a frame to allow for animated loading screens, etc
					if (ShouldSkipFrame(true))
						yield return GetRoomPause();
				}
			}

			yield break; // Required for generation to run synchronously
		}

		private bool ShouldSkipFrame(bool isRoomPlacement)
		{
			if (!GenerateAsynchronously)
				return false;

			if (isRoomPlacement && PauseBetweenRooms > 0)
				return true;
			else
			{
				bool frameWasTooLong = yieldTimer.Elapsed.TotalMilliseconds >= MaxAsyncFrameMilliseconds;

				if (frameWasTooLong)
				{
					yieldTimer.Restart();
					return true;
				}
				else
					return false;
			}
		}

		private YieldInstruction GetRoomPause()
		{
			if (PauseBetweenRooms > 0)
				return new WaitForSeconds(PauseBetweenRooms);
			else
				return null;
		}

		protected virtual IEnumerator GenerateBranchPaths()
		{
			ChangeStatus(GenerationStatus.Branching);

			int[] mainPathBranches = new int[proxyDungeon.MainPathTiles.Count];
			BranchCountHelper.ComputeBranchCounts(DungeonFlow, RandomStream, proxyDungeon, ref mainPathBranches);

			for (int b = 0; b < mainPathBranches.Length; b++)
			{
				var tile = proxyDungeon.MainPathTiles[b];
				int branchCount = mainPathBranches[b];

				// This tile was created from a graph node, there should be no branching
				if (tile.Placement.Archetype == null)
					continue;

				if (branchCount == 0)
					continue;

				for (int i = 0; i < branchCount; i++)
				{
					TileProxy previousTile = tile;
					int branchDepth = tile.Placement.Archetype.BranchingDepth.GetRandom(RandomStream);

					for (int j = 0; j < branchDepth; j++)
					{
						List<TileSet> useableTileSets;

						if (j == (branchDepth - 1) && tile.Placement.Archetype.GetHasValidBranchCapTiles())
						{
							if (tile.Placement.Archetype.BranchCapType == BranchCapType.InsteadOf)
								useableTileSets = tile.Placement.Archetype.BranchCapTileSets;
							else
								useableTileSets = tile.Placement.Archetype.TileSets.Concat(tile.Placement.Archetype.BranchCapTileSets).ToList();
						}
						else
							useableTileSets = tile.Placement.Archetype.TileSets;

						float normalizedDepth = (branchDepth <= 1) ? 1 : j / (float)(branchDepth - 1);
						var newTile = AddTile(previousTile, useableTileSets, normalizedDepth, tile.Placement.Archetype);

						if (newTile == null)
							break;

						newTile.Placement.BranchDepth = j;
						newTile.Placement.NormalizedBranchDepth = normalizedDepth;
						newTile.Placement.GraphNode = previousTile.Placement.GraphNode;
						newTile.Placement.GraphLine = previousTile.Placement.GraphLine;
						previousTile = newTile;

						// Wait for a frame to allow for animated loading screens, etc
						if (ShouldSkipFrame(true))
							yield return GetRoomPause();
					}
				}
			}

			yield break;
		}

		protected virtual TileProxy AddTile(TileProxy attachTo, IEnumerable<TileSet> useableTileSets, float normalizedDepth, DungeonArchetype archetype, TilePlacementResult result = TilePlacementResult.None)
		{
			bool isOnMainPath = (Status == GenerationStatus.MainPath);
			bool isFirstTile = attachTo == null;

			// Check list of tiles to inject
			InjectedTile chosenInjectedTile = null;
			int injectedTileIndexToRemove = -1;

			bool isPlacingSpecificRoom = isOnMainPath && (archetype == null);

			if (tilesPendingInjection != null && !isPlacingSpecificRoom)
			{
				float pathDepth = (isOnMainPath) ? normalizedDepth : attachTo.Placement.PathDepth / (targetLength - 1f);
				float branchDepth = (isOnMainPath) ? 0 : normalizedDepth;

				for (int i = 0; i < tilesPendingInjection.Count; i++)
				{
					var injectedTile = tilesPendingInjection[i];

					if (injectedTile.ShouldInjectTileAtPoint(isOnMainPath, pathDepth, branchDepth))
					{
						chosenInjectedTile = injectedTile;
						injectedTileIndexToRemove = i;

						break;
					}
				}
			}


			// Select appropriate tile weights
			IEnumerable<GameObjectChance> chanceEntries;

			if (chosenInjectedTile != null)
				chanceEntries = new List<GameObjectChance>(chosenInjectedTile.TileSet.TileWeights.Weights);
			else
				chanceEntries = useableTileSets.SelectMany(x => x.TileWeights.Weights);


			// Apply constraint overrides
			bool allowRotation = (isFirstTile) ? false : attachTo.PrefabTile.AllowRotation;

			if (OverrideAllowTileRotation)
				allowRotation = AllowTileRotation;



			DoorwayPairFinder doorwayPairFinder = new DoorwayPairFinder()
			{
				DungeonFlow = DungeonFlow,
				RandomStream = RandomStream,
				Archetype = archetype,
				GetTileTemplateDelegate = GetTileTemplate,
				IsOnMainPath = isOnMainPath,
				NormalizedDepth = normalizedDepth,
				PreviousTile = attachTo,
				UpVector = UpVector,
				AllowRotation = allowRotation,
				TileWeights = new List<GameObjectChance>(chanceEntries),
				DungeonProxy = proxyDungeon,

				IsTileAllowedPredicate = (TileProxy previousTile, TileProxy potentialNextTile, ref float weight) =>
				{
					bool isImmediateRepeat = previousTile != null && (potentialNextTile.Prefab == previousTile.Prefab);
					var repeatMode = TileRepeatMode.Allow;

					if (OverrideRepeatMode)
						repeatMode = RepeatMode;
					else if (potentialNextTile != null)
						repeatMode = potentialNextTile.PrefabTile.RepeatMode;

					bool allowTile = true;

					switch (repeatMode)
					{
						case TileRepeatMode.Allow:
							allowTile = true;
							break;

						case TileRepeatMode.DisallowImmediate:
							allowTile = !isImmediateRepeat;
							break;

						case TileRepeatMode.Disallow:
							allowTile = !proxyDungeon.AllTiles.Where(t => t.Prefab == potentialNextTile.Prefab).Any();
							break;

						default:
							throw new NotImplementedException("TileRepeatMode " + repeatMode + " is not implemented");
					}

					return allowTile;
				},
			};

			int? maxPairingAttempts = (UseMaximumPairingAttempts) ? (int?)MaxPairingAttempts : null;
			Queue<DoorwayPair> pairsToTest = doorwayPairFinder.GetDoorwayPairs(maxPairingAttempts);
			TilePlacementResult lastTileResult = TilePlacementResult.NoValidTile;
			TileProxy createdTile = null;

			while (pairsToTest.Count > 0)
			{
				var pair = pairsToTest.Dequeue();

				lastTileResult = TryPlaceTile(pair, archetype, out createdTile);

				if (lastTileResult == TilePlacementResult.None)
					break;
				else
					AddTilePlacementResult(lastTileResult);
			}

			// Successfully placed the tile
			if (lastTileResult == TilePlacementResult.None)
			{
				// We've successfully injected the tile, so we can remove it from the pending list now
				if (chosenInjectedTile != null)
				{
					createdTile.Placement.InjectionData = chosenInjectedTile;

					injectedTiles[createdTile] = chosenInjectedTile;
					tilesPendingInjection.RemoveAt(injectedTileIndexToRemove);

					if (isOnMainPath)
						targetLength++;
				}

				return createdTile;
			}
			else
				return null;
		}

		protected void AddTilePlacementResult(TilePlacementResult result)
		{
			int count;

			if (!tilePlacementResultCounters.TryGetValue(result, out count))
				tilePlacementResultCounters[result] = 1;
			else
				tilePlacementResultCounters[result] = count + 1;
		}

		protected TilePlacementResult TryPlaceTile(DoorwayPair pair, DungeonArchetype archetype, out TileProxy tile)
		{
			tile = null;

			var toTemplate = pair.NextTemplate;
			var fromDoorway = pair.PreviousDoorway;

			if (toTemplate == null)
				return TilePlacementResult.TemplateIsNull;

			int toDoorwayIndex = pair.NextTemplate.Doorways.IndexOf(pair.NextDoorway);
			tile = new TileProxy(toTemplate);
			tile.Placement.IsOnMainPath = Status == GenerationStatus.MainPath;
			tile.Placement.Archetype = archetype;
			tile.Placement.TileSet = pair.NextTileSet;

			if (fromDoorway != null)
			{
				// Move the proxy object into position
				var toProxyDoor = tile.Doorways[toDoorwayIndex];
				tile.PositionBySocket(toProxyDoor, fromDoorway);

				Bounds proxyBounds = tile.Placement.Bounds;

				// Check if the new tile is outside of the valid bounds
				if (RestrictDungeonToBounds && !TilePlacementBounds.Contains(proxyBounds))
					return TilePlacementResult.OutOfBounds;

				// Check if the new tile is colliding with any other
				bool isColliding = IsCollidingWithAnyTile(tile, fromDoorway.TileProxy);

				if (isColliding)
					return TilePlacementResult.TileIsColliding;
			}

			if (tile == null)
				return TilePlacementResult.NewTileIsNull;

			if (tile.Placement.IsOnMainPath)
			{
				if (pair.PreviousTile != null)
					tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth + 1;
			}
			else
			{
				tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth;
				tile.Placement.BranchDepth = (pair.PreviousTile.Placement.IsOnMainPath) ? 0 : pair.PreviousTile.Placement.BranchDepth + 1;
			}

			if (fromDoorway != null)
			{
				var toDoorway = tile.Doorways[toDoorwayIndex];
				proxyDungeon.MakeConnection(fromDoorway, toDoorway);
			}

			proxyDungeon.AddTile(tile);

			return TilePlacementResult.None;
		}

		protected TileProxy GetTileTemplate(GameObject prefab)
		{
			var template = preProcessData.Where(x => { return x.Prefab == prefab; }).FirstOrDefault();

			// No proxy has been loaded yet, we should create one
			if (template == null)
			{
				template = new TileProxy(prefab, IgnoreSpriteBounds, UpVector);
				preProcessData.Add(template);
			}

			return template;
		}

		protected TileProxy PickRandomTemplate(DoorwaySocket socketGroupFilter)
		{
			// Pick a random tile
			var tile = useableTiles[RandomStream.Next(0, useableTiles.Count)];
			var template = GetTileTemplate(tile);

			// If there's a socket group filter and the chosen Tile doesn't have a socket of this type, try again
			if (socketGroupFilter != null && !template.UnusedDoorways.Where(d => d.Socket == socketGroupFilter).Any())
				return PickRandomTemplate(socketGroupFilter);

			return template;
		}

		protected int NormalizedDepthToIndex(float normalizedDepth)
		{
			return Mathf.RoundToInt(normalizedDepth * (targetLength - 1));
		}

		protected float IndexToNormalizedDepth(int index)
		{
			return index / (float)targetLength;
		}

		protected bool IsCollidingWithAnyTile(TileProxy newTile, TileProxy previousTile)
		{
			foreach (var t in proxyDungeon.AllTiles)
			{
				bool isConnected = previousTile == t;
				float maxOverlap = (isConnected) ? OverlapThreshold : -Padding;

				if (DisallowOverhangs && !isConnected)
				{
					if (newTile.IsOverlappingOrOverhanging(t, UpDirection, maxOverlap))
						return true;
				}
				else
				{
					if (newTile.IsOverlapping(t, maxOverlap))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Registers a post-process step with the generator which allows for a callback function to be invoked during the PostProcess step
		/// </summary>
		/// <param name="postProcessCallback">The callback to invoke</param>
		/// <param name="priority">The priority which determines the order in which post-process steps are invoked (highest to lowest).</param>
		/// <param name="phase">Which phase to run the post-process step. Used to determine whether the step should run before or after DunGen's built-in post-processing</param>
		public void RegisterPostProcessStep(Action<DungeonGenerator> postProcessCallback, int priority = 0, PostProcessPhase phase = PostProcessPhase.AfterBuiltIn)
		{
			postProcessSteps.Add(new DungeonGeneratorPostProcessStep(postProcessCallback, priority, phase));
		}

		/// <summary>
		/// Unregisters an existing post-process step registered using RegisterPostProcessStep()
		/// </summary>
		/// <param name="postProcessCallback">The callback to remove</param>
		public void UnregisterPostProcessStep(Action<DungeonGenerator> postProcessCallback)
		{
			for (int i = 0; i < postProcessSteps.Count; i++)
				if (postProcessSteps[i].PostProcessCallback == postProcessCallback)
					postProcessSteps.RemoveAt(i);
		}

		protected virtual IEnumerator PostProcess()
		{
			GenerationStats.BeginTime(GenerationStatus.PostProcessing);
			ChangeStatus(GenerationStatus.PostProcessing);
			int length = proxyDungeon.MainPathTiles.Count;

			//
			// Need to sort list manually to avoid compilation problems on iOS
			int maxBranchDepth = 0;

			if (proxyDungeon.BranchPathTiles.Count > 0)
			{
				var branchTiles = proxyDungeon.BranchPathTiles.ToList();
				branchTiles.Sort((a, b) =>
				{
					return b.Placement.BranchDepth.CompareTo(a.Placement.BranchDepth);
				});

				maxBranchDepth = branchTiles[0].Placement.BranchDepth;
			}
			// End calculate max branch depth
			//

			// Waiting one frame so objects are in their expected state
			yield return null;


			// Order post-process steps by priority
			postProcessSteps.Sort((a, b) =>
			{
				return b.Priority.CompareTo(a.Priority);
			});

			// Apply any post-process to be run BEFORE built-in post-processing is run
			foreach (var step in postProcessSteps)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				if (step.Phase == PostProcessPhase.BeforeBuiltIn)
					step.PostProcessCallback(this);
			}


			// Waiting one frame so objects are in their expected state
			yield return null;

			foreach (var tile in CurrentDungeon.AllTiles)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				tile.Placement.NormalizedPathDepth = tile.Placement.PathDepth / (float)(length - 1);
			}

			CurrentDungeon.PostGenerateDungeon(this);

			// Process random props
			ProcessLocalProps();
			ProcessGlobalProps();

			if (DungeonFlow.KeyManager != null)
				PlaceLocksAndKeys();

			GenerationStats.SetRoomStatistics(CurrentDungeon.MainPathTiles.Count, CurrentDungeon.BranchPathTiles.Count, maxBranchDepth);
			preProcessData.Clear();


			// Waiting one frame so objects are in their expected state
			yield return null;


			// Apply any post-process to be run AFTER built-in post-processing is run
			foreach (var step in postProcessSteps)
			{
				if (ShouldSkipFrame(false))
					yield return null;

				if (step.Phase == PostProcessPhase.AfterBuiltIn)
					step.PostProcessCallback(this);
			}


			// Finalise
			GenerationStats.EndTime();

			// Activate all door gameobjects that were added to doorways
			foreach (var door in CurrentDungeon.Doors)
				if (door != null)
					door.SetActive(true);
		}

		protected virtual void ProcessLocalProps()
		{
			void GetHierarchyDepth(Transform transform, ref int depth)
			{
				if (transform.parent != null)
				{
					depth++;
					GetHierarchyDepth(transform.parent, ref depth);
				}
			}

			var props = Root.GetComponentsInChildren<RandomProp>();
			var propData = new List<PropProcessingData>();

			foreach (var prop in props)
			{
				int depth = 0;
				GetHierarchyDepth(prop.transform, ref depth);

				propData.Add(new PropProcessingData()
				{
					PropComponent = prop,
					HierarchyDepth = depth,
					OwningTile = prop.GetComponentInParent<Tile>()
				});
			}

			// Loop through props in order of hierarchy depth to ensure a parent prop group is processed before its children
			foreach (var data in propData.OrderBy(x => x.HierarchyDepth))
				data.PropComponent.Process(RandomStream, data.OwningTile);
		}

		protected virtual void ProcessGlobalProps()
		{
			Dictionary<int, GameObjectChanceTable> globalPropWeights = new Dictionary<int, GameObjectChanceTable>();

			foreach (var tile in CurrentDungeon.AllTiles)
			{
				foreach (var prop in tile.GetComponentsInChildren<GlobalProp>())
				{
					GameObjectChanceTable table = null;

					if (!globalPropWeights.TryGetValue(prop.PropGroupID, out table))
					{
						table = new GameObjectChanceTable();
						globalPropWeights[prop.PropGroupID] = table;
					}

					float weight = (tile.Placement.IsOnMainPath) ? prop.MainPathWeight : prop.BranchPathWeight;
					weight *= prop.DepthWeightScale.Evaluate(tile.Placement.NormalizedDepth);

					table.Weights.Add(new GameObjectChance(prop.gameObject, weight, 0, null));
				}
			}

			foreach (var chanceTable in globalPropWeights.Values)
				foreach (var weight in chanceTable.Weights)
					weight.Value.SetActive(false);

			List<int> processedPropGroups = new List<int>(globalPropWeights.Count);

			foreach (var pair in globalPropWeights)
			{
				if (processedPropGroups.Contains(pair.Key))
				{
					Debug.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key + ". Only the first entry will be used.");
					continue;
				}

				var prop = DungeonFlow.GlobalProps.Where(x => x.ID == pair.Key).FirstOrDefault();

				if (prop == null)
					continue;

				var weights = pair.Value.Clone();
				int propCount = prop.Count.GetRandom(RandomStream);
				propCount = Mathf.Clamp(propCount, 0, weights.Weights.Count);

				for (int i = 0; i < propCount; i++)
				{
					var chosenEntry = weights.GetRandom(RandomStream, true, 0, null, true, true);

					if (chosenEntry != null && chosenEntry.Value != null)
						chosenEntry.Value.SetActive(true);
				}

				processedPropGroups.Add(pair.Key);
			}
		}

		protected virtual void PlaceLocksAndKeys()
		{
			var nodes = CurrentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Placement.GraphNode).Where(x => { return x != null; }).Distinct().ToArray();
			var lines = CurrentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Placement.GraphLine).Where(x => { return x != null; }).Distinct().ToArray();

			Dictionary<Doorway, Key> lockedDoorways = new Dictionary<Doorway, Key>();

			// Lock doorways on nodes
			foreach (var node in nodes)
			{
				foreach (var l in node.Locks)
				{
					var tile = CurrentDungeon.AllTiles.Where(x => { return x.Placement.GraphNode == node; }).FirstOrDefault();
					var connections = CurrentDungeon.ConnectionGraph.Nodes.Where(x => { return x.Tile == tile; }).FirstOrDefault().Connections;
					Doorway entrance = null;
					Doorway exit = null;

					foreach (var conn in connections)
					{
						if (conn.DoorwayA.Tile == tile)
							exit = conn.DoorwayA;
						else if (conn.DoorwayB.Tile == tile)
							entrance = conn.DoorwayB;
					}

					var key = node.Graph.KeyManager.GetKeyByID(l.ID);

					if (entrance != null && (node.LockPlacement & NodeLockPlacement.Entrance) == NodeLockPlacement.Entrance)
						lockedDoorways.Add(entrance, key);

					if (exit != null && (node.LockPlacement & NodeLockPlacement.Exit) == NodeLockPlacement.Exit)
						lockedDoorways.Add(exit, key);
				}
			}

			// Lock doorways on lines
			foreach (var line in lines)
			{
				var doorways = CurrentDungeon.ConnectionGraph.Connections.Where(x =>
				{
					bool isDoorwayAlreadyLocked = lockedDoorways.ContainsKey(x.DoorwayA) || lockedDoorways.ContainsKey(x.DoorwayB);
					bool doorwayHasLockPrefabs = x.DoorwayA.Tile.Placement.TileSet.LockPrefabs.Count > 0;

					return x.DoorwayA.Tile.Placement.GraphLine == line &&
							x.DoorwayB.Tile.Placement.GraphLine == line &&
							!isDoorwayAlreadyLocked &&
							doorwayHasLockPrefabs;

				}).Select(x => x.DoorwayA).ToList();

				if (doorways.Count == 0)
					continue;

				foreach (var l in line.Locks)
				{
					int lockCount = l.Range.GetRandom(RandomStream);
					lockCount = Mathf.Clamp(lockCount, 0, doorways.Count);

					for (int i = 0; i < lockCount; i++)
					{
						if (doorways.Count == 0)
							break;

						var doorway = doorways[RandomStream.Next(0, doorways.Count)];
						doorways.Remove(doorway);

						if (lockedDoorways.ContainsKey(doorway))
							continue;

						var key = line.Graph.KeyManager.GetKeyByID(l.ID);
						lockedDoorways.Add(doorway, key);
					}
				}
			}


			// Lock doorways on injected tiles
			foreach (var tile in CurrentDungeon.AllTiles)
			{
				if (tile.Placement.InjectionData != null && tile.Placement.InjectionData.IsLocked)
				{
					var validLockedDoorways = new List<Doorway>();

					foreach (var doorway in tile.UsedDoorways)
					{
						bool isDoorwayAlreadyLocked = lockedDoorways.ContainsKey(doorway) || lockedDoorways.ContainsKey(doorway.ConnectedDoorway);
						bool doorwayHasLockPrefabs = tile.Placement.TileSet.LockPrefabs.Count > 0;
						bool isEntranceDoorway = tile.GetEntranceDoorway() == doorway;

						if (!isDoorwayAlreadyLocked &&
							doorwayHasLockPrefabs &&
							isEntranceDoorway)
						{
							validLockedDoorways.Add(doorway);
						}
					}

					if (validLockedDoorways.Any())
					{
						var doorway = validLockedDoorways.First();
						var key = DungeonFlow.KeyManager.GetKeyByID(tile.Placement.InjectionData.LockID);

						lockedDoorways.Add(doorway, key);
					}
				}
			}

			var locksToRemove = new List<Doorway>();
			var usedSpawnComponents = new List<IKeySpawnable>();

			foreach (var pair in lockedDoorways)
			{
				var doorway = pair.Key;
				var key = pair.Value;
				var possibleSpawnTiles = new List<Tile>();

				foreach (var t in CurrentDungeon.AllTiles)
				{
					if (t.Placement.NormalizedPathDepth > doorway.Tile.Placement.NormalizedPathDepth)
						continue;

					bool canPlaceKey = false;

					if (t.Placement.GraphNode != null && t.Placement.GraphNode.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
						canPlaceKey = true;
					else if (t.Placement.GraphLine != null && t.Placement.GraphLine.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
						canPlaceKey = true;

					if (!canPlaceKey)
						continue;

					possibleSpawnTiles.Add(t);
				}

				var possibleSpawnComponents = possibleSpawnTiles.SelectMany(x => x.GetComponentsInChildren<Component>().OfType<IKeySpawnable>()).Except(usedSpawnComponents).ToList();

				if (possibleSpawnComponents.Count == 0)
					locksToRemove.Add(doorway);
				else
				{
					doorway.LockID = key.ID;

					int keysToSpawn = key.KeysPerLock.GetRandom(RandomStream);
					keysToSpawn = Math.Min(keysToSpawn, possibleSpawnComponents.Count);

					for (int i = 0; i < keysToSpawn; i++)
					{
						int chosenCompID = RandomStream.Next(0, possibleSpawnComponents.Count);
						var comp = possibleSpawnComponents[chosenCompID];
						comp.SpawnKey(key, DungeonFlow.KeyManager);

						foreach (var k in (comp as Component).GetComponentsInChildren<Component>().OfType<IKeyLock>())
							k.OnKeyAssigned(key, DungeonFlow.KeyManager);

						usedSpawnComponents.Add(comp);
					}
				}
			}

			foreach (var doorway in locksToRemove)
			{
				doorway.LockID = null;
				lockedDoorways.Remove(doorway);
			}

			foreach (var pair in lockedDoorways)
			{
				pair.Key.RemoveUsedPrefab();
				LockDoorway(pair.Key, pair.Value, DungeonFlow.KeyManager);
			}
		}

		protected virtual void LockDoorway(Doorway doorway, Key key, KeyManager keyManager)
		{
			var placement = doorway.Tile.Placement;
			var prefabs = doorway.Tile.Placement.TileSet.LockPrefabs.Where(x =>
			{
				if (x == null || x.LockPrefabs == null)
					return false;

				if (!x.LockPrefabs.HasAnyValidEntries(placement.IsOnMainPath, placement.NormalizedDepth, null, true))
					return false;

				var lockSocket = x.Socket;

				if (lockSocket == null)
					return true;
				else
					return DoorwaySocket.CanSocketsConnect(lockSocket, doorway.Socket);

			}).Select(x => x.LockPrefabs).ToArray();

			if (prefabs.Length == 0)
				return;

			var chosenEntry = prefabs[RandomStream.Next(0, prefabs.Length)].GetRandom(RandomStream, placement.IsOnMainPath, placement.NormalizedDepth, null, true);
			var prefab = chosenEntry.Value;

			GameObject doorObj = GameObject.Instantiate(prefab, doorway.transform);

			DungeonUtil.AddAndSetupDoorComponent(CurrentDungeon, doorObj, doorway);

			// Set this locked door as the current door prefab
			doorway.SetUsedPrefab(doorObj);
			doorway.ConnectedDoorway.SetUsedPrefab(doorObj);

			foreach (var keylock in doorObj.GetComponentsInChildren<Component>().OfType<IKeyLock>())
				keylock.OnKeyAssigned(key, keyManager);
		}

		#region ISerializationCallbackReceiver Implementation

		public void OnBeforeSerialize()
		{
			fileVersion = CurrentFileVersion;
		}

		public void OnAfterDeserialize()
		{
			if (fileVersion < 1)
				RepeatMode = (allowImmediateRepeats) ? TileRepeatMode.Allow : TileRepeatMode.DisallowImmediate;
		}

		#endregion
	}
}
