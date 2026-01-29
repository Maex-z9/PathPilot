namespace PathPilot.Core.Models;

/// <summary>
/// Represents a complete Path of Building character build
/// </summary>
public class Build
{
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Ascendancy { get; set; } = string.Empty;
    public int Level { get; set; }
    public string MainHand { get; set; } = string.Empty;
    public string OffHand { get; set; } = string.Empty;
    
    public SkillTree? SkillTree { get; set; }
    
    /// <summary>
    /// All available skill sets (different gem configurations)
    /// </summary>
    public List<SkillSet> SkillSets { get; set; } = new();
    
    /// <summary>
    /// Index of the currently active skill set
    /// </summary>
    public int ActiveSkillSetIndex { get; set; }
    
    /// <summary>
    /// All available item sets (different gear loadouts)
    /// </summary>
    public List<ItemSet> ItemSets { get; set; } = new();
    
    /// <summary>
    /// Index of the currently active item set
    /// </summary>
    public int ActiveItemSetIndex { get; set; }
    
    /// <summary>
    /// Convenience property: Currently active skill set
    /// </summary>
    public SkillSet? ActiveSkillSet => 
        ActiveSkillSetIndex >= 0 && ActiveSkillSetIndex < SkillSets.Count 
            ? SkillSets[ActiveSkillSetIndex] 
            : SkillSets.FirstOrDefault();
    
    /// <summary>
    /// Convenience property: Currently active item set
    /// </summary>
    public ItemSet? ActiveItemSet => 
        ActiveItemSetIndex >= 0 && ActiveItemSetIndex < ItemSets.Count 
            ? ItemSets[ActiveItemSetIndex] 
            : ItemSets.FirstOrDefault();
    
    /// <summary>
    /// Legacy: All gems (from active skill set)
    /// </summary>
    public List<Gem> Gems => ActiveSkillSet?.Gems ?? new();
    
    /// <summary>
    /// Legacy: All items (from active item set)
    /// </summary>
    public List<Item> Items => ActiveItemSet?.Items ?? new();
    
    public string Notes { get; set; } = string.Empty;
    public string? PobUrl { get; set; }
}

/// <summary>
/// Represents a skill set (gem configuration)
/// </summary>
public class SkillSet
{
    public string Name { get; set; } = "Default";
    public List<Gem> Gems { get; set; } = new();
}

/// <summary>
/// Represents an item set (gear loadout)
/// </summary>
public class ItemSet
{
    public string Name { get; set; } = "Default";
    public List<Item> Items { get; set; } = new();
}
