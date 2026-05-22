using System.Text.RegularExpressions;

namespace SafetyScale.Infrastructure.Tenancy;

internal static class TenantSlugNormalizer
{
    private static readonly Regex NonSlugChars =
        new(@"[^\p{L}\p{N}\s-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RepeatedHyphens = new(@"-+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ToSlugBase(string name)
    {
        var trimmed = name.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        var s = NonSlugChars.Replace(trimmed, "");
        s = Spaces.Replace(s, "-");
        s = RepeatedHyphens.Replace(s, "-").Trim('-');
        return s;
    }

    public static string Clamp(string slug, int maxLength)
    {
        if (slug.Length <= maxLength)
        {
            return slug;
        }

        return slug[..maxLength].TrimEnd('-');
    }
}
