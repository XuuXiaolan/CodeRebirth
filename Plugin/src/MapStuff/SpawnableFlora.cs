using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public class SpawnableFlora {
	public GameObject prefab;
	public Levels.LevelTypes levelTypes;
	public string[] customLevelTypes;
	public AnimationCurve spawnCurve;
	public string[] blacklistedTags;
}