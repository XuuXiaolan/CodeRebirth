using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public enum FloraTag {
	Desert,
	Snow,
	Grass,
}

public class SpawnableFlora {
	public GameObject prefab = null!;
	public string[] moonsWhiteList = null!;
	public AnimationCurve spawnCurve = null!;
	public string[] blacklistedTags = null!;
	public FloraTag floraTag;
}