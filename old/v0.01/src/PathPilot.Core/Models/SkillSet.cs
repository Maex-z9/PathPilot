using System.Collections.Generic;
using System.Linq;

namespace PathPilot.Core.Models
{
    /// <summary>
    /// Represents a skill loadout in PoB (can have multiple per build)
    /// </summary>
    public class SkillSet
    {
        /// <summary>
        /// Title/name of this skill set
        /// </summary>
        public string Title { get; set; } = "Default";

        /// <summary>
        /// All link groups in this skill set
        /// </summary>
        public List<GemLinkGroup> LinkGroups { get; set; } = new List<GemLinkGroup>();

        /// <summary>
        /// Total number of gems across all link groups
        /// </summary>
        public int TotalGems => LinkGroups.Sum(lg => lg.LinkCount);

        /// <summary>
        /// Number of active skill gems
        /// </summary>
        public int ActiveGemCount => LinkGroups
            .SelectMany(lg => lg.Gems)
            .Count(g => g.Type == GemType.Active);

        /// <summary>
        /// Number of support gems
        /// </summary>
        public int SupportGemCount => LinkGroups
            .SelectMany(lg => lg.Gems)
            .Count(g => g.Type == GemType.Support);

        /// <summary>
        /// Gets all gems flattened from all link groups
        /// </summary>
        public IEnumerable<Gem> GetAllGems()
        {
            return LinkGroups.SelectMany(lg => lg.Gems);
        }

        /// <summary>
        /// Gets only enabled link groups
        /// </summary>
        public IEnumerable<GemLinkGroup> GetEnabledLinkGroups()
        {
            return LinkGroups.Where(lg => lg.IsEnabled);
        }

        public override string ToString()
        {
            return $"{Title} - {LinkGroups.Count} link groups, {TotalGems} gems";
        }
    }
}
