using System;
using Dawn;
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
    public FloraTag floraTag;
    public Func<DawnMoonInfo, AnimationCurve> spawnCurveFunction;
}