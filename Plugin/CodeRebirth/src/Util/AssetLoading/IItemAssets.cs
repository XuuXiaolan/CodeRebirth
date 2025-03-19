using System.Collections.Generic;
using CodeRebirth.src.Content.Items;

namespace CodeRebirth.src.Util.AssetLoading;
public interface IItemAssets : IBundleAsset
{
    IReadOnlyList<CRItemDefinition> ItemDefinitions { get; }
}
