using HtmlAgilityPack;
using Newtonsoft.Json;
using PathPilot.Core.Models;
using System.Net;

Console.WriteLine("PoE Wiki Gem Scraper");
Console.WriteLine("====================\n");

var scraper = new GemScraper();

// Get all gem names first
var gemNames = await scraper.GetAllGemNames();
Console.WriteLine($"\nFound {gemNames.Count} gems to process\n");

// Now scrape each individual gem page
var allGems = await scraper.ScrapeAllGemPages(gemNames);

Console.WriteLine($"\nTotal gems with sources: {allGems.Count}");

// Save to JSON
string outputPath = Path.Combine(
    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
    "..", "..", "..", "..", "..", "..", "src", "PathPilot.Core", "Data", "gems-database.json");
outputPath = Path.GetFullPath(outputPath);
string json = JsonConvert.SerializeObject(allGems, Formatting.Indented);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, json);

Console.WriteLine($"\n✓ Saved to: {outputPath}");
Console.WriteLine("Done!");

public class GemData
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "White";
    public string? IconUrl { get; set; }
    public List<GemSource> Sources { get; set; } = new();
}

public class GemScraper
{
    private readonly HtmlWeb _web = new();
    
    public async Task<HashSet<string>> GetAllGemNames()
    {
        var gemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            Console.WriteLine("Loading active skill gems list...");
            await LoadGemsFromPage("https://www.poewiki.net/wiki/Skill_gem", gemNames);
            
            Console.WriteLine("Loading support gems list...");
            await LoadGemsFromPage("https://www.poewiki.net/wiki/Support_gem", gemNames);
            
            Console.WriteLine("Loading transfigured gems list...");
            await LoadGemsFromPage("https://www.poewiki.net/wiki/Transfigured_gem", gemNames);
            
            Console.WriteLine($"Found {gemNames.Count} valid gem names total");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading gem lists: {ex.Message}");
        }
        
        return gemNames;
    }
    
    private async Task LoadGemsFromPage(string url, HashSet<string> gemNames)
{
    var htmlDoc = await Task.Run(() => _web.Load(url));
    var doc = htmlDoc.DocumentNode;  // FIX: Get DocumentNode
    
    var gemLinks = doc.SelectNodes("//table//a[contains(@href, '/wiki/') and @title]");
    
    if (gemLinks != null)
    {
        foreach (var link in gemLinks)
        {
            var href = link.GetAttributeValue("href", "");
            var title = WebUtility.HtmlDecode(link.GetAttributeValue("title", ""));
            
            if (href.Contains("/wiki/") && 
                !href.Contains("File:") && 
                !href.Contains("Category:") &&
                !title.Contains("(gem tag)") &&
                !title.Contains("(page does not exist)") &&
                !string.IsNullOrWhiteSpace(title))
            {
                string gemName = CleanGemName(title);
                if (!string.IsNullOrWhiteSpace(gemName) && gemName.Length > 3)
                {
                    gemNames.Add(gemName);
                }
            }
        }
    }
}
    
    public async Task<Dictionary<string, GemData>> ScrapeAllGemPages(HashSet<string> gemNames)
    {
        var allGems = new Dictionary<string, GemData>();
        int processed = 0;
        
        foreach (var gemName in gemNames)
        {
            processed++;
            
            try
            {
                if (processed % 20 == 0)
                {
                    Console.WriteLine($"Progress: {processed}/{gemNames.Count}...");
                }
                
                var gemData = await ScrapeGemPage(gemName);
                
                if (gemData != null)
                {
                    allGems[gemName] = gemData;
                }
                
                // Small delay to not hammer the server
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {gemName}: {ex.Message}");
            }
        }
        
        return allGems;
    }
    
