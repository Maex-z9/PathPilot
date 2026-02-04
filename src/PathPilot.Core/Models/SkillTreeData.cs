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
}
