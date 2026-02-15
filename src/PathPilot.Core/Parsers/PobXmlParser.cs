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

                // Parse skill trees
                ParseTreeSets(root, build);
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

                // Set icon URL
                gem.IconUrl = gemData.IconUrl;

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
                gem.AcquisitionInfo = "Drop only";
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

            // Step 1: Parse ALL items into a dictionary by ID
            var itemsById = new Dictionary<string, Item>();
            foreach (var itemElement in itemsElement.Elements("Item"))
            {
                var id = itemElement.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(id))
                {
                    var item = ParseItem(itemElement);
                    if (item != null)
                    {
                        itemsById[id] = item;
                    }
                }
            }
            Console.WriteLine($"Parsed {itemsById.Count} items from Items element");

            // Step 2: Parse ItemSets and resolve slot references
            var itemSetElements = itemsElement.Elements("ItemSet");

            if (!itemSetElements.Any())
            {
                // No explicit item sets, create default from all items
                var defaultItemSet = new ItemSet
                {
                    Title = "Default",
                    Items = itemsById.Values.ToList()
                };
                build.ItemSets.Add(defaultItemSet);
            }
            else
            {
                foreach (var itemSetElement in itemSetElements)
                {
                    var itemSet = ParseSingleItemSet(itemSetElement, itemsById,
                        itemSetElement.Attribute("title")?.Value ?? "Unnamed Gear Set");
                    build.ItemSets.Add(itemSet);
                }
            }
        }

        private ItemSet ParseSingleItemSet(XElement itemSetElement, Dictionary<string, Item> itemsById, string title)
        {
            var itemSet = new ItemSet
            {
                Title = title,
                Items = new List<Item>()
            };

            // Parse Slot elements that reference items by ID
            foreach (var slotElement in itemSetElement.Elements("Slot"))
            {
                var slotName = slotElement.Attribute("name")?.Value;
                var itemId = slotElement.Attribute("itemId")?.Value;

                if (!string.IsNullOrEmpty(itemId) && itemsById.TryGetValue(itemId, out var item))
                {
                    // Clone the item and set the slot from the Slot element
                    var slotItem = new Item
                    {
                        Name = item.Name,
                        Slot = NormalizeSlotName(slotName ?? item.Slot),
                        Rarity = item.Rarity,
                        BaseType = item.BaseType,
                        RawText = item.RawText
                    };
                    itemSet.Items.Add(slotItem);
                }
            }

            Console.WriteLine($"ItemSet '{title}': {itemSet.Items.Count} items loaded");
            return itemSet;
        }

        private Item ParseItem(XElement itemElement)
        {
            var id = itemElement.Attribute("id")?.Value ?? "Unknown";
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

            // PoB format:
            // Line 0: Rarity: RARE/UNIQUE/MAGIC/NORMAL
            // Line 1: Item Name (for rare/unique) or Base Type (for normal/magic)
            // Line 2: Base Type (for rare/unique)
            // Then: properties like "Sockets:", "LevelReq:", "Implicits:"
            // Then: mods

            var rarity = "Normal";
            var itemName = "";
            var baseType = "";
            var mods = new List<string>();
            int modsStartIndex = 0;

            // Parse rarity
            if (lines[0].StartsWith("Rarity:", StringComparison.OrdinalIgnoreCase))
            {
                rarity = lines[0].Split(':')[1].Trim().ToUpperInvariant();

                if (rarity == "UNIQUE" || rarity == "RARE")
                {
                    // Line 1 = name, Line 2 = base type
                    itemName = lines.Count > 1 ? lines[1] : "Unknown";
                    baseType = lines.Count > 2 ? lines[2] : itemName;
                    modsStartIndex = 3;
                }
                else
                {
                    // Magic/Normal: Line 1 = base type (may include prefix/suffix in name)
                    baseType = lines.Count > 1 ? lines[1] : "Unknown";
                    itemName = baseType;
                    modsStartIndex = 2;
                }
            }
            else
            {
                // No rarity line - treat first line as name
                itemName = lines[0];
                baseType = lines.Count > 1 ? lines[1] : itemName;
                modsStartIndex = 2;
            }

            // Parse mods - skip property lines and collect actual mods
            bool inMods = false;
            int implicits = 0;

            for (int i = modsStartIndex; i < lines.Count; i++)
            {
                var line = lines[i];

                // Skip known property lines
                if (line.StartsWith("Sockets:") ||
                    line.StartsWith("LevelReq:") ||
                    line.StartsWith("ItemLvl:") ||
                    line.StartsWith("Quality:") ||
                    line.StartsWith("Armour:") ||
                    line.StartsWith("Evasion:") ||
                    line.StartsWith("Energy Shield:") ||
                    line.StartsWith("Ward:") ||
                    line.StartsWith("Chance to Block:") ||
                    line.StartsWith("Physical Damage:") ||
                    line.StartsWith("Critical Strike Chance:") ||
                    line.StartsWith("Attacks per Second:") ||
                    line.StartsWith("Weapon Range:") ||
                    line.StartsWith("Limited to:") ||
                    line.StartsWith("Radius:"))
                {
                    continue;
                }

                // Check for implicits count
                if (line.StartsWith("Implicits:"))
                {
                    int.TryParse(line.Split(':')[1].Trim(), out implicits);
                    inMods = true;
                    continue;
                }

                // Collect mods
                if (inMods || i >= modsStartIndex + 3)
                {
                    // Clean up mod text
                    var mod = line.Replace("{crafted}", "[C]")
                                  .Replace("{fractured}", "[F]")
                                  .Replace("{range:", "")
                                  .Replace("}", "");

                    if (!string.IsNullOrWhiteSpace(mod) && mod.Length > 2)
                    {
                        mods.Add(mod);
                    }
                }
            }

            var item = new Item
            {
                Name = itemName,
                Slot = id,
                Rarity = rarity,
                BaseType = baseType,
                RawText = itemText,
                ImportantMods = mods
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

        private void ParseTreeSets(XElement root, Build build)
        {
            var treeElement = root.Element("Tree");
            if (treeElement == null)
                return;

            // Parse each Spec element (different tree loadouts)
            var specElements = treeElement.Elements("Spec");

            if (!specElements.Any())
            {
                // No specs, try to parse the tree element itself as a single spec
                var defaultTreeSet = ParseSingleTreeSpec(treeElement, "Default");
                if (defaultTreeSet != null)
                    build.TreeSets.Add(defaultTreeSet);
            }
            else
            {
                foreach (var specElement in specElements)
                {
                    var title = specElement.Attribute("title")?.Value ?? "Unnamed Tree";
                    var treeSet = ParseSingleTreeSpec(specElement, title);
                    if (treeSet != null)
                        build.TreeSets.Add(treeSet);
                }
            }

            Console.WriteLine($"TreeSets: {build.TreeSets.Count}");
        }

        private SkillTreeSet? ParseSingleTreeSpec(XElement specElement, string title)
        {
            var treeSet = new SkillTreeSet
            {
                Title = title
            };

            // Get the tree URL from the URL element
            var urlElement = specElement.Element("URL");
            if (urlElement != null)
            {
                treeSet.TreeUrl = urlElement.Value?.Trim() ?? string.Empty;
            }

            // Parse Sockets (jewel sockets and their jewels)
            var socketsElement = specElement.Element("Sockets");
            // Future: parse socketed jewels

            // Parse allocated nodes from the URL or nodes element
            // PoB encodes nodes in a base64 URL format, but also has a Nodes element

            // For now, extract points from the treeVersion and count nodes
            var treeVersion = specElement.Attribute("treeVersion")?.Value;

            // Count ascendancy points from ascendClassId
            var ascendClassId = specElement.Attribute("ascendClassId")?.Value;
            if (!string.IsNullOrEmpty(ascendClassId) && ascendClassId != "0")
            {
                treeSet.AscendancyPointsUsed = 8; // Assume full ascendancy if selected
            }

            // Parse keystones, notables from the URL
            // The tree URL contains encoded node data - we'll extract stats from notes instead
            var notesElement = specElement.Element("Notes");
            if (notesElement != null)
            {
                // Notes sometimes contain build-relevant info
            }

            // Decode allocated nodes from tree URL
            if (!string.IsNullOrEmpty(treeSet.TreeUrl))
            {
                treeSet.AllocatedNodes = TreeUrlDecoder.DecodeAllocatedNodes(treeSet.TreeUrl);
                treeSet.PointsUsed = treeSet.AllocatedNodes.Count;
            }

            return treeSet;
        }
    }
}
