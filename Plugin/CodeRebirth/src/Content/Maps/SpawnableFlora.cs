using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public enum FloraTag
{
    None,
    Desert,
    Snow,
    Grass,
    Dangerous,
}

public class SpawnableFlora
{
    public GameObject prefab = null!;
    public string[] moonsWhiteList = null!;
    public AnimationCurve spawnCurve = null!;
    public string[] blacklistedTags = null!;
    public FloraTag floraTag;
    public string[] moonsBlackList = null!;
}