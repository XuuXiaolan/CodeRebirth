using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

[CreateAssetMenu(fileName = "CRMapObjectDefinition", menuName = "CodeRebirth/CRMapObjectDefinition", order = 1)]
public class CRMapObjectDefinition : CRContentDefinition
{
    public GameObject gameObject;
    public string objectName;

    public GameObject? GetGameObjectOnName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (objectName.ToLowerInvariant().Contains(name.ToLowerInvariant())) return gameObject;
        return null;
    }
}