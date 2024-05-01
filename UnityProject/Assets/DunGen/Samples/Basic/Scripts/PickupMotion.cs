using UnityEngine;

namespace DunGen.Demo
{
	public class PickupMotion : MonoBehaviour
	{
		public float SpinSpeed = 90;
		public float BobSpeed = 1;
		public float BobDistance = 1;

		private Vector3 positionOffset;


		protected virtual void Update()
		{
			transform.position -= positionOffset;
			positionOffset = transform.up * Mathf.Sin(Time.time * BobSpeed) * BobDistance;
			transform.position += positionOffset;

			transform.rotation *= Quaternion.AngleAxis(SpinSpeed * Time.deltaTime, transform.up);
		}
	}
}