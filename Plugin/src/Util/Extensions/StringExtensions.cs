namespace CodeRebirth.Util.Extensions;

static class StringExtensions {
	public static string OrIfEmpty(this string? self, string defaultValue)
	{
		return !string.IsNullOrEmpty(self) ? self : defaultValue;
	}
}