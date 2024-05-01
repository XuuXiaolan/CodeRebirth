using DunGen.Tags;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public sealed class DoorwayProxy
	{
		public bool Used { get { return ConnectedDoorway != null; } }
		public TileProxy TileProxy { get; private set; }
		public int Index { get; private set; }
		public DoorwaySocket Socket { get; private set; }
		public Doorway DoorwayComponent { get; private set; }
		public Vector3 LocalPosition { get; private set; }
		public Quaternion LocalRotation { get; private set; }
		public DoorwayProxy ConnectedDoorway { get; private set; }
		public Vector3 Forward { get { return (TileProxy.Placement.Rotation * LocalRotation) * Vector3.forward; } }
		public Vector3 Up { get { return (TileProxy.Placement.Rotation * LocalRotation) * Vector3.up; } }
		public Vector3 Position { get { return TileProxy.Placement.Transform.MultiplyPoint(LocalPosition); } }
		public TagContainer Tags { get; private set; }


		public DoorwayProxy(TileProxy tileProxy, DoorwayProxy other)
		{
			TileProxy = tileProxy;
			Index = other.Index;
			Socket = other.Socket;
			DoorwayComponent = other.DoorwayComponent;
			LocalPosition = other.LocalPosition;
			LocalRotation = other.LocalRotation;
			Tags = new TagContainer(other.Tags);
		}

		public DoorwayProxy(TileProxy tileProxy, int index, Doorway doorwayComponent, Vector3 localPosition, Quaternion localRotation)
		{
			TileProxy = tileProxy;
			Index = index;
			Socket = doorwayComponent.Socket;
			DoorwayComponent = doorwayComponent;
			LocalPosition = localPosition;
			LocalRotation = localRotation;
		}

		public static void Connect(DoorwayProxy a, DoorwayProxy b)
		{
			Debug.Assert(a.ConnectedDoorway == null, "Doorway 'a' is already connected to something");
			Debug.Assert(b.ConnectedDoorway == null, "Doorway 'b' is already connected to something");

			a.ConnectedDoorway = b;
			b.ConnectedDoorway = a;
		}

		public void Disconnect()
		{
			if (ConnectedDoorway == null)
				return;

			ConnectedDoorway.ConnectedDoorway = null;
			ConnectedDoorway = null;
		}
	}

	public sealed class TileProxy
	{
		public GameObject Prefab { get; private set; }
		public Tile PrefabTile { get; private set; }
		public TilePlacementData Placement { get; internal set; }
		public DoorwayProxy Entrance { get; private set; }
		public DoorwayProxy Exit { get; private set; }
		public ReadOnlyCollection<DoorwayProxy> Doorways { get; private set; }
		public IEnumerable<DoorwayProxy> UsedDoorways { get { return doorways.Where(d => d.Used); } }
		public IEnumerable<DoorwayProxy> UnusedDoorways { get { return doorways.Where(d => !d.Used); } }
		public TagContainer Tags { get; private set; }

		private List<DoorwayProxy> doorways = new List<DoorwayProxy>();


		public TileProxy(TileProxy existingTile)
		{
			Prefab = existingTile.Prefab;
			PrefabTile = existingTile.PrefabTile;
			Placement = new TilePlacementData(existingTile.Placement);
			Tags = new TagContainer(existingTile.Tags);

			// Copy proxy doorways
			Doorways = new ReadOnlyCollection<DoorwayProxy>(doorways);

			foreach(var existingDoorway in existingTile.doorways)
			{
				var doorway = new DoorwayProxy(this, existingDoorway);
				doorways.Add(doorway);

				if (existingTile.Entrance == existingDoorway)
					Entrance = doorway;
				if (existingTile.Exit == existingDoorway)
					Exit = doorway;
			}
		}

		public TileProxy(GameObject prefab, bool ignoreSpriteRendererBounds, Vector3 upVector)
		{
			prefab.transform.localPosition = Vector3.zero;
			prefab.transform.localRotation = Quaternion.identity;

			Prefab = prefab;
			PrefabTile = prefab.GetComponent<Tile>();

			if (PrefabTile == null)
				PrefabTile = prefab.AddComponent<Tile>();

			Placement = new TilePlacementData();
			Tags = new TagContainer(PrefabTile.Tags);

			// Add proxy doorways
			Doorways = new ReadOnlyCollection<DoorwayProxy>(doorways);

			var allDoorways = prefab.GetComponentsInChildren<Doorway>();
			for (int i = 0; i < allDoorways.Length; i++)
			{
				var doorway = allDoorways[i];

				Vector3 localPosition = doorway.transform.position;
				Quaternion localRotation = doorway.transform.rotation;

				var proxyDoorway = new DoorwayProxy(this, i, doorway, localPosition, localRotation);
				doorways.Add(proxyDoorway);

				if (PrefabTile.Entrance == doorway)
					Entrance = proxyDoorway;
				if (PrefabTile.Exit == doorway)
					Exit = proxyDoorway;
			}

			// Calculate bounds
			Bounds bounds;

			if (PrefabTile != null && PrefabTile.OverrideAutomaticTileBounds)
				bounds = PrefabTile.TileBoundsOverride;
			else
				bounds = UnityUtil.CalculateProxyBounds(Prefab, ignoreSpriteRendererBounds, upVector);

			// Let the user know if the automatically calculated bounds are incorrect
			if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
				Debug.LogError(string.Format("Tile prefab '{0}' has automatic bounds that are zero or negative in size. The bounding volume for this tile will need to be manually defined.", prefab), prefab);

			Placement.LocalBounds = UnityUtil.CondenseBounds(bounds, Prefab.GetComponentsInChildren<Doorway>());
		}

		public void PositionBySocket(DoorwayProxy myDoorway, DoorwayProxy otherDoorway)
		{
			Quaternion targetRotation = Quaternion.LookRotation(-otherDoorway.Forward, otherDoorway.Up);
			Placement.Rotation = targetRotation * Quaternion.Inverse(Quaternion.Inverse(Placement.Rotation) * (Placement.Rotation * myDoorway.LocalRotation));

			Vector3 targetPosition = otherDoorway.Position;
			Placement.Position = targetPosition - (myDoorway.Position - Placement.Position);
		}

		private Vector3 CalculateOverlap(TileProxy other)
		{
			var boundsA = Placement.Bounds;
			var boundsB = other.Placement.Bounds;

			float overlapPx = boundsA.max.x - boundsB.min.x;
			float overlapNx = boundsB.max.x - boundsA.min.x;
			float overlapPy = boundsA.max.y - boundsB.min.y;
			float overlapNy = boundsB.max.y - boundsA.min.y;
			float overlapPz = boundsA.max.z - boundsB.min.z;
			float overlapNz = boundsB.max.z - boundsA.min.z;

			return new Vector3(	Mathf.Min(overlapPx, overlapNx),
								Mathf.Min(overlapPy, overlapNy),
								Mathf.Min(overlapPz, overlapNz));
		}

		public bool IsOverlapping(TileProxy other, float maxOverlap)
		{
			Vector3 overlap = CalculateOverlap(other);
			return Mathf.Min(overlap.x, overlap.y, overlap.z) > maxOverlap;
		}

		public bool IsOverlappingOrOverhanging(TileProxy other, AxisDirection upDirection, float maxOverlap)
		{
			Vector3 overlaps = UnityUtil.CalculatePerAxisOverlap(other.Placement.Bounds, Placement.Bounds);
			float overlap;

			// Check for overlaps only along the ground plane, disregarding the up-axis
			// E.g. For +Y up, check for overlaps along X & Z axes
			switch (upDirection)
			{
				case AxisDirection.PosX:
				case AxisDirection.NegX:
					overlap = Mathf.Min(overlaps.y, overlaps.z);
					break;

				case AxisDirection.PosY:
				case AxisDirection.NegY:
					overlap = Mathf.Min(overlaps.x, overlaps.z);
					break;

				case AxisDirection.PosZ:
				case AxisDirection.NegZ:
					overlap = Mathf.Min(overlaps.x, overlaps.y);
					break;

				default:
					throw new NotImplementedException("AxisDirection '" + upDirection + "' is not implemented");
			}

			return overlap > maxOverlap;
		}
	}
}
