using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DunGen
{
	public static class UnityUtil
	{
		#region Reflection

		public static Type ProBuilderMeshType { get; private set; }
		public static PropertyInfo ProBuilderPositionsProperty { get; private set; }


		static UnityUtil()
		{
			FindProBuilderObjectType();
		}

		public static void FindProBuilderObjectType()
		{
			if (ProBuilderMeshType != null)
				return;

			// Look through each of the loaded assemblies in our current AppDomain, looking for ProBuilder's pb_Object type
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.FullName.Contains("ProBuilder"))
				{
					ProBuilderMeshType = assembly.GetType("UnityEngine.ProBuilder.ProBuilderMesh");

					if (ProBuilderMeshType != null)
					{
						ProBuilderPositionsProperty = ProBuilderMeshType.GetProperty("positions");

						if (ProBuilderPositionsProperty != null)
							break;
					}
				}
			}
		}

		#endregion


		public static void Restart(this System.Diagnostics.Stopwatch stopwatch)
		{
			if (stopwatch == null)
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
			else
			{
				stopwatch.Reset();
				stopwatch.Start();
			}
		}

		public static bool Contains(this Bounds bounds, Bounds other)
		{
			if (other.min.x < bounds.min.x || other.min.y < bounds.min.y || other.min.z < bounds.min.z ||
				other.max.x > bounds.max.x || other.max.y > bounds.max.y || other.max.z > bounds.max.z)
				return false;

			return true;
		}

		public static Bounds TransformBounds(this Transform transform, Bounds localBounds)
		{
			Vector3 transformedCenter = transform.TransformPoint(localBounds.center);
			Vector3 transformedSize = transform.rotation * localBounds.size;

			transformedSize.x = Mathf.Abs(transformedSize.x);
			transformedSize.y = Mathf.Abs(transformedSize.y);
			transformedSize.z = Mathf.Abs(transformedSize.z);

			return new Bounds(transformedCenter, transformedSize);
		}

		public static Bounds InverseTransformBounds(this Transform transform, Bounds worldBounds)
		{
			Vector3 transformedCenter = transform.InverseTransformPoint(worldBounds.center);
			Vector3 transformedSize = Quaternion.Inverse(transform.rotation) * worldBounds.size;

			transformedSize.x = Mathf.Abs(transformedSize.x);
			transformedSize.y = Mathf.Abs(transformedSize.y);
			transformedSize.z = Mathf.Abs(transformedSize.z);

			return new Bounds(transformedCenter, transformedSize);
		}

		public static void SetLayerRecursive(GameObject gameObject, int layer)
		{
			gameObject.layer = layer;

			for (int i = 0; i < gameObject.transform.childCount; i++)
				SetLayerRecursive(gameObject.transform.GetChild(i).gameObject, layer);
		}

		public static void Destroy(UnityEngine.Object obj)
		{
			if (Application.isPlaying)
			{
				// Work-Around
				// If we're destroying a GameObject, disable it first to avoid tile colliders from contributing to the NavMesh when generating synchronously
				// since Destroy() only destroys the GameObject at the end of the frame. Are there any down-sides to using DestroyImmediate() here instead?
				GameObject go = obj as GameObject;

				if (go != null)
					go.SetActive(false);

				UnityEngine.Object.Destroy(obj);
			}
			else
				UnityEngine.Object.DestroyImmediate(obj);
		}

		public static string GetUniqueName(string name, IEnumerable<string> usedNames)
		{
			if(string.IsNullOrEmpty(name))
				return GetUniqueName("New", usedNames);
			
			string baseName = name;
			int number = 0;
			bool hasNumber = false;

			int indexOfLastSeperator = name.LastIndexOf(' ');

			if(indexOfLastSeperator > -1)
			{
				baseName = name.Substring(0, indexOfLastSeperator);
				hasNumber = int.TryParse(name.Substring(indexOfLastSeperator + 1), out number);
				number++;
			}

			foreach(var n in usedNames)
			{
				if(n == name)
				{
					if(hasNumber)
						return GetUniqueName(baseName + " " + number.ToString(), usedNames);
					else
						return GetUniqueName(name + " 2", usedNames);
				}
			}
			
			return name;
		}

		public static Bounds CombineBounds(params Bounds[] bounds)
		{
			if (bounds.Length == 0)
				return new Bounds();
			else if (bounds.Length == 1)
				return bounds[0];

			Bounds combinedBounds = bounds[0];

			for (int i = 1; i < bounds.Length; i++)
				combinedBounds.Encapsulate(bounds[i]);

			return combinedBounds;
		}

		public static Bounds CalculateProxyBounds(GameObject prefab, bool ignoreSpriteRendererBounds, Vector3 upVector)
		{
			var bounds = UnityUtil.CalculateObjectBounds(prefab, true, ignoreSpriteRendererBounds);

			// Since ProBuilder objects don't have a mesh until they're instantiated, we have to calculate the bounds manually
			if (ProBuilderMeshType != null && ProBuilderPositionsProperty != null)
			{
				foreach (var pbMesh in prefab.GetComponentsInChildren(ProBuilderMeshType))
				{
					Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
					Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

					var vertices = (IList<Vector3>)ProBuilderPositionsProperty.GetValue(pbMesh, null);

					foreach (var vert in vertices)
					{
						min = Vector3.Min(min, vert);
						max = Vector3.Max(max, vert);
					}

					Vector3 size = prefab.transform.TransformDirection(max - min);
					Vector3 center = prefab.transform.TransformPoint(min) + size / 2;

					bounds.Encapsulate(new Bounds(center, size));
				}
			}

			return bounds;
		}

		public static Bounds CalculateObjectBounds(GameObject obj, bool includeInactive, bool ignoreSpriteRenderers, bool ignoreTriggerColliders = true)
		{
			Bounds bounds = new Bounds();
			bool hasBounds = false;


			// We need to compress the bounds of a tilemap first or the renderer will return bounds that are too big
			foreach (var tilemap in obj.GetComponentsInChildren<Tilemap>(includeInactive))
				tilemap.CompressBounds();


			// Renderers
			foreach (var renderer in obj.GetComponentsInChildren<Renderer>(includeInactive))
			{
				if (ignoreSpriteRenderers && renderer is SpriteRenderer)
					continue;
				if (renderer is ParticleSystemRenderer)
					continue;

				if (hasBounds)
					bounds.Encapsulate(renderer.bounds);
				else
					bounds = renderer.bounds;

				hasBounds = true;
			}

			// Colliders
			foreach (var collider in obj.GetComponentsInChildren<Collider>(includeInactive))
			{
				// Terrain colliders report incorrect bounds when not placed in the scene
				if (collider is TerrainCollider)
					continue;

				if (ignoreTriggerColliders && collider.isTrigger)
					continue;

				if (hasBounds)
					bounds.Encapsulate(collider.bounds);
				else
					bounds = collider.bounds;

				hasBounds = true;
			}

			// Terrain
			foreach(var terrain in obj.GetComponentsInChildren<Terrain>(includeInactive))
			{
				var terrainBounds = terrain.terrainData.bounds;
				terrainBounds.center += terrain.gameObject.transform.position;

				if (hasBounds)
					bounds.Encapsulate(terrainBounds);
				else
					bounds = terrainBounds;

				hasBounds = true;
			}

			// Fix any zero or negative extents
			const float minExtents = 0.01f;
			Vector3 extents = bounds.extents;

			if (extents.x == 0f)
				extents.x = minExtents;
			else if (extents.x < 0f)
				extents.x *= -1f;

			if (extents.y == 0f)
				extents.y = minExtents;
			else if (extents.y < 0f)
				extents.y *= -1f;

			if (extents.z == 0f)
				extents.z = minExtents;
			else if (extents.z < 0f)
				extents.z *= -1f;

			bounds.extents = extents;
			return bounds;
		}

		/// <summary>
		/// Positions an object by aligning one of it's own sockets to the socket of another object
		/// </summary>
		/// <param name="objectA">The object to move</param>
		/// <param name="socketA">A socket for the object that we want to move (must be a child somewhere in the object's hierarchy)</param>
		/// <param name="socketB">The socket we want to attach the object to (must not be a child in the object's hierarchy)</param>
		public static void PositionObjectBySocket(GameObject objectA, GameObject socketA, GameObject socketB)
		{
			PositionObjectBySocket(objectA.transform, socketA.transform, socketB.transform);
		}

		/// <summary>
		/// Positions an object by aligning one of it's own sockets to the socket of another object
		/// </summary>
		/// <param name="objectA">The object to move</param>
		/// <param name="socketA">A socket for the object that we want to move (must be a child somewhere in the object's hierarchy)</param>
		/// <param name="socketB">The socket we want to attach the object to (must not be a child in the object's hierarchy)</param>
		public static void PositionObjectBySocket(Transform objectA, Transform socketA, Transform socketB)
		{
			Quaternion targetRotation = Quaternion.LookRotation(-socketB.forward, socketB.up);
			objectA.rotation = targetRotation * Quaternion.Inverse(Quaternion.Inverse(objectA.rotation) * socketA.rotation);

			Vector3 targetPosition = socketB.position;
			objectA.position = targetPosition - (socketA.position - objectA.position);
		}

		public static bool IsVectorAxisAligned(Vector3 direction)
		{
			float dotX = Mathf.Abs(Vector3.Dot(direction, new Vector3(1, 0, 0)));
			float dotY = Mathf.Abs(Vector3.Dot(direction, new Vector3(0, 1, 0)));
			float dotZ = Mathf.Abs(Vector3.Dot(direction, new Vector3(0, 0, 1)));

			const float epsilon = 0.01f;

			if (dotX > 1 - epsilon && dotY < epsilon && dotZ < epsilon)
				return true;
			if (dotY > 1 - epsilon && dotX < epsilon && dotZ < epsilon)
				return true;
			if (dotZ > 1 - epsilon && dotX < epsilon && dotY < epsilon)
				return true;

			return false;
		}

		public static Vector3 GetCardinalDirection(Vector3 direction, out float magnitude)
		{
			float absX = Math.Abs(direction.x);
			float absY = Math.Abs(direction.y);
			float absZ = Math.Abs(direction.z);

			float dirX = direction.x / absX;
			float dirY = direction.y / absY;
			float dirZ = direction.z / absZ;

			if (absX > absY && absX > absZ)
			{
				magnitude = dirX;
				return new Vector3(dirX, 0, 0);
			}
			else if (absY > absX && absY > absZ)
			{
				magnitude = dirY;
				return new Vector3(0, dirY, 0);
			}
			else if (absZ > absX && absZ > absY)
			{
				magnitude = dirZ;
				return new Vector3(0, 0, dirZ);
			}
			else
			{
				magnitude = dirX;
				return new Vector3(dirX, 0, 0);
			}
		}

		public static Vector3 VectorAbs(Vector3 vector)
		{
			return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
		}

		public static void SetVector3Masked(ref Vector3 input, Vector3 value, Vector3 mask)
		{
			if (mask.x != 0)
				input.x = value.x;
			if (mask.y != 0)
				input.y = value.y;
			if (mask.z != 0)
				input.z = value.z;
		}

		public static Vector3 ClampVector(Vector3 input, Vector3 min, Vector3 max)
		{
			return new Vector3(
				input.x < min.x ? min.x : input.x > max.x ? max.x : input.x,
				input.y < min.y ? min.y : input.y > max.y ? max.y : input.y,
				input.z < min.z ? min.z : input.z > max.z ? max.z : input.z
				);
		}

		public static Bounds CondenseBounds(Bounds bounds, IEnumerable<Doorway> doorways)
		{
			Vector3 min = bounds.center - bounds.extents;
			Vector3 max = bounds.center + bounds.extents;

			foreach(var doorway in doorways)
			{
				float magnitude;
				Vector3 dir = UnityUtil.GetCardinalDirection(doorway.transform.forward, out magnitude);

				if (magnitude < 0)
					SetVector3Masked(ref min, doorway.transform.position, dir);
				else
					SetVector3Masked(ref max, doorway.transform.position, dir);
			}

			Vector3 size = max - min;
			Vector3 center = min + (size / 2);

			return new Bounds(center, size);
		}

		public static IEnumerable<T> GetComponentsInParents<T>(GameObject obj, bool includeInactive = false) where T : Component
		{
			if (obj.activeSelf || includeInactive)
			{
				foreach (var comp in obj.GetComponents<T>())
					yield return comp;
			}

			if (obj.transform.parent != null)
				foreach (var comp in GetComponentsInParents<T>(obj.transform.parent.gameObject, includeInactive))
					yield return comp;
		}

		public static T GetComponentInParents<T>(GameObject obj, bool includeInactive = false) where T : Component
		{
			if (obj.activeSelf || includeInactive)
			{
				foreach (var comp in obj.GetComponents<T>())
					return comp;
			}

			if (obj.transform.parent != null)
				return GetComponentInParents<T>(obj.transform.parent.gameObject, includeInactive);
			else
				return null;
		}

		public static float CalculateOverlap(Bounds boundsA, Bounds boundsB)
		{
			float overlapPx = boundsA.max.x - boundsB.min.x;
			float overlapNx = boundsB.max.x - boundsA.min.x;
			float overlapPy = boundsA.max.y - boundsB.min.y;
			float overlapNy = boundsB.max.y - boundsA.min.y;
			float overlapPz = boundsA.max.z - boundsB.min.z;
			float overlapNz = boundsB.max.z - boundsA.min.z;

			return Mathf.Min(overlapPx, overlapNx, overlapPy, overlapNy, overlapPz, overlapNz);
		}

		public static Vector3 CalculatePerAxisOverlap(Bounds boundsA, Bounds boundsB)
		{
			float overlapPx = boundsA.max.x - boundsB.min.x;
			float overlapNx = boundsB.max.x - boundsA.min.x;
			float overlapPy = boundsA.max.y - boundsB.min.y;
			float overlapNy = boundsB.max.y - boundsA.min.y;
			float overlapPz = boundsA.max.z - boundsB.min.z;
			float overlapNz = boundsB.max.z - boundsA.min.z;

			return new Vector3(Mathf.Min(overlapPx, overlapNx), Mathf.Min(overlapPy, overlapNy), Mathf.Min(overlapPz, overlapNz));
		}
	}
}
