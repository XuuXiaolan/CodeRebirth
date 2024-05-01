using System;
using UnityEngine;

namespace DunGen
{
	public enum GameObjectFilter
	{
		Scene = 1,
		Asset = 2,

		All = Scene | Asset,
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class AcceptGameObjectTypesAttribute : PropertyAttribute
	{
		public GameObjectFilter Filter { get; private set; }


		public AcceptGameObjectTypesAttribute(GameObjectFilter filter)
		{
			Filter = filter;
		}
	}
}
