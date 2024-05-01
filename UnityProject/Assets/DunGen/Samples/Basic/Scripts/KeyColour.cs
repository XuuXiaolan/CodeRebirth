using UnityEngine;

namespace DunGen.Demo
{
	public class KeyColour : MonoBehaviour, IKeyLock
	{
		public void OnKeyAssigned(Key key, KeyManager manager)
		{
			SetColour(key.Colour);
		}

		private void SetColour(Color colour)
		{
			if (Application.isPlaying)
			{
				foreach (var r in GetComponentsInChildren<Renderer>())
					r.material.color = colour;
			}
		}
	}
}