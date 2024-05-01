using UnityEngine;

namespace DunGen
{
	public sealed class InjectedTile
	{
		public TileSet TileSet;
		public float NormalizedPathDepth;
		public float NormalizedBranchDepth;
		public bool IsOnMainPath;
		public bool IsRequired;
		public bool IsLocked;
		public int LockID;
		public GameObject LockedDoorPrefab;


		public InjectedTile(TileSet tileSet, bool isOnMainPath, float normalizedPathDepth, float normalizedBranchDepth, bool isRequired = false)
		{
			TileSet = tileSet;
			IsOnMainPath = isOnMainPath;
			NormalizedPathDepth = normalizedPathDepth;
			NormalizedBranchDepth = normalizedBranchDepth;
			IsRequired = isRequired;
		}

		public InjectedTile(TileInjectionRule rule, bool isOnMainPath, RandomStream randomStream)
		{
			TileSet = rule.TileSet;
			NormalizedPathDepth = rule.NormalizedPathDepth.GetRandom(randomStream);
			NormalizedBranchDepth = rule.NormalizedBranchDepth.GetRandom(randomStream);
			IsOnMainPath = isOnMainPath;
			IsRequired = rule.IsRequired;
			IsLocked = rule.IsLocked;
			LockID = rule.LockID;
		}

		public bool ShouldInjectTileAtPoint(bool isOnMainPath, float pathDepth, float branchDepth)
		{
			if (IsOnMainPath != isOnMainPath)
				return false;

			if (NormalizedPathDepth > pathDepth)
				return false;
			else if (isOnMainPath)
				return true;

			return NormalizedBranchDepth <= branchDepth;
		}
	}
}
