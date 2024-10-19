using System;
using CodeRebirth.Util.Extensions;

namespace CodeRebirth.Util.Extensions;

static class ObjectExtensions {
	// this is so easy to do in kotlin why not here :sob:
	public static string ToStringWithDefault(this object it, string defaultString = "null") {
		return it.ToString().OrIfEmpty(defaultString);
	} 
}