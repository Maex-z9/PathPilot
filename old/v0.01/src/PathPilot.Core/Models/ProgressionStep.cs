namespace PathPilot.Core.Models;

/// <summary>
/// Represents the current progression state of a character following a build
/// </summary>
public class BuildProgression
{
    /// <summary>
    /// The target build being followed
    /// </summary>
    public Build TargetBuild { get; set; } = new();
    
    /// <summary>
    /// Current character level
    /// </summary>
    public int CurrentLevel { get; set; }
    
    /// <summary>
    /// Currently allocated passive nodes
    /// </summary>
    public HashSet<int> AllocatedNodes { get; set; } = new();
    
    /// <summary>
    /// Currently acquired gems
    /// </summary>
    public List<string> AcquiredGems { get; set; } = new();
    
    /// <summary>
    /// Currently owned items
    /// </summary>
    public List<string> AcquiredItems { get; set; } = new();
    
    /// <summary>
    /// Next recommended steps
    /// </summary>
    public List<ProgressionStep> NextSteps { get; set; } = new();
}

/// <summary>
/// Represents a single step in the build progression
/// </summary>
public class ProgressionStep
{
    /// <summary>
    /// Type of step
    /// </summary>
    public StepType Type { get; set; }
    
    /// <summary>
    /// Priority (1 = highest priority)
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Level requirement for this step
    /// </summary>
    public int RequiredLevel { get; set; }
    
    /// <summary>
    /// Description of what to do
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Details specific to the step type
    /// </summary>
    public object? Details { get; set; }
}

/// <summary>
/// Types of progression steps
/// </summary>
public enum StepType
{
    AllocatePassiveNode,
    AcquireGem,
    UpgradeGem,
    SocketGem,
    AcquireItem,
    LinkItem,
    AllocateAscendancy,
    CompleteLabyrinth
}

/// <summary>
/// Details for allocating a passive node
/// </summary>
public class PassiveNodeStep
{
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public List<int> PathToNode { get; set; } = new();
    public int PathCost { get; set; }
}

/// <summary>
/// Details for acquiring a gem
/// </summary>
public class GemAcquisitionStep
{
    public string GemName { get; set; } = string.Empty;
    public GemSource BestSource { get; set; } = new();
    public List<GemSource> AlternativeSources { get; set; } = new();
}

/// <summary>
/// Details for item socket/link requirements
/// </summary>
public class ItemLinkingStep
{
    public string ItemName { get; set; } = string.Empty;
    public ItemSlot Slot { get; set; }
    public int RequiredLinks { get; set; }
    public List<SocketColor> RequiredColors { get; set; } = new();
    public List<string> GemsToSocket { get; set; } = new();
}
