using UnityEngine;

namespace DunGen.Demo
{
	public class LockedDoor : MonoBehaviour, IKeyLock
	{
		public Key Key { get { return keyManager.GetKeyByID(keyID); } }
		public float OpenDuration = 1.0f;
		public Vector3 OpenPositionOffset = new Vector3(0, -3, 0);

		[HideInInspector]
		[SerializeField]
		private int keyID;

		[HideInInspector]
		[SerializeField]
		private KeyManager keyManager;

		private Vector3 initialPosition;
		private float openTime;
		private bool isOpening;
		private Door door;


		private void Start()
		{
			door = GetComponent<Door>();
		}

		public void OnKeyAssigned(Key key, KeyManager keyManager)
		{
			keyID = key.ID;
			this.keyManager = keyManager;
		}

		private void OnTriggerEnter(Collider c)
		{
			if (isOpening)
				return;

			var inventory = c.GetComponent<PlayerInventory>();

			if (inventory == null)
				return;

			if (inventory.HasKey(keyID))
			{
				ScreenText.Log("Opened {0} door", Key.Name);

				inventory.RemoveKey(keyID);
				Open();
			}
			else
				ScreenText.Log("{0} key required", Key.Name);
		}

		private void Update()
		{
			if (isOpening)
			{
				openTime += Time.deltaTime;

				if (openTime >= OpenDuration)
				{
					openTime = OpenDuration;
					isOpening = false;
				}

				transform.position = Vector3.Lerp(initialPosition, initialPosition + OpenPositionOffset, openTime / OpenDuration);
			}
		}

		private void Open()
		{
			if (isOpening)
				return;

			isOpening = true;
			initialPosition = transform.position;
			openTime = 0;
			door.IsOpen = true;
		}
	}
}