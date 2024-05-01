using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen
{
	public sealed class CoroutineHelper : MonoBehaviour
	{
		private static CoroutineHelper instance;
		private static CoroutineHelper Instance
		{
			get
			{
				if (instance == null)
				{
					var obj = new GameObject("DunGen Coroutine Helper");
					obj.hideFlags = HideFlags.HideInHierarchy;

					instance = obj.AddComponent<CoroutineHelper>();
				}

				return instance;
			}
		}


		public static Coroutine Start(IEnumerator routine)
		{
			return Instance.StartCoroutine(routine);
		}

		public static void StopAll()
		{
			Instance.StopAllCoroutines();
		}
	}
}
