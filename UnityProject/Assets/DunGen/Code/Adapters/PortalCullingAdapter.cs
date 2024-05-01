using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen.Adapters
{
	public abstract class CullingAdapter : BaseAdapter
	{
		public CullingAdapter()
		{
			Priority = -1;
		}

		protected abstract void PrepareForCulling(DungeonGenerator generator, Dungeon dungeon);

		protected override void Run(DungeonGenerator generator)
		{
			PrepareForCulling(generator, generator.CurrentDungeon);
		}
	}
}
