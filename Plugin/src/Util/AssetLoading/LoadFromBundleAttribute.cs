using System;

namespace CodeRebirth.Util.AssetLoading;

[AttributeUsage(AttributeTargets.Property)]
internal class LoadFromBundleAttribute(string bundleFile) : Attribute {
	public string BundleFile { get; private set; } = bundleFile;
}
