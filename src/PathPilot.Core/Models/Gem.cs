using System;

namespace PathPilot.Core.Models
{
    public class Gem
    {
        /// <summary>
        /// Gem name as it appears in PoB
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gem level
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Gem quality percentage
        /// </summary>
        public int Quality { get; set; } = 0;

        /// <summary>
        /// Type of gem (Active or Support)
        /// </summary>
        public GemType Type { get; set; } = GemType.Active;

        /// <summary>
        /// Socket color requirement
        /// </summary>
        public SocketColor Color { get; set; } = SocketColor.White;

        /// <summary>
        /// Whether the gem is enabled in PoB
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Link group identifier (e.g., "Body Armour", "Weapon 1")
        /// </summary>
        public string LinkGroup { get; set; } = string.Empty;

        /// <summary>
        /// Position within the link group (1-indexed)
        /// </summary>
        public int IndexInGroup { get; set; }

        /// <summary>
        /// Whether this is the main active skill in the link group
        /// </summary>
        public bool IsMainActiveSkill { get; set; }

        /// <summary>
        /// Acquisition information (quest/vendor/drop)
        /// </summary>
        public string AcquisitionInfo { get; set; } = string.Empty;

        /// <summary>
        /// Convenience property for checking if gem is a support gem
        /// </summary>
        public bool IsSupport => Type == GemType.Support;

        /// <summary>
        /// Display name with level and quality
        /// </summary>
        public string DisplayName
        {
            get
            {
                var display = Name;
                if (Quality > 0)
                    display += $" ({Quality}%)";
                return display;
            }
        }

        /// <summary>
        /// Short color name for UI display
        /// </summary>
        public string ColorName => Color.ToString();

        public override string ToString()
        {
            return $"{Name} (Lvl {Level}, {Color})";
        }
    }

    public enum GemType
    {
        Active,
        Support
    }

    public enum SocketColor
    {
        Red,
        Green,
        Blue,
        White
    }
}
