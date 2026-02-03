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
        /// Passive skill trees for each loadout
        /// </summary>
        public List<SkillTreeSet> TreeSets { get; set; } = new List<SkillTreeSet>();

        /// <summary>
        /// Currently selected tree set index
        /// </summary>
        public int ActiveTreeSetIndex { get; set; } = 0;

        /// <summary>
        /// Gets the currently active tree set
        /// </summary>
        public SkillTreeSet? ActiveTreeSet
        {
            get
            {
                if (TreeSets.Count == 0)
                    return null;

                if (ActiveTreeSetIndex < 0 || ActiveTreeSetIndex >= TreeSets.Count)
                    ActiveTreeSetIndex = 0;

                return TreeSets[ActiveTreeSetIndex];
            }
        }

        /// <summary>
        /// Currently selected skill set index
        /// </summary>
        public int ActiveSkillSetIndex { get; set; } = 0;

        /// <summary>
        /// Currently selected item set index
        /// </summary>
        public int ActiveItemSetIndex { get; set; } = 0;

        /// <summary>
        /// Gets all unique loadout names from SkillSets, ItemSets, and TreeSets
        /// </summary>
        public List<string> GetLoadoutNames()
        {
            var skillSetNames = SkillSets.Select(s => s.Title);
            var itemSetNames = ItemSets.Select(i => i.Title);
            var treeSetNames = TreeSets.Select(t => t.Title);
            return skillSetNames.Union(itemSetNames).Union(treeSetNames).ToList();
        }

        /// <summary>
        /// Sets skill set, item set, and tree set to match the given loadout name
        /// </summary>
        public void SetActiveLoadout(string loadoutName)
        {
            // Find matching skill set
            var skillSetIndex = SkillSets.FindIndex(s => s.Title == loadoutName);
            if (skillSetIndex >= 0)
                ActiveSkillSetIndex = skillSetIndex;

            // Find matching item set
            var itemSetIndex = ItemSets.FindIndex(i => i.Title == loadoutName);
            if (itemSetIndex >= 0)
                ActiveItemSetIndex = itemSetIndex;

            // Find matching tree set
            var treeSetIndex = TreeSets.FindIndex(t => t.Title == loadoutName);
            if (treeSetIndex >= 0)
                ActiveTreeSetIndex = treeSetIndex;
        }

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
