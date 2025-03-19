using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

[CreateAssetMenu(fileName = "CREnemyDefinition", menuName = "CodeRebirth/CREnemyDefinition", order = 1)]
public class CRItemDefinition : ScriptableObject
{
    public Item item;
    public DynamicConfigSettings ConfigEntries;
}