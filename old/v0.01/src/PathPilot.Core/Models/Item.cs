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
    /// Item slot
    /// </summary>
    public ItemSlot Slot { get; set; }
    
    /// <summary>
    /// Item rarity
    /// </summary>
    public ItemRarity Rarity { get; set; }
    
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
