namespace PathPilot.Core.Models;

/// <summary>
/// Represents a single sprite coordinate within a sprite sheet
/// </summary>
public class SpriteCoordinate
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}

/// <summary>
/// Represents a sprite sheet with coordinates for all sprites in it
/// </summary>
public class SpriteSheetData
{
    public string Filename { get; set; } = string.Empty;  // Full URL from JSON
    public int SheetWidth { get; set; }   // "w" from JSON
    public int SheetHeight { get; set; }  // "h" from JSON
    public Dictionary<string, SpriteCoordinate> Coords { get; set; } = new();
}

/// <summary>
/// Represents a background image for a node group
/// </summary>
public class GroupBackground
{
    public string Image { get; set; } = string.Empty;  // e.g. "PSGroupBackground3"
    public bool IsHalfImage { get; set; }
}
