namespace PathPilot.Core.Models;

/// <summary>
/// Represents a skill tree loadout (can have multiple per build)
/// </summary>
public class SkillTreeSet
{
    /// <summary>
    /// Title/name of this tree set
    /// </summary>
    public string Title { get; set; } = "Default";

    /// <summary>
    /// The encoded tree URL from PoB
    /// </summary>
    public string TreeUrl { get; set; } = string.Empty;

    /// <summary>
    /// All allocated passive skill node IDs
    /// </summary>
    public List<int> AllocatedNodes { get; set; } = new();

    /// <summary>
    /// Keystone names that are allocated
    /// </summary>
    public List<string> Keystones { get; set; } = new();

    /// <summary>
    /// Notable names that are allocated
    /// </summary>
    public List<string> Notables { get; set; } = new();

    /// <summary>
    /// Mastery effects selected
    /// </summary>
    public List<string> Masteries { get; set; } = new();

    /// <summary>
    /// Total points allocated
    /// </summary>
    public int PointsUsed { get; set; }

    /// <summary>
    /// Ascendancy points allocated
    /// </summary>
    public int AscendancyPointsUsed { get; set; }
}

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
