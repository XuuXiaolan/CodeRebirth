using UnityEngine;

namespace DunGen.Demo
{
	public class CameraMovement : MonoBehaviour
	{
		public float MovementSpeed = 100;


		private void Start()
		{
			RuntimeDungeon runtimeDungeon = Component.FindObjectOfType<RuntimeDungeon>();

			if (runtimeDungeon != null)
				transform.forward = -runtimeDungeon.Generator.UpVector;
		}

		private void Update()
		{
			Vector3 direction = Vector3.zero;

			direction += transform.up * Input.GetAxisRaw("Vertical");
			direction += transform.right * Input.GetAxisRaw("Horizontal");

			direction.Normalize();

			Vector3 offset = direction * MovementSpeed * Time.deltaTime;
			if (Input.GetKey(KeyCode.LeftShift))
				offset *= 2;

			float zoom = Input.GetAxisRaw("Mouse ScrollWheel");
			offset += transform.forward * zoom * Time.deltaTime * MovementSpeed * 100;

			transform.position += offset;
		}
	}
}