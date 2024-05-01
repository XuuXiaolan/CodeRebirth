using DunGen.Graph;

namespace DunGen.Editor.Validation.Rules
{
	sealed class DungeonFlowIntegrityRule : IValidationRule
	{
		public void Validate(DungeonFlow flow, DungeonValidator validator)
		{
			// Check DungeonFlow
			if(flow == null)
			{
				validator.AddError("No DungeonFlow is assigned");
				return;
			}

			CheckLineSegments(flow, validator);
			CheckNodes(flow, validator);
			CheckArchetypes(flow, validator);
			CheckTileSets(flow, validator);
		}

		private void CheckLineSegments(DungeonFlow flow, DungeonValidator validator)
		{
			if (flow.Lines.Count < 1)
				validator.AddError("The dungeon flow must contain at least one line segment");

			foreach (var line in flow.Lines)
			{
				if (line.DungeonArchetypes.Count == 0)
					validator.AddError("A line segments in your dungeon flow graph has no archetype applied");

				foreach (var archetype in line.DungeonArchetypes)
					if (archetype == null)
						validator.AddError("A line segment in your dungeon flow graph has an unset archetype value");
			}
		}

		private void CheckNodes(DungeonFlow flow, DungeonValidator validator)
		{
			if (flow.Nodes.Count < 2)
				validator.AddError("The dungeon flow must contain at least two nodes");

			foreach (var node in flow.Nodes)
			{
				if (node.TileSets.Count == 0)
					validator.AddError("The node '{0}' in the dungeon flow graph has no tile sets applied", node.Label);
				else
				{
					foreach(var tileSet in node.TileSets)
						if(tileSet == null)
							validator.AddError("Node '{0}' in your dungeon flow graph has an unset tileset value", node.Label);
				}
			}
		}

		private void CheckArchetypes(DungeonFlow flow, DungeonValidator validator)
		{
			var archetypes = flow.GetUsedArchetypes();

			foreach(var archetype in archetypes)
			{
				if (archetype == null)
					continue;

				if (archetype.TileSets.Count == 0)
					validator.AddError("The archetype '{0}' has no tile sets assigned", archetype, archetype.name);
				else
				{
					foreach (var tileSet in archetype.TileSets)
						if (tileSet == null)
							validator.AddError("The archetype '{0}' has a missing tile set", archetype, archetype.name);
				}

				foreach (var tileSet in archetype.BranchCapTileSets)
					if (tileSet == null)
						validator.AddError("Archetype '{0}' has a missing branch cap tile set", archetype, archetype.name);
			}
		}

		private void CheckTileSets(DungeonFlow flow, DungeonValidator validator)
		{
			var tileSets = flow.GetUsedTileSets();

			foreach (var tileSet in tileSets)
			{
				if (tileSet == null)
					continue;

				foreach (var tileWeight in tileSet.TileWeights.Weights)
				{
					if (tileWeight.Value == null)
						validator.AddWarning("The tile set '{0}' has a missing tile", tileSet, tileSet.name);

					if (tileWeight.MainPathWeight <= 0f && tileWeight.BranchPathWeight <= 0f)
						validator.AddWarning("Tile set '{0}' has a tile ({1}) set up with a main path weight and branch path weight of zero. This tile will never appear in the dungeon", tileSet, tileSet.name, (tileWeight.Value == null) ? "NULL" : tileWeight.Value.name);

					if (tileWeight.DepthWeightScale != null)
					{
						bool hasNonZeroKeyframe = false;

						foreach (var key in tileWeight.DepthWeightScale.keys)
						{
							if (key.value > 0f)
							{
								hasNonZeroKeyframe = true;
								break;
							}
						}

						if (!hasNonZeroKeyframe)
							validator.AddWarning("Tile set '{0}' has a tile ({1}) set up with a depth curve that will always return zero. This tile will never appear in the dungeon", tileSet, tileSet.name, tileWeight.Value.name);
					}
				}
			}
		}
	}
}
