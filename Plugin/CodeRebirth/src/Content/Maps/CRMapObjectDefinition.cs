using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

[CreateAssetMenu(fileName = "CRMapObjectDefinition", menuName = "CodeRebirth/CRMapObjectDefinition", order = 1)]
public class CRMapObjectDefinition : CRContentDefinition
{
    public GameObject gameObject;
    public string objectName;
    public bool alignWithTerrain;
    public SpawnSyncedCRObject.CRObjectType CRObjectType;

    public GameObject? GetGameObjectOnName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (objectName.ToLowerInvariant().Contains(name.ToLowerInvariant())) return gameObject;
        return null;
    }
}

public static class CRMapObjectDefinitionExtensions
{
    public static CRMapObjectDefinition? GetCRMapObjectDefinitionWithObjectName(this IReadOnlyList<CRMapObjectDefinition> MapObjectDefinitions, string objectName)
    {
        foreach (var entry in MapObjectDefinitions)
        {
            if (entry.GetGameObjectOnName(objectName) != null) return entry;
        }
        return null;
    }
}