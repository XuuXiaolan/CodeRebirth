using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

[CreateAssetMenu(fileName = "CREnemyDefinition", menuName = "CodeRebirth/CREnemyDefinition", order = 1)]
public class CREnemyDefinition : ScriptableObject
{
    public EnemyType enemyType;
    public TerminalNode? terminalNode;
    public TerminalKeyword? terminalKeyword;
    public List<CRDynamicConfig> ConfigEntries;
}