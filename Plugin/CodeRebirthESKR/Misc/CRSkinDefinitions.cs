using System.Collections.Generic;
using AntlerShed.SkinRegistry;
using CodeRebirthESKR.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.Misc;

[CreateAssetMenu(fileName = "GenericSkinDefinition", menuName = "XSkins/GenericSkinDefinition", order = 0)]
public class CRSkinDefinitions : ScriptableObject
{
    public string authorName;
    public DefaultSkinConfigData config;
    public List<BaseSkin> BaseSkins = new();
}