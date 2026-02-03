using System.Text.RegularExpressions;

namespace PathPilot.Core.Models;

/// <summary>
/// Represents a gear item in the build
/// </summary>
public class Item
{
    /// <summary>
    /// Item name (e.g. "Rare Gloves", "Belly of the Beast")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item slot (e.g. "Main Hand", "Body Armour")
    /// </summary>
    public string Slot { get; set; } = string.Empty;

    /// <summary>
    /// Item rarity (Normal, Magic, Rare, Unique)
    /// </summary>
    public string Rarity { get; set; } = "Normal";

    /// <summary>
    /// Base type of the item
    /// </summary>
    public string BaseType { get; set; } = string.Empty;

    /// <summary>
    /// Raw item text from PoB
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Formatted item text with PoB tags removed and ranges calculated
    /// </summary>
    public string FormattedText => FormatRawText(RawText);

    private static string FormatRawText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return string.Empty;

        var lines = rawText.Split('\n');
        var result = new List<string>();

        // Lines to skip (metadata)
        var skipPrefixes = new[] { "Rarity:", "New Item", "Crafted:", "Prefix:", "Suffix:", "LevelReq:", "Implicits:", "Sockets:" };

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and metadata lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;
            if (skipPrefixes.Any(p => trimmedLine.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Process the line to remove tags and calculate ranges
            var processed = ProcessLine(trimmedLine);
            if (!string.IsNullOrWhiteSpace(processed))
                result.Add(processed);
        }

        return string.Join("\n", result);
    }

    private static string ProcessLine(string line)
    {
        // First, extract all range values and store them
        var rangePattern = new Regex(@"\{range:([\d.]+)\}");
        double currentRange = 0.5; // Default to middle if no range specified

        var rangeMatch = rangePattern.Match(line);
        if (rangeMatch.Success)
        {
            double.TryParse(rangeMatch.Groups[1].Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out currentRange);
        }

        // Remove all PoB tags: {range:X}, {tags:...}, {crafted}, {variant:X}, etc.
        var result = Regex.Replace(line, @"\{[^}]+\}", "");

        // Calculate actual values from ranges like (10-16) or (23-30)
        result = Regex.Replace(result, @"\((\d+)-(\d+)\)", match =>
        {
            if (int.TryParse(match.Groups[1].Value, out int min) &&
                int.TryParse(match.Groups[2].Value, out int max))
            {
                var value = (int)Math.Round(min + currentRange * (max - min));
                return value.ToString();
            }
            return match.Value;
        });

        // Clean up extra whitespace
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    /// <summary>
    /// Required sockets (list of colors)
    /// </summary>
    public List<SocketColor> RequiredSockets { get; set; } = new();

    /// <summary>
    /// Required number of links (e.g. 6 for 6-link)
    /// </summary>
    public int RequiredLinks { get; set; }

    /// <summary>
    /// Which gems are socketed in this item
    /// </summary>
    public List<string> SocketedGems { get; set; } = new();

    /// <summary>
    /// Important item properties/mods to look for
    /// </summary>
    public List<string> ImportantMods { get; set; } = new();

    /// <summary>
    /// Item level requirement
    /// </summary>
    public int RequiredLevel { get; set; }

    /// <summary>
    /// Is this item required for the build to function?
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Notes about the item (e.g. "Can use any rare chest until you get this")
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Equipment slots
/// </summary>
public enum ItemSlot
{
    Weapon,
    OffHand,
    Helmet,
    BodyArmour,
    Gloves,
    Boots,
    Amulet,
    Ring,
    Belt,
    Jewel,
    Flask
}

/// <summary>
/// Item rarity
/// </summary>
public enum ItemRarity
{
    Normal,
    Magic,
    Rare,
    Unique
}
