using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

namespace DunGen
{
	[Serializable]
	[CreateAssetMenu(menuName = "DunGen/Key Manager", order = 700)]
	public sealed class KeyManager : ScriptableObject
	{
		public ReadOnlyCollection<Key> Keys
		{
			get
			{
				if (keysReadOnly == null)
					keysReadOnly = new ReadOnlyCollection<Key>(keys);

				return keysReadOnly;
			}
		}

		private ReadOnlyCollection<Key> keysReadOnly;

		[SerializeField]
		private List<Key> keys = new List<Key>();


		public Key CreateKey()
		{
			Key key = new Key(GetNextAvailableID());
			key.Name = UnityUtil.GetUniqueName("New Key", keys.Select(x => x.Name));
			key.Colour = new Color(Random.value, Random.value, Random.value);

			keys.Add(key);

			return key;
		}

		public void DeleteKey(int index)
		{
			keys.RemoveAt(index);
		}

		public Key GetKeyByID(int id)
		{
			return keys.Where(x => { return x.ID == id; }).FirstOrDefault();
		}

		public Key GetKeyByName(string name)
		{
			return keys.Where(x => { return x.Name == name; }).FirstOrDefault();
		}

		public bool RenameKey(int index, string newName)
		{
			if(keys[index].Name == newName)
				return false;

			newName = UnityUtil.GetUniqueName(newName, keys.Select(x => x.Name));

			keys[index].Name = newName;
			return true;
		}

		private int GetNextAvailableID()
		{
			int nextID = 0;

			foreach(var key in keys.OrderBy(x => x.ID))
			{
				if(key.ID >= nextID)
					nextID = key.ID + 1;
			}

			return nextID;
		}
	}
}

