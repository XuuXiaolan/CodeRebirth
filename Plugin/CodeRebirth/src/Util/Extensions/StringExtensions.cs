using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeRebirth.src.Util.Extensions;

static class StringExtensions
{
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

	private static readonly Regex ConfigCleanerRegex = new Regex(@"[\n\t""`\[\]']");
	public static string CleanStringForConfig(this string input)
	{
		// The regex pattern matches: newline, tab, double quote, backtick, apostrophe, [ or ].
	    return ConfigCleanerRegex.Replace(input, string.Empty);
	}
}