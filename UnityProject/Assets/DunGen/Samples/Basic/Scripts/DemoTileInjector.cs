using UnityEngine;
using System.Collections.Generic;

namespace DunGen.Demo
{
	public class DemoTileInjector : MonoBehaviour
	{
		public RuntimeDungeon RuntimeDungeon;
		public TileSet TileSet;
		public float NormalizedPathDepth;
		public float NormalizedBranchDepth;
		public bool IsOnMainPath;


		private void Awake()
		{
			RuntimeDungeon.Generator.TileInjectionMethods += InjectTiles;
		}

		private void InjectTiles(RandomStream randomStream, ref List<InjectedTile> tilesToInject)
		{
			tilesToInject.Add(new InjectedTile(TileSet, IsOnMainPath, NormalizedPathDepth, NormalizedBranchDepth));
		}
	}
}
