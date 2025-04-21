using System.Collections.Generic;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
[CreateAssetMenu(fileName = "CREnemyDefinition", menuName = "CodeRebirth/CREnemyDefinition", order = 1)]
public class CREnemyDefinition : CRContentDefinition
{
    public EnemyType enemyType;
    public TerminalNode? terminalNode;
    public TerminalKeyword? terminalKeyword;

    public EnemyType? GetEnemyTypeOnName(string enemyName)
    {
        if (string.IsNullOrEmpty(enemyName))
            return null;

        if (enemyType.enemyName.Contains(enemyName, System.StringComparison.OrdinalIgnoreCase))
            return enemyType;

        return null;
    }
}

public static class CREnemyDefinitionExtensions
{
    public static CREnemyDefinition? GetCREnemyDefinitionWithEnemyName(this IReadOnlyList<CREnemyDefinition> EnemyDefinitions, string enemyName)
    {
        foreach (var entry in EnemyDefinitions)
        {
            if (entry.GetEnemyTypeOnName(enemyName) != null) return entry;
        }
        return null;
    }
}