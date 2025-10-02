using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BookHeaven.EpubManager;

public static partial class HtmlManager
{
    [GeneratedRegex(@"\d+\.?\d*")]
    private static partial Regex NumberRegex();
    [GeneratedRegex("[a-zA-Z%]+$")]
    private static partial Regex UnitRegex();
    private enum CssEditMode
    {
        Replace,
        Add,
        Max,
        Remove,
        ReplaceProperty
    }
    
    private class CssProperty
    {
        public string Property { get; init; } = null!;
        public string NewProperty { get; init; } = null!;
        public string? CssVariable { get; init; }
        public string? CssUnit { get; init; }
        public CssEditMode Mode { get; init; }
    }
    
    private static readonly List<CssProperty> CustomStyles =
    [
        new() { Property = "line-height", CssVariable= "var(--line-height)", CssUnit = "em", Mode = CssEditMode.Add },
        new() { Property = "text-indent", CssVariable= "var(--text-indent)", CssUnit = "em", Mode = CssEditMode.Replace },
        new() { Property = "margin-top", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
        new() { Property = "margin-bottom", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
        new() { Property = "margin", CssVariable= "var(--paragraph-spacing)", CssUnit = "pt", Mode = CssEditMode.Max },
        new() { Property = "font-size", CssVariable= "1", CssUnit = "em", Mode = CssEditMode.Max },
        new() { Property = "font-family", Mode = CssEditMode.Remove },
        new() { Property = "widows", Mode = CssEditMode.Remove },
        new() { Property = "orphans", Mode = CssEditMode.Remove },
        new() { Property = "padding-top", NewProperty = "margin-top",Mode = CssEditMode.ReplaceProperty},
        new() { Property = "padding-bottom", NewProperty = "margin-bottom",Mode = CssEditMode.ReplaceProperty}
    ];

    public static async Task ReplaceCssProperties(StringBuilder content)
    {
        var contentString = content.ToString();
        var tasks = CustomStyles.SelectMany(cSsProperty =>
        {
            Regex regex = new(@$"{cSsProperty.Property}:\s*([^;}}]+?)(;|}})");
            if (!regex.IsMatch(contentString))
            {
                return [Task.FromResult((original: (string?)null, replacement: (string?)null))];
            }

            var matches = regex.Matches(contentString);
            return matches.DistinctBy(m => m.Value).Select(async match =>
            {
                return await Task.Run(() =>
                {
                    switch (cSsProperty.Mode)
                    {
                        case CssEditMode.Remove:
                            return ((string?)match.Value.Trim(), "");
                        case CssEditMode.ReplaceProperty:
                            return ((string?)match.Value.Trim(), (string?)match.Value.Replace(cSsProperty.Property, cSsProperty.NewProperty).Trim());
                    }
					
                    var delimiter = match.Groups[2].Value;

                    var values = match.Groups[1].Value.Split(' ').Select(v => v.Trim()).ToList();
                    var processedValues = values.Select(value =>
                    {
                        if (!IsAboveZero(value))
                        {
                            return value;
                        }
						
                        return cSsProperty.Mode switch
                        {
                            CssEditMode.Replace => $"calc({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit})",
                            CssEditMode.Add => $"calc({EnsureUnit(value, cSsProperty.CssUnit!)} + ({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit}))",
                            CssEditMode.Max => $"max({value}, calc({cSsProperty.CssVariable} * 1{cSsProperty.CssUnit}))",
                            _ => value
                        };
                    });

                    var replacement = $"{cSsProperty.Property}: {string.Join(" ", processedValues)}{delimiter}";

                    return ((string?)match.Value.Trim(), (string?)replacement.Trim());
                });

            });
        });

        var results = await Task.WhenAll(tasks);
        results = results.Where(x => x is { Item1: not null, Item2: not null }).Distinct().ToArray();
        foreach (var (original, replacement) in results)
        {
            if (original != null && replacement != null)
            {
                content.Replace(original, replacement);
            }
        }
    }
    
    private static bool IsAboveZero(string cssValue)
    {
        var numberMatch = NumberRegex().Match(cssValue);
        if (numberMatch.Success)
        {
            return double.Parse(numberMatch.Value) > 0;
        }
        return false;
    }

    private static string EnsureUnit(string value, string unit)
    {
        if (UnitRegex().IsMatch(value))
            return value;
		
        return value + unit;
    }
}