using System.Text;
using UnityEngine;

namespace DunGen.Demo
{
	[RequireComponent(typeof(CharacterController))]
	public class PlayerController : MonoBehaviour
	{
		public float MinYaw = -360;
		public float MaxYaw = 360;
		public float MinPitch = -60;
		public float MaxPitch = 60;
		public float LookSensitivity = 1;

		public float MoveSpeed = 10;
		public float TurnSpeed = 90;

		public bool IsControlling { get { return isControlling; } }
		public Camera ActiveCamera { get { return isControlling ? playerCamera : overheadCamera; } }

		protected CharacterController movementController;
		protected Camera playerCamera;
		protected Camera overheadCamera;
		protected bool isControlling;
		protected float yaw;
		protected float pitch;
		protected Generator gen;
		protected Vector3 velocity;


		protected virtual void Start()
		{
			movementController = GetComponent<CharacterController>();
			playerCamera = GetComponentInChildren<Camera>();
			gen = GameObject.FindObjectOfType<Generator>();
			overheadCamera = GameObject.Find("Overhead Camera").GetComponent<Camera>();

			isControlling = true;
			ToggleControl();

			gen.DungeonGenerator.Generator.OnGenerationStatusChanged += OnGenerationStatusChanged;
			gen.GetAdditionalText = GetAdditionalScreenText;
		}

		protected virtual void OnDestroy()
		{
			gen.DungeonGenerator.Generator.OnGenerationStatusChanged -= OnGenerationStatusChanged;
			gen.GetAdditionalText = null;
		}

		private void GetAdditionalScreenText(StringBuilder infoText)
		{
			infoText.AppendLine("Press 'C' to switch between camera modes");
		}

		protected virtual void OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status == GenerationStatus.Complete)
			{
				FrameObjectWithCamera(generator.Root);
				transform.position = new Vector3(0, 1, 7); // Hard-coded spawn position
				velocity = Vector3.zero;
			}
		}

		protected virtual void Update()
		{
			if (Input.GetKeyDown(KeyCode.C))
				ToggleControl();

			// Repeatedly frame the dungeon while the generation process is running
			var generator = gen.DungeonGenerator.Generator;
			if (generator.IsGenerating && generator.GenerateAsynchronously && generator.PauseBetweenRooms > 0f)
				FrameObjectWithCamera(generator.Root);

			if (isControlling)
			{
				Vector3 direction = Vector3.zero;
				direction += transform.forward * Input.GetAxisRaw("Vertical");
				direction += transform.right * Input.GetAxisRaw("Horizontal");

				direction.Normalize();

				if (movementController.isGrounded)
					velocity = Vector3.zero;
				else
					velocity += -transform.up * (9.81f * 10) * Time.deltaTime; // Gravity

				direction += velocity * Time.deltaTime;
				movementController.Move(direction * Time.deltaTime * MoveSpeed);

				// Camera Look
				yaw += Input.GetAxisRaw("Mouse X") * LookSensitivity;
				pitch += Input.GetAxisRaw("Mouse Y") * LookSensitivity;

				yaw = ClampAngle(yaw, MinYaw, MaxYaw);
				pitch = ClampAngle(pitch, MinPitch, MaxPitch);

				transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up);
				playerCamera.transform.localRotation = Quaternion.AngleAxis(pitch, -Vector3.right);
			}
		}

		protected float ClampAngle(float angle)
		{
			return ClampAngle(angle, 0, 360);
		}

		protected float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360)
				angle += 360;
			if (angle > 360)
				angle -= 360;

			return Mathf.Clamp(angle, min, max);
		}

		protected void ToggleControl()
		{
			isControlling = !isControlling;

			overheadCamera.gameObject.SetActive(!isControlling);
			playerCamera.gameObject.SetActive(isControlling);

			overheadCamera.transform.position = new Vector3(transform.position.x, overheadCamera.transform.position.y, transform.position.z);

#if UNITY_4
        Screen.lockCursor = isControlling;
#else
			Cursor.lockState = (isControlling) ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !isControlling;
#endif

			if (!isControlling)
				FrameObjectWithCamera(gen.DungeonGenerator.Generator.Root);
		}

		protected void FrameObjectWithCamera(GameObject gameObject)
		{
			if (gameObject == null)
				return;

			Bounds bounds = UnityUtil.CalculateObjectBounds(gameObject, false, false);
			float radius = Mathf.Max(bounds.size.x, bounds.size.z);

			float distance = radius / Mathf.Sin(overheadCamera.fieldOfView / 2);
			distance = Mathf.Abs(distance);

			Vector3 position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
			position += gen.DungeonGenerator.Generator.UpVector * distance;

			overheadCamera.transform.position = position;
		}
	}
}