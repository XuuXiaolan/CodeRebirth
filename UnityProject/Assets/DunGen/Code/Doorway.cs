using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using DunGen.Tags;
using System.Linq;

namespace DunGen
{
	/// <summary>
	/// A component to handle doorway placement and behaviour
	/// </summary>
	[AddComponentMenu("DunGen/Doorway")]
	public class Doorway : MonoBehaviour, ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 1;

		public bool HasSocketAssigned { get { return socket != null; } }

		/// <summary>
		/// The socket this doorway uses. Allows you to use different sized doorways and have them connect correctly
		/// </summary>
		public DoorwaySocket Socket { get { return (socket != null) ? socket : DunGenSettings.Instance.DefaultSocket; } }
		/// <summary>
		/// When placing a door prefab, the doorway with the higher priority will have their prefab used
		/// </summary>
		public int DoorPrefabPriority;
		/// <summary>
		/// When this doorway is in use, a prefab will be picked at random from this list and is spawned at the doorway location - one per doorways pair (connection)
		/// </summary>
		public List<GameObjectWeight> ConnectorPrefabWeights = new List<GameObjectWeight>();
		/// <summary>
		/// When this doorway is NOT in use, a prefab will be picked at random from this list and is spawned at the doorway location - one per doorway
		/// </summary>
		public List<GameObjectWeight> BlockerPrefabWeights = new List<GameObjectWeight>();
		/// <summary>
		/// If true, the chosen Door prefab will not be oriented to match the rotation of the doorway it is placed on
		/// </summary>
		public bool AvoidRotatingDoorPrefab;
		/// <summary>
		/// If true, the chosen Blocker prefab will not be oriented to match the rotation of the doorway it is placed on
		/// </summary>
		public bool AvoidRotatingBlockerPrefab;
		/// <summary>
		/// When this doorway is in use, objects in this list will remain in the scene, otherwise, they are destroyed
		/// </summary>
		[FormerlySerializedAs("AddWhenInUse")]
		public List<GameObject> ConnectorSceneObjects = new List<GameObject>();
		/// <summary>
		/// When this doorway is NOT in use, objects in this list will remain in the scene, otherwise, they are destroyed
		/// </summary>
		[FormerlySerializedAs("AddWhenNotInUse")]
		public List<GameObject> BlockerSceneObjects = new List<GameObject>();
		/// <summary>
		/// A collection of tags for this doorway. These can be used in code with DoorwayPairFinder.CustomConnectionRules for custom connection logic
		/// </summary>
		public TagContainer Tags = new TagContainer();
		/// <summary>
		/// The Tile that this doorway belongs to
		/// </summary>
		public Tile Tile { get { return tile; } internal set { tile = value; } }
		/// <summary>
		/// The ID of the key used to unlock this door
		/// </summary>
		public int? LockID;
		/// <summary>
		/// Gets the lock status of the door
		/// </summary>
		public bool IsLocked { get { return LockID.HasValue; } }
		/// <summary>
		/// Does this doorway have a prefab object placed as a door?
		/// </summary>
		public bool HasDoorPrefabInstance { get { return doorPrefabInstance != null; } }
		/// <summary>
		/// The prefab that has been placed as a door for this doorway
		/// </summary>
		public GameObject UsedDoorPrefabInstance { get { return doorPrefabInstance; } }
		/// <summary>
		/// The Door component that has been assigned to the door prefab instance (if any)
		/// </summary>
		public Door DoorComponent { get { return doorComponent; } }
		/// <summary>
		/// The dungeon that this doorway belongs to
		/// </summary>
		public Dungeon Dungeon { get; internal set; }
		/// <summary>
		/// The doorway that this is connected to
		/// </summary>
		public Doorway ConnectedDoorway { get { return connectedDoorway; } internal set { connectedDoorway = value; } }
		/// <summary>
		/// Allows for hiding of any GameObject in the "AddWhenInUse" and "AddWhenNotInUse" lists - used to remove clutter at design-time; should not be used at runtime
		/// </summary>
		public bool HideConditionalObjects
		{
			get { return hideConditionalObjects; }
			set
			{
				hideConditionalObjects = value;

				foreach (var obj in ConnectorSceneObjects)
					if (obj != null)
						obj.SetActive(!hideConditionalObjects);

				foreach (var obj in BlockerSceneObjects)
					if (obj != null)
						obj.SetActive(!hideConditionalObjects);
			}
		}

		#region Legacy Properties

#pragma warning disable 0414
		[SerializeField]
		[FormerlySerializedAs("SocketGroup")]
		private DoorwaySocketType socketGroup_obsolete = (DoorwaySocketType)(-1);

		[SerializeField]
		[FormerlySerializedAs("DoorPrefabs")]
		private List<GameObject> doorPrefabs_obsolete = new List<GameObject>();

		[SerializeField]
		[FormerlySerializedAs("BlockerPrefabs")]
		private List<GameObject> blockerPrefabs_obsolete = new List<GameObject>();

#pragma warning restore 0414

		#endregion

		[SerializeField]
		private DoorwaySocket socket = null;
		[SerializeField]
		private GameObject doorPrefabInstance;
		[SerializeField]
		private Door doorComponent;
		[SerializeField]
		private Tile tile;
		[SerializeField]
		private Doorway connectedDoorway;
		[SerializeField]
		private bool hideConditionalObjects;
		[SerializeField]
		private int fileVersion;

		internal bool placedByGenerator;


		internal void SetUsedPrefab(GameObject doorPrefab)
		{
			this.doorPrefabInstance = doorPrefab;

			if (doorPrefab != null)
				doorComponent = doorPrefab.GetComponent<Door>();
		}

