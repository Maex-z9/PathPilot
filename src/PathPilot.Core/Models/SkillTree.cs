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
    public bool IsClassStart { get; set; }
    public int? ClassStartIndex { get; set; }

    /// <summary>
    /// Icon sprite key (e.g. "Art/2DArt/SkillIcons/passives/2handeddamage.png")
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Available mastery effects (for mastery nodes only).
    /// Each entry has an effect ID and stat descriptions.
    /// </summary>
    public List<MasteryEffect> MasteryEffects { get; set; } = new();

    // Position data for tree rendering
    public int? Group { get; set; }
    public int? Orbit { get; set; }
    public int? OrbitIndex { get; set; }
    public string? AscendancyName { get; set; }

    /// <summary>
    /// Calculated X position (set during rendering)
    /// </summary>
    public float? CalculatedX { get; set; }

    /// <summary>
    /// Calculated Y position (set during rendering)
    /// </summary>
    public float? CalculatedY { get; set; }
}

/// <summary>
/// A selectable mastery effect on a mastery node
/// </summary>
public class MasteryEffect
{
    public int EffectId { get; set; }
    public List<string> Stats { get; set; } = new();
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

/// <summary>
/// Helper for calculating node positions from group/orbit data
/// </summary>
public static class SkillTreePositionHelper
{
    // Orbit radii from GGG constants (orbitRadii in data.json)
    private static readonly float[] OrbitRadii = { 0, 82, 162, 335, 493, 662, 846 };

    // Nodes per orbit from GGG constants (skillsPerOrbit in data.json)
    private static readonly int[] NodesPerOrbit = { 1, 6, 16, 16, 40, 72, 72 };

    /// <summary>
    /// Calculates absolute position for a node given its group position
    /// </summary>
    public static (float X, float Y) CalculateNodePosition(
        PassiveNode node,
        float groupX,
        float groupY)
    {
        if (node.Orbit == null || node.OrbitIndex == null)
            return (groupX, groupY);

        var orbit = node.Orbit.Value;
        var orbitIndex = node.OrbitIndex.Value;

        if (orbit < 0 || orbit >= OrbitRadii.Length)
            return (groupX, groupY);

        var radius = OrbitRadii[orbit];
        var nodesInOrbit = NodesPerOrbit[orbit];

        // Calculate angle (radians, starting from top)
        var angle = (2 * Math.PI * orbitIndex / nodesInOrbit) - (Math.PI / 2);

        var x = groupX + (float)(radius * Math.Cos(angle));
        var y = groupY + (float)(radius * Math.Sin(angle));

        return (x, y);
    }

    /// <summary>
    /// Calculates positions for all nodes in tree data
    /// </summary>
    public static void CalculateAllPositions(SkillTreeData treeData)
    {
        foreach (var node in treeData.Nodes.Values)
        {
            if (node.Group == null) continue;

            if (treeData.Groups.TryGetValue(node.Group.Value, out var group))
            {
                var (x, y) = CalculateNodePosition(node, group.X, group.Y);
                node.CalculatedX = x;
                node.CalculatedY = y;
            }
        }
    }
}
