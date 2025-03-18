using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;

namespace CodeRebirth.src.Util.AssetLoading;
public interface IEnemyAssets
{
    IReadOnlyList<CREnemyDefinition> EnemyDefinitions { get; }
}