private async Task<GemData?> ScrapeGemPage(string gemName)
{
    try
    {
        // Create URL-friendly name (encode special characters like apostrophes)
        string urlName = Uri.EscapeDataString(gemName.Replace(" ", "_"));
        string url = $"https://www.poewiki.net/wiki/{urlName}";
        
        var htmlDoc = await Task.Run(() => _web.Load(url));
        var doc = htmlDoc.DocumentNode;  // FIX: Get DocumentNode from HtmlDocument
        
        var gemData = new GemData
        {
            Name = gemName,
            Color = ParseGemColor(doc),
            IconUrl = ParseIconUrl(doc, gemName)
        };

        // Find "Quest reward" section
        var questHeader = doc.SelectSingleNode("//h3[.//span[@id='Quest_reward']]");
        if (questHeader != null)
        {
            var questTable = questHeader.SelectSingleNode("following-sibling::table[1]");
            if (questTable != null)
            {
                ParseQuestRewardTable(questTable, gemData);
            }
        }
        
        // Find "Vendor reward" section
        var vendorHeader = doc.SelectSingleNode("//h3[.//span[@id='Vendor_reward']]");
        if (vendorHeader != null)
        {
            var vendorTable = vendorHeader.SelectSingleNode("following-sibling::table[1]");
            if (vendorTable != null)
            {
                ParseVendorRewardTable(vendorTable, gemData);
            }
        }
        
        return gemData;
    }
    catch
    {
        return null;
    }
}
    
private string? ParseIconUrl(HtmlNode doc, string gemName)
{
    try
    {
        // Look for inventory icon in the infobox
        var iconImg = doc.SelectSingleNode("//td[contains(@class, 'inventory-image')]//img")
                     ?? doc.SelectSingleNode("//span[contains(@class, 'inventory-image')]//img");

        if (iconImg != null)
        {
            var src = iconImg.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src) && src.Contains("inventory_icon", StringComparison.OrdinalIgnoreCase))
            {
                if (src.StartsWith("//")) src = "https:" + src;
                return src;
            }
        }

        // Fallback: construct URL via Special:FilePath
        string encodedName = Uri.EscapeDataString(gemName.Replace(" ", "_"));
        return $"https://www.poewiki.net/wiki/Special:FilePath/{encodedName}_inventory_icon.png";
    }
    catch
    {
        return null;
    }
}

