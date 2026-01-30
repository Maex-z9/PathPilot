namespace PathPilot.Core.Models;

/// <summary>
/// Represents a skill gem in the build
/// </summary>
public class Gem
{
    /// <summary>
    /// Name of the gem (e.g. "Lightning Strike", "Multistrike Support")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of gem
    /// </summary>
    public GemType Type { get; set; }
    
    /// <summary>
    /// Required character level to use this gem
    /// </summary>
    public int RequiredLevel { get; set; }
    
    /// <summary>
    /// Current level of the gem
    /// </summary>
    public int Level { get; set; } = 1;
    
    /// <summary>
    /// Target level for the gem
    /// </summary>
    public int TargetLevel { get; set; }
    
    /// <summary>
    /// Quality percentage (0-20, or higher for exceptional gems)
    /// </summary>
    public int Quality { get; set; }
    
    /// <summary>
    /// Where this gem can be obtained
    /// </summary>
    public List<GemSource> Sources { get; set; } = new();
    
    /// <summary>
    /// Which gem link group this belongs to (e.g. main skill, aura setup)
    /// </summary>
    public string LinkGroup { get; set; } = string.Empty;
    
    /// <summary>
    /// Socket color requirement (R, G, B, W for white)
    /// </summary>
    public SocketColor Color { get; set; }
    
    /// <summary>
    /// Is this gem enabled in the build?
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Acquisition information (where to get this gem)
    /// </summary>
    public string AcquisitionInfo { get; set; } = string.Empty;
}

/// <summary>
/// Types of gems
/// </summary>
public enum GemType
{
    Active,
    Support,
    Aura,
    Curse,
    Herald,
    Vaal
}

/// <summary>
/// Socket colors
/// </summary>
public enum SocketColor
{
    Red,      // Strength
    Green,    // Dexterity
    Blue,     // Intelligence
    White     // Any (prismatic/corrupted)
}

/// <summary>
/// Where a gem can be obtained
/// </summary>
public class GemSource
{
    /// <summary>
    /// Act number (1-10)
    /// </summary>
    public int Act { get; set; }
    
    /// <summary>
    /// Type of source (quest reward, vendor, drop)
    /// </summary>
    public SourceType Type { get; set; }
    
    /// <summary>
    /// Quest name if from a quest reward
    /// </summary>
    public string? QuestName { get; set; }
    
    /// <summary>
    /// NPC name if from a vendor
    /// </summary>
    public string? VendorName { get; set; }
    
    /// <summary>
    /// Classes that can get this gem from this source
    /// </summary>
    public List<string> AvailableForClasses { get; set; } = new();
}

/// <summary>
/// How a gem is obtained
/// </summary>
public enum SourceType
{
    QuestReward,
    Vendor,
    Drop,
    Siosa,      // Library vendor in Act 3
    Lilly       // Vendor in Act 6 and 10
}