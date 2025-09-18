// =============================================
// File: src/Cuaderno.Core/Utils/SlugHelper.cs
// Desc: Normaliza títulos a kebab-case
// =============================================

using System.Text;
using System.Text.RegularExpressions;

namespace Cuaderno.Core.Utils;

public static class SlugHelper
{
    public static string ToKebab(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "nota";
        var normalized = input.ToLowerInvariant();
        normalized = RemoveDiacritics(normalized);
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", ""); 
        normalized = Regex.Replace(normalized, @"\s+", "-").Trim('-');
        normalized = Regex.Replace(normalized, @"-+", "-"); 
        return string.IsNullOrWhiteSpace(normalized) ? "nota" : normalized;
    }

    private static string RemoveDiacritics(string text)
    {
        var formD = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}