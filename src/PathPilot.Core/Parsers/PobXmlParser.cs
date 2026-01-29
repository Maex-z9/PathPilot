using System.Xml.Linq;
using PathPilot.Core.Models;

namespace PathPilot.Core.Parsers;

/// <summary>
/// Parses Path of Building XML files into Build objects
/// </summary>
public class PobXmlParser
{
    /// <summary>
    /// Parses XML string to Build object
    /// </summary>
    public Build Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML cannot be empty", nameof(xml));
        }

        try
        {
            var doc = XDocument.Parse(xml);
            var root = doc.Root ?? throw new InvalidOperationException("XML has no root element");

            var build = new Build();

            // Parse Build metadata
            ParseBuildMetadata(root, build);

            // Parse Skill Tree
            build.SkillTree = ParseSkillTree(root);

            // Parse Skill Sets
            build.SkillSets = ParseSkillSets(root, out int activeSkillSetIndex);
            build.ActiveSkillSetIndex = activeSkillSetIndex;

            // Parse Item Sets
            build.ItemSets = ParseItemSets(root, out int activeItemSetIndex);
            build.ActiveItemSetIndex = activeItemSetIndex;

            return build;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException("Failed to parse PoB XML. The file may be corrupted or in an unsupported format.", ex);
        }
    }

    /// <summary>
    /// Parses build metadata (name, class, level, etc.)
    /// </summary>
    private void ParseBuildMetadata(XElement root, Build build)
    {
        var buildElement = root.Element("Build");
        if (buildElement != null)
        {
            build.Level = int.Parse(buildElement.Attribute("level")?.Value ?? "1");
            build.ClassName = buildElement.Attribute("className")?.Value ?? "Unknown";
            build.Ascendancy = buildElement.Attribute("ascendClassName")?.Value ?? "";
            build.Name = buildElement.Attribute("name")?.Value ?? "Unnamed Build";
            build.MainHand = buildElement.Attribute("mainSocketGroup")?.Value ?? "";
        }

        // Parse notes
        var notesElement = root.Element("Notes");
        if (notesElement != null)
        {
            build.Notes = notesElement.Value ?? "";
        }
    }

    /// <summary>
    /// Parses the passive skill tree
    /// </summary>
    private SkillTree ParseSkillTree(XElement root)
    {
        var tree = new SkillTree();

        var treeElement = root.Element("Tree");
        if (treeElement == null)
        {
            return tree;
        }

        // Parse spec (allocated nodes)
        var specElement = treeElement.Element("Spec");
        if (specElement != null)
        {
            tree.ClassName = specElement.Attribute("className")?.Value ?? "";
            tree.Ascendancy = specElement.Attribute("ascendClassName")?.Value ?? "";

            // Parse allocated nodes
            var nodesAttr = specElement.Attribute("nodes");
            if (nodesAttr != null)
            {
                var nodeIds = nodesAttr.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var nodeIdStr in nodeIds)
                {
                    if (int.TryParse(nodeIdStr.Trim(), out int nodeId))
                    {
                        tree.AllocatedNodes.Add(nodeId);
                    }
                }
            }

            tree.PointsUsed = tree.AllocatedNodes.Count;

            // Parse masteries
            var masteriesElement = specElement.Element("Masteries");
            if (masteriesElement != null)
            {
                foreach (var masteryElement in masteriesElement.Elements("Mastery"))
                {
                    int nodeId = int.Parse(masteryElement.Attribute("node")?.Value ?? "0");
                    int effectId = int.Parse(masteryElement.Attribute("effect")?.Value ?? "0");
                    
                    if (nodeId > 0 && effectId > 0)
                    {
                        tree.MasterySelections[nodeId] = effectId;
                    }
                }
            }
        }

        return tree;
    }

    /// <summary>
    /// Parses all skill sets from the build
    /// </summary>
    private List<SkillSet> ParseSkillSets(XElement root, out int activeIndex)
    {
        var skillSets = new List<SkillSet>();
        activeIndex = 0;

        var skillsElement = root.Element("Skills");
        if (skillsElement == null)
        {
            return skillSets;
        }

        // Get active skill set index
        var activeSetAttr = skillsElement.Attribute("activeSkillSet");
        if (activeSetAttr != null && int.TryParse(activeSetAttr.Value, out int activeIdx))
        {
            activeIndex = activeIdx - 1; // PoB uses 1-based indexing
        }

        int setIndex = 0;
        foreach (var skillSetElement in skillsElement.Elements("SkillSet"))
        {
            var skillSet = new SkillSet
            {
                Name = skillSetElement.Attribute("title")?.Value ?? $"Skill Set {setIndex + 1}"
            };

            foreach (var skillElement in skillSetElement.Elements("Skill"))
            {
                string? label = skillElement.Attribute("label")?.Value;
                bool enabled = skillElement.Attribute("enabled")?.Value != "false";

                if (!enabled)
                {
                    continue; // Skip disabled skills
                }

                foreach (var gemElement in skillElement.Elements("Gem"))
                {
                    var gem = ParseGem(gemElement, label);
                    if (gem != null)
                    {
                        skillSet.Gems.Add(gem);
                    }
                }
            }

            skillSets.Add(skillSet);
            setIndex++;
        }

        // Ensure at least one skill set exists
        if (skillSets.Count == 0)
        {
            skillSets.Add(new SkillSet { Name = "Default" });
        }

        return skillSets;
    }

    /// <summary>
    /// Parses a single gem element
    /// </summary>
    private Gem? ParseGem(XElement gemElement, string? linkGroup)
    {
        string? gemName = gemElement.Attribute("nameSpec")?.Value ?? gemElement.Attribute("skillId")?.Value;
        
        if (string.IsNullOrWhiteSpace(gemName))
        {
            return null;
        }

        var gem = new Gem
        {
            Name = gemName,
            Level = int.Parse(gemElement.Attribute("level")?.Value ?? "1"),
            Quality = int.Parse(gemElement.Attribute("quality")?.Value ?? "0"),
            LinkGroup = linkGroup ?? "Unknown",
            IsEnabled = gemElement.Attribute("enabled")?.Value != "false"
        };

        // Determine gem type based on name
        gem.Type = DetermineGemType(gemName);
        
        // Determine socket color based on gem attributes
        gem.Color = DetermineSocketColor(gemElement);

        return gem;
    }

    /// <summary>
    /// Determines gem type from name
    /// </summary>
    private GemType DetermineGemType(string name)
    {
        string lowerName = name.ToLower();

        if (lowerName.Contains("support"))
            return GemType.Support;
        if (lowerName.Contains("aura") || lowerName.Contains("grace") || lowerName.Contains("determination"))
            return GemType.Aura;
        if (lowerName.Contains("curse") || lowerName.Contains("mark"))
            return GemType.Curse;
        if (lowerName.Contains("herald"))
            return GemType.Herald;
        if (lowerName.StartsWith("vaal "))
            return GemType.Vaal;

        return GemType.Active;
    }

    /// <summary>
    /// Determines socket color from gem attributes
    /// </summary>
    private SocketColor DetermineSocketColor(XElement gemElement)
    {
        // PoB doesn't always specify color directly, we'd need to look at gem requirements
        // For now, return a default - this can be enhanced with gem data
        string? gemName = gemElement.Attribute("nameSpec")?.Value ?? "";
        
        // Simple heuristic based on common gem types
        if (gemName.Contains("Melee") || gemName.Contains("Physical") || gemName.Contains("Life"))
            return SocketColor.Red;
        if (gemName.Contains("Projectile") || gemName.Contains("Attack") || gemName.Contains("Speed"))
            return SocketColor.Green;
        if (gemName.Contains("Spell") || gemName.Contains("Elemental") || gemName.Contains("Mana"))
            return SocketColor.Blue;

        return SocketColor.Red; // Default
    }

    /// <summary>
    /// Parses all item sets from the build
    /// </summary>
    private List<ItemSet> ParseItemSets(XElement root, out int activeIndex)
    {
        var itemSets = new List<ItemSet>();
        activeIndex = 0;

        var itemsElement = root.Element("Items");
        if (itemsElement == null)
        {
            return itemSets;
        }

        // Get active item set index
        var activeSetAttr = itemsElement.Attribute("activeItemSet");
        if (activeSetAttr != null && int.TryParse(activeSetAttr.Value, out int activeIdx))
        {
            activeIndex = activeIdx - 1; // PoB uses 1-based indexing
        }

        int setIndex = 0;
        foreach (var itemSetElement in itemsElement.Elements("ItemSet"))
        {
            var itemSet = new ItemSet
            {
                Name = itemSetElement.Attribute("title")?.Value ?? $"Item Set {setIndex + 1}"
            };

            foreach (var slotElement in itemSetElement.Elements("Slot"))
            {
                string? slotName = slotElement.Attribute("name")?.Value;
                string? itemText = slotElement.Attribute("itemId")?.Value ?? slotElement.Value;

                if (string.IsNullOrWhiteSpace(itemText))
                {
                    continue;
                }

                var item = ParseItem(itemText, slotName);
                if (item != null)
                {
                    itemSet.Items.Add(item);
                }
            }

            itemSets.Add(itemSet);
            setIndex++;
        }

        // Ensure at least one item set exists
        if (itemSets.Count == 0)
        {
            itemSets.Add(new ItemSet { Name = "Default" });
        }

        return itemSets;
    }

    /// <summary>
    /// Parses a single item
    /// </summary>
    private Item? ParseItem(string itemText, string? slotName)
    {
        if (string.IsNullOrWhiteSpace(itemText))
        {
            return null;
        }

        var item = new Item
        {
            Slot = ParseItemSlot(slotName ?? "Unknown")
        };

        // Parse item text (PoB item format is complex, this is simplified)
        var lines = itemText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length > 0)
        {
            item.Name = lines[0].Trim();
        }

        // Determine rarity from name formatting (simplified)
        if (item.Name.StartsWith("Unique:"))
        {
            item.Rarity = ItemRarity.Unique;
            item.Name = item.Name.Replace("Unique:", "").Trim();
        }
        else if (item.Name.StartsWith("Rare:"))
        {
            item.Rarity = ItemRarity.Rare;
            item.Name = item.Name.Replace("Rare:", "").Trim();
        }
        else
        {
            item.Rarity = ItemRarity.Rare; // Default assumption
        }

        // Parse sockets (look for "Sockets:" line)
        foreach (var line in lines)
        {
            if (line.StartsWith("Sockets:"))
            {
                ParseSockets(line, item);
            }
        }

        return item;
    }

    /// <summary>
    /// Parses socket information from item text
    /// </summary>
    private void ParseSockets(string socketLine, Item item)
    {
        // Example: "Sockets: R-R-G-G-G-G"
        string socketsText = socketLine.Replace("Sockets:", "").Trim();
        var socketGroups = socketsText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int maxLinkCount = 0;

        foreach (var group in socketGroups)
        {
            var sockets = group.Split('-', StringSplitOptions.RemoveEmptyEntries);
            maxLinkCount = Math.Max(maxLinkCount, sockets.Length);

            foreach (var socket in sockets)
            {
                var color = socket.Trim().ToUpper() switch
                {
                    "R" => SocketColor.Red,
                    "G" => SocketColor.Green,
                    "B" => SocketColor.Blue,
                    "W" => SocketColor.White,
                    _ => SocketColor.Red
                };

                item.RequiredSockets.Add(color);
            }
        }

        item.RequiredLinks = maxLinkCount;
    }

    /// <summary>
    /// Maps slot name to ItemSlot enum
    /// </summary>
    private ItemSlot ParseItemSlot(string slotName)
    {
        return slotName.ToLower() switch
        {
            "weapon 1" or "weapon1" => ItemSlot.Weapon,
            "weapon 2" or "weapon2" or "offhand" => ItemSlot.OffHand,
            "helmet" or "helm" => ItemSlot.Helmet,
            "body armour" or "bodyarmour" or "chest" => ItemSlot.BodyArmour,
            "gloves" => ItemSlot.Gloves,
            "boots" => ItemSlot.Boots,
            "amulet" => ItemSlot.Amulet,
            "ring 1" or "ring 2" or "ring" => ItemSlot.Ring,
            "belt" => ItemSlot.Belt,
            "flask 1" or "flask 2" or "flask 3" or "flask 4" or "flask 5" or "flask" => ItemSlot.Flask,
            _ => ItemSlot.Jewel
        };
    }

    /// <summary>
    /// Parses a PoB build from a file path
    /// </summary>
    public Build ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"PoB file not found: {filePath}");
        }

        string xml = File.ReadAllText(filePath);
        return Parse(xml);
    }

    /// <summary>
    /// Parses a PoB build from a paste code
    /// </summary>
    public Build ParseFromPasteCode(string pasteCode)
    {
        string xml = PobDecoder.DecodeToXml(pasteCode);
        return Parse(xml);
    }
}