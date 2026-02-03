using System;
using System.Collections.Generic;
using System.Linq;

namespace PathPilot.Core.Models;

/// <summary>
/// Represents an item set/loadout in PoB
/// </summary>
public class ItemSet
{
    /// <summary>
    /// Title/name of this item set
    /// </summary>
    public string Title { get; set; } = "Default";
    
    /// <summary>
    /// All items in this set
    /// </summary>
    public List<Item> Items { get; set; } = new List<Item>();
    
    /// <summary>
    /// Gets item by slot name
    /// </summary>
    public Item? GetItemBySlot(string slot)
    {
        return Items?.FirstOrDefault(i =>
            string.Equals(i.Slot, slot, StringComparison.OrdinalIgnoreCase));
    }
    
    public override string ToString()
    {
        return $"{Title} - {Items?.Count ?? 0} items";
    }
}