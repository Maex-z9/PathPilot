using System.Collections.Generic;

namespace PathPilot.Core.Models
{
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
        public Item GetItemBySlot(string slot)
        {
            return Items?.FirstOrDefault(i => 
                i.Slot.Equals(slot, StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString()
        {
            return $"{Title} - {Items?.Count ?? 0} items";
        }
    }

    /// <summary>
    /// Represents a single item (gear piece)
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Item name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Equipment slot (Helmet, Body Armour, Weapon, etc.)
        /// </summary>
        public string Slot { get; set; }

        /// <summary>
        /// Item rarity (Normal, Magic, Rare, Unique)
        /// </summary>
        public string Rarity { get; set; } = "Normal";

        /// <summary>
        /// Item base type
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// Raw item text from PoB (includes all mods, stats, etc.)
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// Number of sockets (if applicable)
        /// </summary>
        public int Sockets { get; set; }

        /// <summary>
        /// Socket colors (if applicable)
        /// </summary>
        public string SocketColors { get; set; }

        /// <summary>
        /// Required level to equip
        /// </summary>
        public int RequiredLevel { get; set; }

        /// <summary>
        /// Whether this is a unique item
        /// </summary>
        public bool IsUnique => Rarity?.Equals("Unique", StringComparison.OrdinalIgnoreCase) ?? false;

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                    return $"Empty {Slot}";
                return Name;
            }
        }

        public override string ToString()
        {
            return $"{Name} ({Slot}) - {Rarity}";
        }
    }
}
