namespace PathPilot.Core.Models;

/// <summary>
/// Represents the passive skill tree of a build
/// </summary>
public class SkillTree
{
    /// <summary>
    /// Character class (determines starting position)
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// All allocated passive skill nodes (by node ID)
    /// </summary>
    public HashSet<int> AllocatedNodes { get; set; } = new();
    
    /// <summary>
    /// Ascendancy class name
    /// </summary>
    public string Ascendancy { get; set; } = string.Empty;
    
    /// <summary>
    /// Allocated ascendancy nodes (by node ID)
    /// </summary>
    public HashSet<int> AscendancyNodes { get; set; } = new();
    
    /// <summary>
    /// Total number of passive skill points used
    /// </summary>
    public int PointsUsed { get; set; }
    
    /// <summary>
    /// Number of ascendancy points used
    /// </summary>
    public int AscendancyPointsUsed { get; set; }
    
    /// <summary>
    /// Jewel socket nodes and their socketed jewels
    /// </summary>
    public Dictionary<int, string> SocketedJewels { get; set; } = new();
    
    /// <summary>
    /// Mastery selections (node ID -> mastery effect ID)
    /// </summary>
    public Dictionary<int, int> MasterySelections { get; set; } = new();
}

/// <summary>
/// Represents a single passive skill node
/// </summary>
public class PassiveNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public List<string> Stats { get; set; } = new();
    public List<int> ConnectedNodes { get; set; } = new();
    public bool IsAscendancy { get; set; }
    public bool IsKeystone { get; set; }
    public bool IsNotable { get; set; }
    public bool IsJewelSocket { get; set; }
    public bool IsMastery { get; set; }
}

/// <summary>
/// Types of passive nodes
/// </summary>
public enum NodeType
{
    Normal,
    Notable,
    Keystone,
    JewelSocket,
    Mastery,
    AscendancyNormal,
    AscendancyNotable,
    AscendancyStart
}
