using System.Collections.Generic;

namespace CodeRebirth.src.Util.Extensions;

public static class IListExtensions { // Taken from Smooth_P in https://discussions.unity.com/t/clever-way-to-shuffle-a-list-t-in-one-line-of-c-code/535113
	/// <summary>
	/// Shuffles the element order of the specified list.
	/// </summary>
	public static void Shuffle<T>(this IList<T> ts) {
		var count = ts.Count;
		var last = count - 1;
		for (var i = 0; i < last; ++i) {
			var r = UnityEngine.Random.Range(i, count);
			var tmp = ts[i];
			ts[i] = ts[r];
			ts[r] = tmp;
		}
	}
}