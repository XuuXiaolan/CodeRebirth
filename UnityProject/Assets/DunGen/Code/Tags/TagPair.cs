using System;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class TagPair
	{
		public Tag TagA;
		public Tag TagB;


		public TagPair()
		{
		}

		public TagPair(Tag a, Tag b)
		{
			TagA = a;
			TagB = b;
		}

		public override string ToString()
		{
			return string.Format("{0} <-> {1}", TagA.Name, TagB.Name);
		}

		public bool Matches(Tag a, Tag b, bool twoWay)
		{
			if (twoWay)
				return (a == TagA && b == TagB) || (a == TagB && b == TagA);
			else
				return a == TagA && b == TagB;
		}
	}
}
