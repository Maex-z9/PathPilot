namespace PathPilot.Core.Models;

/// <summary>
/// Container for parsed GGG Skill Tree data
/// </summary>
public class SkillTreeData
{
    /// <summary>
    /// All passive nodes keyed by node ID (integer key, not string)
    /// </summary>
    public Dictionary<int, PassiveNode> Nodes { get; set; } = new();

    /// <summary>
    /// Node groups for position calculation
    /// </summary>
    public Dictionary<int, NodeGroup> Groups { get; set; } = new();

    /// <summary>
    /// Tree version/league identifier
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Total node count for validation
    /// </summary>
    public int TotalNodes => Nodes.Count;

    /// <summary>
    /// Sprite sheets organized by type and zoom level
    /// Key: sprite type (e.g. "normalActive"), Value: dict keyed by zoom string (e.g. "0.1246")
    /// </summary>
    public Dictionary<string, Dictionary<string, SpriteSheetData>> SpriteSheets { get; set; } = new();

    /// <summary>
    /// Parsed imageZoomLevels from JSON
    /// </summary>
    public List<float> ImageZoomLevels { get; set; } = new();
}

/// <summary>
/// Represents a group of nodes (used for position calculation)
/// </summary>
public class NodeGroup
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public List<int> NodeIds { get; set; } = new();
    public bool IsProxy { get; set; }
    public GroupBackground? Background { get; set; }
}
