using UnityEngine;

namespace DunGen.Demo
{
	public class KeySpawnPoint : MonoBehaviour, IKeySpawnable
	{
		public bool SetColourOnSpawn = true;


		public void SpawnKey(Key key, KeyManager manager)
		{
			var obj = (GameObject)GameObject.Instantiate(key.Prefab);
			obj.transform.parent = transform;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;

			// We can't set the colour of a material in the editor without leaking materials, so it's disabled for this demo
			if (SetColourOnSpawn && Application.isPlaying)
			{
				foreach (var r in obj.GetComponentsInChildren<Renderer>())
					r.material.color = key.Colour;
			}
		}
	}
}