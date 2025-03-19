using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;

namespace CodeRebirth.src.Util.AssetLoading;
public interface IEnemyAssets : IBundleAsset
{
    IReadOnlyList<CREnemyDefinition> EnemyDefinitions { get; }
}
