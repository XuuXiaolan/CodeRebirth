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
        if (string.IsNullOrEmpty(enemyName)) return null;
        if (enemyType.enemyName.ToLowerInvariant().Contains(enemyName.ToLowerInvariant())) return enemyType;
        return null;
    }
}