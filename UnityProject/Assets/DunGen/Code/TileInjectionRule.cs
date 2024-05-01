using System;

namespace DunGen
{
	[Serializable]
	public sealed class TileInjectionRule
	{
		public TileSet TileSet;
		public FloatRange NormalizedPathDepth = new FloatRange(0, 1);
		public FloatRange NormalizedBranchDepth = new FloatRange(0, 1);
		public bool CanAppearOnMainPath = true;
		public bool CanAppearOnBranchPath = false;
		public bool IsRequired = false;
		public bool IsLocked = false;
		public int LockID = 0;
	}
}
