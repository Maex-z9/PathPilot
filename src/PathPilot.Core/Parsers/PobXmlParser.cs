using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PathPilot.Core.Models;
using PathPilot.Core.Services;

namespace PathPilot.Core.Parsers
{
    public class PobXmlParser
    {
        private readonly GemDataService _gemDataService;

        public PobXmlParser(GemDataService gemDataService)
        {
            _gemDataService = gemDataService ?? throw new ArgumentNullException(nameof(gemDataService));
        }

        /// <summary>
        /// Parses the decompressed PoB XML into a Build object with proper link groups
        /// </summary>
        public Build Parse(string xmlContent)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
                throw new ArgumentException("XML content cannot be empty", nameof(xmlContent));

            var build = new Build();
            
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                if (root == null)
                    throw new InvalidOperationException("XML root element not found");

                // Parse build metadata
                ParseBuildMetadata(root, build);

                // Parse all skill sets (loadouts)
                ParseSkillSets(root, build);

                // Parse items for each item set
                ParseItemSets(root, build);

                // Parse skill tree (TODO: future implementation)
                // ParseSkillTree(root, build);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse PoB XML: {ex.Message}", ex);
            }

            return build;
        }

        private void ParseBuildMetadata(XElement root, Build build)
        {
            var buildElement = root.Element("Build");
            if (buildElement != null)
            {
                build.Name = buildElement.Attribute("level")?.Value ?? "Unnamed Build";
                build.ClassName = buildElement.Attribute("className")?.Value ?? "Unknown";
                build.Level = int.TryParse(buildElement.Attribute("level")?.Value, out var level) ? level : 1;
                build.Ascendancy = buildElement.Attribute("ascendClassName")?.Value ?? string.Empty;
            }
        }

        private void ParseSkillSets(XElement root, Build build)
        {
            var skillsElement = root.Element("Skills");
            if (skillsElement == null)
                return;

            // Get all skill set elements (different loadouts)
            var skillSetElements = skillsElement.Elements("SkillSet");
            
            if (!skillSetElements.Any())
            {
                // No explicit skill sets, parse as single default set
                var defaultSkillSet = ParseSingleSkillSet(skillsElement, "Default");
                build.SkillSets.Add(defaultSkillSet);
            }
            else
            {
                // Parse each skill set (loadout)
                foreach (var skillSetElement in skillSetElements)
                {
                    var skillSet = ParseSingleSkillSet(skillSetElement, 
                        skillSetElement.Attribute("title")?.Value ?? "Unnamed Loadout");
                    build.SkillSets.Add(skillSet);
                }
            }
        }

        private SkillSet ParseSingleSkillSet(XElement skillSetElement, string title)
        {
            var skillSet = new SkillSet
            {
                Title = title,
                LinkGroups = new List<GemLinkGroup>()
            };

            // Parse each Skill element (these are the actual link groups)
            var skillElements = skillSetElement.Elements("Skill");
            int linkGroupIndex = 1;

            foreach (var skillElement in skillElements)
            {
                var linkGroup = ParseLinkGroup(skillElement, linkGroupIndex);
                if (linkGroup.Gems.Any())
                {
                    skillSet.LinkGroups.Add(linkGroup);
                    linkGroupIndex++;
                }
            }

            return skillSet;
        }

        private GemLinkGroup ParseLinkGroup(XElement skillElement, int groupIndex)
        {
            var slot = skillElement.Attribute("slot")?.Value ?? $"Group {groupIndex}";
            var isEnabled = ParseBoolAttribute(skillElement.Attribute("enabled"), true);
            var mainActiveSkillIndex = int.TryParse(skillElement.Attribute("mainActiveSkill")?.Value, out var idx) 
                ? idx : 1;

            var linkGroup = new GemLinkGroup
            {
                Key = slot,
                Gems = new List<Gem>(),
                IsEnabled = isEnabled,
                SocketGroup = slot
            };

            // Parse all gems in this link group
            var gemElements = skillElement.Elements("Gem");
            int gemIndexInGroup = 1;

            foreach (var gemElement in gemElements)
            {
                var gem = ParseGem(gemElement, slot, gemIndexInGroup, mainActiveSkillIndex);
                if (gem != null)
                {
                    linkGroup.Gems.Add(gem);
                    gemIndexInGroup++;
                }
            }

            return linkGroup;
        }

        private Gem ParseGem(XElement gemElement, string linkGroupKey, int indexInGroup, int mainActiveSkillIndex)
        {
            var gemName = gemElement.Attribute("nameSpec")?.Value 
                ?? gemElement.Attribute("skillId")?.Value
                ?? gemElement.Attribute("gemId")?.Value;

            if (string.IsNullOrWhiteSpace(gemName))
                return null;

            // Clean up gem name (remove quality suffixes, etc.)
            gemName = CleanGemName(gemName);

            var gem = new Gem
            {
                Name = gemName,
                Level = int.TryParse(gemElement.Attribute("level")?.Value, out var lvl) ? lvl : 1,
                Quality = int.TryParse(gemElement.Attribute("quality")?.Value, out var qual) ? qual : 0,
                IsEnabled = ParseBoolAttribute(gemElement.Attribute("enabled"), true),
                LinkGroup = linkGroupKey,
                IndexInGroup = indexInGroup
            };

            // Determine if this is the main active skill in the group
            gem.IsMainActiveSkill = indexInGroup == mainActiveSkillIndex;

            // Enrich gem data from database (type, color, acquisition info)
            EnrichGemFromDatabase(gem);

            return gem;
        }

        private void EnrichGemFromDatabase(Gem gem)
        {
            var gemData = _gemDataService.GetGemInfo(gem.Name);

            if (gemData != null)
            {
                // Infer type from gem name
                gem.Type = gem.Name.Contains("Support") ? GemType.Support : GemType.Active;

                // Parse color string to enum
                gem.Color = ParseSocketColor(gemData.Color);

                // Format acquisition info from sources
                var earliestSource = gemData.Sources.OrderBy(s => s.Act).FirstOrDefault();
                if (earliestSource != null)
                {
                    gem.AcquisitionInfo = $"Act {earliestSource.Act}: {earliestSource.QuestName ?? "Vendor"} ({earliestSource.VendorName})";
                }
                else
                {
                    gem.AcquisitionInfo = "Drop only";
                }
            }
            else
            {
                // Fallback: try to infer from gem name
                gem.Type = gem.Name.Contains("Support") ? GemType.Support : GemType.Active;
                gem.Color = SocketColor.White;
                gem.AcquisitionInfo = "Source unknown - gem not in database";
            }
        }

        private SocketColor ParseSocketColor(string color)
        {
            return color?.ToLowerInvariant() switch
            {
                "red" => SocketColor.Red,
                "green" => SocketColor.Green,
                "blue" => SocketColor.Blue,
                _ => SocketColor.White
            };
        }

        private void ParseItemSets(XElement root, Build build)
        {
            var itemsElement = root.Element("Items");
            if (itemsElement == null)
                return;

            // Get all item set elements (different gear loadouts)
            var itemSetElements = itemsElement.Elements("ItemSet");
            
            if (!itemSetElements.Any())
            {
                // No explicit item sets, parse as single default set
                var defaultItemSet = ParseSingleItemSet(itemsElement, "Default");
                build.ItemSets.Add(defaultItemSet);
            }
            else
            {
                // Parse each item set
                foreach (var itemSetElement in itemSetElements)
                {
                    var itemSet = ParseSingleItemSet(itemSetElement, 
                        itemSetElement.Attribute("title")?.Value ?? "Unnamed Gear Set");
                    build.ItemSets.Add(itemSet);
                }
            }
        }

        private ItemSet ParseSingleItemSet(XElement itemSetElement, string title)
        {
            var itemSet = new ItemSet
            {
                Title = title,
                Items = new List<Item>()
            };

            // Parse each Item element
            var itemElements = itemSetElement.Elements("Item");

            foreach (var itemElement in itemElements)
            {
                var item = ParseItem(itemElement);
                if (item != null)
                {
                    itemSet.Items.Add(item);
                }
            }

            return itemSet;
        }

        private Item ParseItem(XElement itemElement)
        {
            var slot = itemElement.Attribute("id")?.Value ?? "Unknown";
            var itemText = itemElement.Value?.Trim();

            if (string.IsNullOrWhiteSpace(itemText))
                return null;

            // Parse the item text (PoB stores items in a special text format)
            var lines = itemText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            if (lines.Count == 0)
                return null;

            // First line is usually the item name
            var itemName = lines[0];
            
            // Try to find rarity line
            var rarity = "Normal";
            var rarityLine = lines.FirstOrDefault(l => l.StartsWith("Rarity:", StringComparison.OrdinalIgnoreCase));
            if (rarityLine != null)
            {
                rarity = rarityLine.Split(':')[1].Trim();
            }

            // Try to find item base type
            var baseType = lines.Count > 1 ? lines[1] : itemName;

            var item = new Item
            {
                Name = itemName,
                Slot = NormalizeSlotName(slot),
                Rarity = rarity,
                BaseType = baseType,
                RawText = itemText
            };

            return item;
        }

        private string CleanGemName(string gemName)
        {
            if (string.IsNullOrWhiteSpace(gemName))
                return gemName;

            // Remove common suffixes
            gemName = gemName.Replace(" (Transfigured)", "")
                .Replace(" (Awakened)", "")
                .Replace(" (Anomalous)", "")
                .Replace(" (Divergent)", "")
                .Replace(" (Phantasmal)", "")
                .Trim();

            return gemName;
        }

        private string NormalizeSlotName(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return "Unknown";

            // Normalize common slot names
            return slot switch
            {
                "Weapon1" or "Weapon 1" => "Main Hand",
                "Weapon2" or "Weapon 2" => "Off Hand",
                "Helm" => "Helmet",
                "BodyArmour" or "Body Armour" => "Body Armour",
                "Gloves" => "Gloves",
                "Boots" => "Boots",
                "Amulet" => "Amulet",
                "Ring1" or "Ring 1" => "Ring 1",
                "Ring2" or "Ring 2" => "Ring 2",
                "Belt" => "Belt",
                "Flask1" or "Flask 1" => "Flask 1",
                "Flask2" or "Flask 2" => "Flask 2",
                "Flask3" or "Flask 3" => "Flask 3",
                "Flask4" or "Flask 4" => "Flask 4",
                "Flask5" or "Flask 5" => "Flask 5",
                _ => slot
            };
        }

        private bool ParseBoolAttribute(XAttribute attribute, bool defaultValue)
        {
            if (attribute == null)
                return defaultValue;

            var value = attribute.Value?.ToLowerInvariant();
            return value switch
            {
                "true" or "1" or "yes" => true,
                "false" or "0" or "no" => false,
                _ => defaultValue
            };
        }
    }
}
