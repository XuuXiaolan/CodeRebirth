using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CodeRebirth.EnemyStuff;
using MonoMod.Cil;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.Misc;

public static class ExtensionMethods
{
		
    public static SnailCatAI.State ToSnailState(this int index)
	{
		return (SnailCatAI.State)index;
	}

	public static CutieFlyAI.State ToCutieState(this int index)
	{
		return (CutieFlyAI.State)index;
	}

	public static QuestMasterAI.State ToQuestMasterAIState(this int index)
	{
		return (QuestMasterAI.State)index;
	} 
}