		internal void RemoveUsedPrefab()
		{
			if (doorPrefabInstance != null)
				UnityUtil.Destroy(doorPrefabInstance);

			doorPrefabInstance = null;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (!placedByGenerator)
				DebugDraw();
		}

		internal void DebugDraw()
		{
			Vector2 size = Socket.Size;
			Vector2 halfSize = size * 0.5f;

			bool isValidPlacement = true;
			Color doorwayColour = Color.white;

			isValidPlacement = ValidateTransform(out var tileBounds, out bool isAxisAligned, out bool isEdgePositioned);

			bool isSelected = UnityEditor.Selection.gameObjects.Contains(gameObject);

			if (isSelected)
			{
				if (isValidPlacement)
					doorwayColour = EditorConstants.DoorRectColourValid;
				else if (!isAxisAligned)
					doorwayColour = EditorConstants.DoorRectColourError;
				else
					doorwayColour = EditorConstants.DoorRectColourWarning;
			}
			else
				doorwayColour = EditorConstants.DoorRectColourValid;


			// Draw Forward Vector
			float lineLength = Mathf.Min(size.x, size.y);

			Gizmos.color = EditorConstants.DoorDirectionColour;
			Gizmos.DrawLine(transform.position + transform.up * halfSize.y, transform.position + transform.up * halfSize.y + transform.forward * lineLength);


			// Draw Up Vector
			Gizmos.color = EditorConstants.DoorUpColour;
			Gizmos.DrawLine(transform.position + transform.up * halfSize.y, transform.position + transform.up * size.y);


			// Draw Rectangle
			Gizmos.color = doorwayColour;
			Vector3 topLeft = transform.position - (transform.right * halfSize.x) + (transform.up * size.y);
			Vector3 topRight = transform.position + (transform.right * halfSize.x) + (transform.up * size.y);
			Vector3 bottomLeft = transform.position - (transform.right * halfSize.x);
			Vector3 bottomRight = transform.position + (transform.right * halfSize.x);

			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(topRight, bottomRight);
			Gizmos.DrawLine(bottomRight, bottomLeft);
			Gizmos.DrawLine(bottomLeft, topLeft);


			// Draw position correction line
			if (!isValidPlacement)
			{
				Vector3 projectedPosition = ProjectPositionToTileBounds(tileBounds);

				Gizmos.color = Color.red;
				Gizmos.DrawLine(transform.position, projectedPosition);
			}
		}
#endif

		private void GetTileRoot(out GameObject tileRoot, out Tile tileComponent)
		{
			tileComponent = GetComponentInParent<Tile>();

			if (tileComponent != null)
				tileRoot = tileComponent.gameObject;
			else
				tileRoot = transform.root.gameObject;
		}

		public bool ValidateTransform(out Bounds tileBounds, out bool isAxisAligned, out bool isEdgePositioned)
		{
			GetTileRoot(out var tileRoot, out var tile);

			isAxisAligned = true;
			isEdgePositioned = true;

			if (tile != null && tile.OverrideAutomaticTileBounds)
			{
				tileBounds = tile.TileBoundsOverride;
				tileBounds.center += tileRoot.transform.position;
			}
			else
				tileBounds = UnityUtil.CalculateObjectBounds(tileRoot, false, false);

			if(!UnityUtil.IsVectorAxisAligned(transform.forward))
				isAxisAligned = false;

			Vector3 projectedPosition = ProjectPositionToTileBounds(tileBounds);

			if ((projectedPosition - transform.position).magnitude > 0.1f)
				isEdgePositioned = false;

			return isAxisAligned && isEdgePositioned;
		}

		public void TrySnapToCorrectedTransform()
		{
			if (ValidateTransform(out var tileBounds, out _, out _))
				return;

			Vector3 correctedForward = UnityUtil.GetCardinalDirection(transform.forward, out _);

			transform.forward = correctedForward;
			transform.position = ProjectPositionToTileBounds(tileBounds);
		}

		public Vector3 ProjectPositionToTileBounds(Bounds tileBounds)
		{
			Vector3 correctedForward = UnityUtil.GetCardinalDirection(transform.forward, out var magnitude);
			Vector3 offsetFromBoundsCenter = transform.position - tileBounds.center;

			// Calculate correction distance along forward vector (snap to edge)
			float currentForwardDistance = Vector3.Dot(correctedForward, offsetFromBoundsCenter);
			float extentForwardDistance = Vector3.Dot(magnitude < 0 ? -correctedForward : correctedForward, tileBounds.extents);
			float forwardCorrectionDistance = extentForwardDistance - currentForwardDistance;

			Vector3 targetPosition = transform.position;
			targetPosition += correctedForward * forwardCorrectionDistance;

			// Once we're positioned on the correct side of the bounding box based on the forward vector
			// of the doorway, clamp the position to keep it restrained within the bounds along the other axes
			targetPosition = UnityUtil.ClampVector(targetPosition, tileBounds.min, tileBounds.max);

			return targetPosition;
		}

		#region ISerializationCallbackReceiver Implementation

		public void OnBeforeSerialize()
		{
			fileVersion = CurrentFileVersion;
		}

		public void OnAfterDeserialize()
		{
			// Convert old object lists to weighted lists
			if (fileVersion < 1)
			{
				foreach (var obj in doorPrefabs_obsolete)
					ConnectorPrefabWeights.Add(new GameObjectWeight(obj));

				foreach (var obj in blockerPrefabs_obsolete)
					BlockerPrefabWeights.Add(new GameObjectWeight(obj));

				doorPrefabs_obsolete.Clear();
				blockerPrefabs_obsolete.Clear();
			}
		}

		#endregion
	}
}