private string ParseGemColor(HtmlNode doc)
{
    try
    {
        // Method 1: Look for categories at the bottom of the page
        var catLinks = doc.SelectNodes("//div[@id='mw-normal-catlinks']//a");
        if (catLinks != null)
        {
            foreach (var link in catLinks)
            {
                string catText = link.InnerText;
                if (catText.Contains("Strength skill gems", StringComparison.OrdinalIgnoreCase))
                    return "Red";
                if (catText.Contains("Dexterity skill gems", StringComparison.OrdinalIgnoreCase))
                    return "Green";
                if (catText.Contains("Intelligence skill gems", StringComparison.OrdinalIgnoreCase))
                    return "Blue";
            }
        }
        
        // Method 2: Look in the progression table header for "Required Strength/Dexterity/Intelligence"
        var tableHeaders = doc.SelectNodes("//table[contains(@class, 'skill-progression-table')]//th//abbr");
        if (tableHeaders != null)
        {
            foreach (var header in tableHeaders)
            {
                string titleText = header.GetAttributeValue("title", "");  // FIX: Use GetAttributeValue
                if (titleText.Contains("Required Strength", StringComparison.OrdinalIgnoreCase))
                    return "Red";
                if (titleText.Contains("Required Dexterity", StringComparison.OrdinalIgnoreCase))
                    return "Green";
                if (titleText.Contains("Required Intelligence", StringComparison.OrdinalIgnoreCase))
                    return "Blue";
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing color: {ex.Message}");
    }
    
    return "White";
}
    
    private void ParseQuestRewardTable(HtmlNode table, GemData gemData)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;
        
        // Skip first 2 rows (headers)
        foreach (var row in rows.Skip(2))
        {
            var cells = row.SelectNodes(".//th | .//td");
            if (cells == null || cells.Count < 2) continue;
            
            // First cell has quest name and act
            string questText = CleanText(cells[0].InnerText);
            string questName = ExtractQuestName(questText);
            int act = ExtractActNumber(questText);
            
            // Check which classes (columns 2-8) have checkmarks
            var classes = new List<string>();
            for (int i = 1; i < cells.Count && i < 8; i++)
            {
                if (cells[i].InnerText.Contains("Yes") || cells[i].SelectSingleNode(".//img[@alt='Yes']") != null)
                {
                    string className = GetClassNameByIndex(i - 1);
                    classes.Add(className);
                }
            }
            
            if (classes.Count > 0)
            {
                gemData.Sources.Add(new GemSource
                {
                    Type = SourceType.QuestReward,
                    Act = act,
                    QuestName = questName,
                    AvailableForClasses = classes
                });
            }
        }
    }
    
    private void ParseVendorRewardTable(HtmlNode table, GemData gemData)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;
        
        // Skip first 2 rows (headers)
        foreach (var row in rows.Skip(2))
        {
            var cells = row.SelectNodes(".//th | .//td");
            if (cells == null || cells.Count < 2) continue;
            
            // First cell has quest name, act, and vendor
            string questText = CleanText(cells[0].InnerText);
            string questName = ExtractQuestName(questText);
            int act = ExtractActNumber(questText);
            string vendor = ExtractVendorName(questText);
            
            // Check if available for all classes (colspan=7)
            if (cells.Count == 2 && cells[1].GetAttributeValue("colspan", "") == "7")
            {
                // Available for all classes
                gemData.Sources.Add(new GemSource
                {
                    Type = vendor.Contains("Lilly") ? SourceType.Lilly :
                           vendor.Contains("Siosa") ? SourceType.Siosa : SourceType.Vendor,
                    Act = act,
                    VendorName = vendor,
                    QuestName = questName,
                    AvailableForClasses = new List<string> { "All" }
                });
            }
            else
            {
                // Check which classes have checkmarks
                var classes = new List<string>();
                for (int i = 1; i < cells.Count && i < 8; i++)
                {
                    if (cells[i].InnerText.Contains("Yes") || cells[i].SelectSingleNode(".//img[@alt='Yes']") != null)
                    {
                        string className = GetClassNameByIndex(i - 1);
                        classes.Add(className);
                    }
                }
                
                if (classes.Count > 0)
                {
                    gemData.Sources.Add(new GemSource
                    {
                        Type = vendor.Contains("Lilly") ? SourceType.Lilly :
                               vendor.Contains("Siosa") ? SourceType.Siosa : SourceType.Vendor,
                        Act = act,
                        VendorName = vendor,
                        QuestName = questName,
                        AvailableForClasses = classes
                    });
                }
            }
        }
    }
    
    private static string GetClassNameByIndex(int index)
    {
        var classNames = new[] { "Witch", "Shadow", "Ranger", "Duelist", "Marauder", "Templar", "Scion" };
        return index >= 0 && index < classNames.Length ? classNames[index] : "Unknown";
    }
    
    private static string ExtractQuestName(string text)
    {
        // Split by newlines and clean
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
        
        if (lines.Count == 0) return "Unknown";
        
        // First line is quest name, but remove "Act X" if present at the end
        string questName = lines[0];
        
        // Remove "Act X" pattern at the end
        questName = System.Text.RegularExpressions.Regex.Replace(
            questName, 
            @"Act\s+\d+$", 
            "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        ).Trim();
        
        // Also remove vendor name if present
        var vendors = new[] { "Nessa", "Siosa", "Lilly Roth", "Petarus", "Vanja", "Yeena", "Clarissa" };
        foreach (var vendor in vendors)
        {
            if (questName.EndsWith(vendor, StringComparison.OrdinalIgnoreCase))
            {
                questName = questName.Substring(0, questName.Length - vendor.Length).Trim();
            }
        }
        
        return questName;
    }
    
    private static string ExtractVendorName(string text)
    {
        if (text.Contains("Lilly Roth")) return "Lilly Roth";
        if (text.Contains("Siosa")) return "Siosa";
        if (text.Contains("Nessa")) return "Nessa";
        if (text.Contains("Petarus") || text.Contains("Vanja")) return "Petarus and Vanja";
        if (text.Contains("Yeena")) return "Yeena";
        if (text.Contains("Clarissa")) return "Clarissa";
        return "Unknown Vendor";
    }
    
    private static string CleanGemName(string name)
    {
        return name.Trim()
            .Replace(" skill gem", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (gem)", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" (skill gem)", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
    
    private static string CleanText(string text)
    {
        return text.Trim()
            .Replace("\n", " ")
            .Replace("  ", " ");
    }
    
    private static int ExtractActNumber(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text, @"Act\s+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}