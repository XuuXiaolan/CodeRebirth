using AntlerShed.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.SkinRegistry;
public abstract class BaseSkin : ScriptableObject, Skin
{
    public SpawnLocation spawnLocation;

    [Tooltip("Label found in the config.")]
    [SerializeField] protected string label;
    public string Label => label;

    [Tooltip("Internal ID, follow a format like EnemyName.SkinName or AuthorName.EnemyName.SkinName")]
    [SerializeField] protected string id;
    public string Id => id;

    [Tooltip("Icon of the skin found in the config.")]
    [SerializeField] protected Texture2D icon;
    public Texture2D Icon => icon;

    [Tooltip("The EnemyName found in the EnemyType ScriptableObject.")]
    [SerializeField] protected string enemyId;
    public string EnemyId => enemyId;
    public abstract Skinner CreateSkinner();
}