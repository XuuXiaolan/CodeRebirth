using System;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	public sealed class GameObjectWeight
	{
		public GameObject GameObject = null;
		public float Weight = 1f;


		public GameObjectWeight()
		{
		}

		public GameObjectWeight(GameObject gameObject, float weight = 1f)
		{
			GameObject = gameObject;
			Weight = weight;
		}
	}
}
