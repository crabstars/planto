namespace Planto.Extensions;

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxChars)
    {
        if (maxChars < 0)
            return value;
        return value.Length <= maxChars ? value : value[..maxChars];
    }
}