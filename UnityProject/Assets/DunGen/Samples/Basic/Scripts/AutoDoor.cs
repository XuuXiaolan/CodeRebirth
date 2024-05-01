using UnityEngine;

namespace DunGen.Demo
{
	public class AutoDoor : MonoBehaviour
	{
		public enum DoorState
		{
			Open,
			Closed,
			Opening,
			Closing,
		}

		public GameObject Door;
		public Vector3 OpenOffset = new Vector3(0, 2.5f, 0);
		public float Speed = 3.0f;

		private Vector3 closedPosition;
		private DoorState currentState = DoorState.Closed;
		private float currentFramePosition = 0.0f;
		private Door doorComponent;


		private void Start()
		{
			doorComponent = GetComponent<Door>();
			closedPosition = Door.transform.localPosition;
		}

		private void Update()
		{
			if (currentState == DoorState.Opening || currentState == DoorState.Closing)
			{
				Vector3 openPosition = closedPosition + OpenOffset;

				float frameOffset = Speed * Time.deltaTime;

				if (currentState == DoorState.Closing)
					frameOffset *= -1;

				currentFramePosition += frameOffset;
				currentFramePosition = Mathf.Clamp(currentFramePosition, 0, 1);

				Door.transform.localPosition = Vector3.Lerp(closedPosition, openPosition, currentFramePosition);

				// Finished
				if (currentFramePosition == 1.0f)
					currentState = DoorState.Open;
				else if (currentFramePosition == 0.0f)
				{
					currentState = DoorState.Closed;
					doorComponent.IsOpen = false;
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			PlayerController playerController = other.GetComponent<PlayerController>();

			// Ignore overlaps with anything other than the player
			if (playerController == null)
				return;

			currentState = DoorState.Opening;
			doorComponent.IsOpen = true;
		}

		private void OnTriggerExit(Collider other)
		{
			PlayerController playerController = other.GetComponent<PlayerController>();

			// Ignore overlaps with anything other than the player
			if (playerController == null)
				return;

			currentState = DoorState.Closing;
		}
	}
}