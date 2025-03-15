using System;
using System.Linq;

namespace CodeRebirth.Util.Extensions;

static class StringExtensions {
	public static string OrIfEmpty(this string? self, string defaultValue)
	{
		return !string.IsNullOrEmpty(self) ? self : defaultValue;
	}

	public static string FirstCharToUpper(this string input)
	{
		if (string.IsNullOrEmpty(input))
			throw new ArgumentException("ARGH!");
		return input.First().ToString().ToUpper() + input[1..];
	}
}