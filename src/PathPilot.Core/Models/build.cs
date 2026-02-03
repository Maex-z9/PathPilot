using System.Collections.Generic;
using System.Linq;

namespace PathPilot.Core.Models
{
    /// <summary>
    /// Represents a complete Path of Building build
    /// </summary>
    public class Build
    {
        /// <summary>
        /// Build name/title
        /// </summary>
        public string Name { get; set; } = "Unnamed Build";

        /// <summary>
        /// Character class (Marauder, Witch, etc.)
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Character level
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Ascendancy class (Juggernaut, Necromancer, etc.)
        /// </summary>
        public string Ascendancy { get; set; } = string.Empty;

        /// <summary>
        /// All skill sets (loadouts) in this build
        /// </summary>
        public List<SkillSet> SkillSets { get; set; } = new List<SkillSet>();

        /// <summary>
        /// All item sets (gear loadouts) in this build
        /// </summary>
        public List<ItemSet> ItemSets { get; set; } = new List<ItemSet>();

        /// <summary>
        /// Currently selected skill set index
        /// </summary>
        public int ActiveSkillSetIndex { get; set; } = 0;

        /// <summary>
        /// Currently selected item set index
        /// </summary>
        public int ActiveItemSetIndex { get; set; } = 0;

        /// <summary>
        /// Gets the currently active skill set
        /// </summary>
        public SkillSet? ActiveSkillSet
        {
            get
            {
                if (SkillSets.Count == 0)
                    return null;

                if (ActiveSkillSetIndex < 0 || ActiveSkillSetIndex >= SkillSets.Count)
                    ActiveSkillSetIndex = 0;

                return SkillSets[ActiveSkillSetIndex];
            }
        }

        /// <summary>
        /// Gets the currently active item set
        /// </summary>
        public ItemSet? ActiveItemSet
        {
            get
            {
                if (ItemSets.Count == 0)
                    return null;

                if (ActiveItemSetIndex < 0 || ActiveItemSetIndex >= ItemSets.Count)
                    ActiveItemSetIndex = 0;

                return ItemSets[ActiveItemSetIndex];
            }
        }

        /// <summary>
        /// Total number of gems across all skill sets
        /// </summary>
        public int TotalGems => SkillSets.Sum(ss => ss.TotalGems);

        /// <summary>
        /// Total number of items across all item sets
        /// </summary>
        public int TotalItems => ItemSets.Sum(items => items.Items.Count);

        /// <summary>
        /// Full character description
        /// </summary>
        public string CharacterDescription
        {
            get
            {
                var desc = $"Level {Level} {ClassName}";
                if (!string.IsNullOrWhiteSpace(Ascendancy))
                    desc += $" ({Ascendancy})";
                return desc;
            }
        }

        /// <summary>
        /// Gets all unique gems required across all skill sets
        /// </summary>
        public IEnumerable<Gem> GetAllUniqueGems()
        {
            return SkillSets
                .SelectMany(ss => ss.GetAllGems())
                .GroupBy(g => g.Name)
                .Select(group => group.First());
        }

        public override string ToString()
        {
            return $"{Name} - {CharacterDescription} - {SkillSets.Count} loadouts";
        }
    }
}
