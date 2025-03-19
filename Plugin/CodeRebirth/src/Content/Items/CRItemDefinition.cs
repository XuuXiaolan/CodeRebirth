using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;

[CreateAssetMenu(fileName = "CRItemDefinition", menuName = "CodeRebirth/CRItemDefinition", order = 1)]
public class CRItemDefinition : ScriptableObject
{
    public Item item;
    public DynamicConfigSettings ConfigEntries;
}