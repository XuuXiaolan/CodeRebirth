using UnityEngine;

namespace DunGen.Demo
{
	public class KeyPickup : MonoBehaviour, IKeyLock
	{
		public Key Key { get { return keyManager.GetKeyByID(keyID); } }

		[HideInInspector]
		[SerializeField]
		private int keyID;

		[HideInInspector]
		[SerializeField]
		private KeyManager keyManager;


		public void OnKeyAssigned(Key key, KeyManager keyManager)
		{
			keyID = key.ID;
			this.keyManager = keyManager;
		}

		private void OnTriggerEnter(Collider c)
		{
			var inventory = c.GetComponent<PlayerInventory>();

			if (inventory == null)
				return;

			ScreenText.Log("Picked up {0} key", Key.Name);
			inventory.AddKey(keyID);
			UnityUtil.Destroy(gameObject);
		}
	}
}