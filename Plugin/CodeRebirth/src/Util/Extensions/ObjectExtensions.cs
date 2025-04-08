
namespace CodeRebirth.src.Util.Extensions;
public static class ObjectExtensions
{
    // this is so easy to do in kotlin why not here :sob:
    public static string ToStringWithDefault(this object it, string defaultString = "null")
    {
        return it.ToString().OrIfEmpty(defaultString);
    }
}