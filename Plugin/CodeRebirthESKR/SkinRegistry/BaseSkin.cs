using AntlerShed.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry;
public abstract class BaseSkin : ScriptableObject, Skin
{
    [SerializeField] public SpawnLocation spawnLocation;

    [SerializeField] protected string label;
    public string Label => label;

    [SerializeField] protected string id;
    public string Id => id;

    [SerializeField] protected Texture2D icon;
    public Texture2D Icon => icon;

    public abstract string EnemyId { get; }
    public abstract Skinner CreateSkinner();
}