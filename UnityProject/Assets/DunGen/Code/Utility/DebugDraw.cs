using UnityEngine;

namespace DunGen
{
	public static class DebugDraw
	{
		public static void Bounds(Bounds localBounds, Matrix4x4 transform, Color colour, float duration = 0f, bool depthTest = false)
		{
			Vector3 localMin = localBounds.min;
			Vector3 localMax = localBounds.max;

			Vector3 lut = transform.MultiplyPoint(new Vector3(localMin.x, localMax.y, localMax.z));
			Vector3 lub = transform.MultiplyPoint(new Vector3(localMin.x, localMax.y, localMin.z));
			Vector3 rut = transform.MultiplyPoint(new Vector3(localMax.x, localMax.y, localMax.z));
			Vector3 rub = transform.MultiplyPoint(new Vector3(localMax.x, localMax.y, localMin.z));

			Vector3 ldt = transform.MultiplyPoint(new Vector3(localMin.x, localMin.y, localMax.z));
			Vector3 ldb = transform.MultiplyPoint(new Vector3(localMin.x, localMin.y, localMin.z));
			Vector3 rdt = transform.MultiplyPoint(new Vector3(localMax.x, localMin.y, localMax.z));
			Vector3 rdb = transform.MultiplyPoint(new Vector3(localMax.x, localMin.y, localMin.z));

			// Top Face
			Debug.DrawLine(lut, lub, colour, duration, depthTest);
			Debug.DrawLine(lut, rut, colour, duration, depthTest);
			Debug.DrawLine(lub, rub, colour, duration, depthTest);
			Debug.DrawLine(rut, rub, colour, duration, depthTest);

			// Bottom Face
			Debug.DrawLine(ldt, ldb, colour, duration, depthTest);
			Debug.DrawLine(ldt, rdt, colour, duration, depthTest);
			Debug.DrawLine(ldb, rdb, colour, duration, depthTest);
			Debug.DrawLine(rdt, rdb, colour, duration, depthTest);

			// Connecting Lines
			Debug.DrawLine(lut, ldt, colour, duration, depthTest);
			Debug.DrawLine(rut, rdt, colour, duration, depthTest);
			Debug.DrawLine(rub, rdb, colour, duration, depthTest);
			Debug.DrawLine(lub, ldb, colour, duration, depthTest);
		}
	}
}
