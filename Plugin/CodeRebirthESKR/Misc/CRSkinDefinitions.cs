using System;
using System.Collections.Generic;
using CodeRebirthESKR.SkinRegistry;
using UnityEngine;

namespace CodeRebirthESKR.Misc;

[CreateAssetMenu(fileName = "CRSkinDefinition", menuName = "CodeRebirth/CRSkinDefinition", order = 1)]
public class CRSkinDefinitions : ScriptableObject
{
    public string authorName;
    public List<BaseSkin> BaseSkins = new();
}