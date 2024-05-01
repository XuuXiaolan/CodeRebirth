using System;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	public sealed class Key
	{
		public int ID
		{
			get { return id; }
			set { id = value; }
		}
		public string Name
		{
			get { return name; }
			internal set { name = value; }
		}
		public GameObject Prefab;
		public Color Colour;
        public IntRange KeysPerLock = new IntRange(1, 1);


		[SerializeField]
		private int id;
		[SerializeField]
		private string name;


		internal Key(int id)
		{
			this.id = id;
		}
	}
}

