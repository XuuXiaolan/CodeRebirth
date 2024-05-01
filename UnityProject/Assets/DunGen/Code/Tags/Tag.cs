using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class Tag : IEqualityComparer<Tag>
	{
		public int ID
		{
			get { return id; }
			set
			{
				id = value;
			}
		}
		public string Name
		{
			get { return DunGenSettings.Instance.TagManager.TryGetNameFromID(id); }
			set { DunGenSettings.Instance.TagManager.TryRenameTag(id, value); }
		}


		[SerializeField]
		private int id = -1;



		public Tag(int id)
		{
			this.id = id;
		}

		public Tag(string name)
		{
			DunGenSettings.Instance.TagManager.TagExists(name, out id);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			var other = obj as Tag;

			if (other == null)
				return false;
			else
				return Equals(this, other);
		}

		public override int GetHashCode()
		{
			return id;
		}

		public override string ToString()
		{
			return string.Format("[{0}] {1}", id, DunGenSettings.Instance.TagManager.TryGetNameFromID(id));
		}

		#region IEqualityComparer<Tag>

		public int GetHashCode(Tag tag)
		{
			return id;
		}

		public bool Equals(Tag x, Tag y)
		{
			if (x == null && y == null)
				return true;
			else if (x == null || y == null)
				return false;

			return x.id == y.id;
		}

		#endregion

		#region Operator Overloads

		public static bool operator ==(Tag a, Tag b)
		{
			if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
				return true;
			else if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;

			return a.id == b.id;
		}

		public static bool operator !=(Tag a, Tag b)
		{
			if (a == null && b == null)
				return false;
			else if (a == null && b != null)
				return true;

			return a.id != b.id;
		}

		#endregion
	}
}
