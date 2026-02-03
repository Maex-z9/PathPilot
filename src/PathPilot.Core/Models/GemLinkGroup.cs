using System.Collections.Generic;

namespace PathPilot.Core.Models
{
    /// <summary>
    /// Represents a group of linked gems (socket group)
    /// </summary>
    public class GemLinkGroup
    {
        /// <summary>
        /// Display name/key for the link group (e.g., "Body Armour", "Weapon 1")
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The socket location (same as Key, kept for compatibility)
        /// </summary>
        public string SocketGroup { get; set; } = string.Empty;

        /// <summary>
        /// List of gems in this link group
        /// </summary>
        public List<Gem> Value { get; set; } = new List<Gem>();

        /// <summary>
        /// Convenience property - same as Value
        /// </summary>
        public List<Gem> Gems
        {
            get => Value;
            set => Value = value;
        }

        /// <summary>
        /// Whether this link group is enabled in PoB
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Number of gems in this link group
        /// </summary>
        public int LinkCount => Gems?.Count ?? 0;

        /// <summary>
        /// Gets the main active skill gem in this link group
        /// </summary>
        public Gem? MainActiveGem => Gems?.FirstOrDefault(g => g.IsMainActiveSkill);

        /// <summary>
        /// Display name for UI (shows link count)
        /// </summary>
        public string DisplayName => $"{Key} ({LinkCount}L)";

        public override string ToString()
        {
            return $"{Key} - {LinkCount} gems";
        }
    }
}
