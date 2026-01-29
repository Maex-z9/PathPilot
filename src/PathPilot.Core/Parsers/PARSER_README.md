# PoB Parser - Usage Guide

## Overview

The PathPilot PoB Parser converts Path of Building builds into C# objects that can be used in the application.

## Components

### 1. PobDecoder
Handles Base64 decoding and Deflate decompression of PoB paste codes.

```csharp
// Decode a PoB paste code to XML
string xml = PobDecoder.DecodeToXml(pasteCode);

// Encode XML back to PoB format
string pasteCode = PobDecoder.EncodeFromXml(xml);

// Check if a string is a valid PoB code
bool isValid = PobDecoder.IsValidPobCode(pasteCode);
```

### 2. PobUrlImporter
Downloads builds from pobb.in or pastebin.com URLs.

```csharp
var importer = new PobUrlImporter();

// Download from URL
string xml = await importer.ImportFromUrlAsync("https://pobb.in/abc123");

// Validate URL
bool isValid = PobUrlImporter.IsValidPobbUrl(url);
```

### 3. PobXmlParser
Parses XML into Build objects.

```csharp
var parser = new PobXmlParser();

// Parse from XML string
Build build = parser.Parse(xml);

// Parse from file
Build build = parser.ParseFile("path/to/build.xml");

// Parse from paste code
Build build = parser.ParseFromPasteCode(pasteCode);
```

## Complete Usage Examples

### Example 1: Import from pobb.in URL

```csharp
using PathPilot.Core.Parsers;
using PathPilot.Core.Models;

// Create parser and importer
var importer = new PobUrlImporter();
var parser = new PobXmlParser();

// Download and parse
string xml = await importer.ImportFromUrlAsync("https://pobb.in/abc123");
Build build = parser.Parse(xml);

// Access build data
Console.WriteLine($"Build: {build.Name}");
Console.WriteLine($"Class: {build.ClassName} ({build.Ascendancy})");
Console.WriteLine($"Level: {build.Level}");
Console.WriteLine($"Passive Points: {build.SkillTree?.PointsUsed}");
Console.WriteLine($"Gems: {build.Gems.Count}");
Console.WriteLine($"Items: {build.Items.Count}");
```

### Example 2: Parse from local file

```csharp
var parser = new PobXmlParser();

try
{
    Build build = parser.ParseFile("C:/Users/YourName/Documents/PoB/MyBuild.xml");
    
    // Process the build
    foreach (var gem in build.Gems)
    {
        Console.WriteLine($"{gem.Name} (Level {gem.Level})");
    }
}
catch (FileNotFoundException)
{
    Console.WriteLine("Build file not found!");
}
```

### Example 3: Parse from paste code

```csharp
var parser = new PobXmlParser();

// Get paste code from user
string pasteCode = GetUserInput();

try
{
    Build build = parser.ParseFromPasteCode(pasteCode);
    
    // Show skill tree info
    var tree = build.SkillTree;
    if (tree != null)
    {
        Console.WriteLine($"Allocated {tree.PointsUsed} passive points");
        Console.WriteLine($"Ascendancy: {tree.Ascendancy}");
    }
}
catch (PobParserException ex)
{
    Console.WriteLine($"Failed to parse: {ex.Message}");
}
```

### Example 4: Get socket requirements

```csharp
var parser = new PobXmlParser();
Build build = parser.ParseFromPasteCode(pasteCode);

// Find 6-link requirements
var sixLinks = build.Items
    .Where(item => item.RequiredLinks >= 6)
    .ToList();

foreach (var item in sixLinks)
{
    Console.WriteLine($"{item.Slot}: {item.RequiredLinks}-Link");
    Console.WriteLine($"Colors: {string.Join("-", item.RequiredSockets)}");
}
```

### Example 5: List all gems by link group

```csharp
var parser = new PobXmlParser();
Build build = parser.ParseFromPasteCode(pasteCode);

// Group gems by their link group
var gemsByGroup = build.Gems
    .GroupBy(g => g.LinkGroup)
    .ToList();

foreach (var group in gemsByGroup)
{
    Console.WriteLine($"\n{group.Key}:");
    foreach (var gem in group)
    {
        Console.WriteLine($"  - {gem.Name} (Lvl {gem.Level}, Q{gem.Quality}%)");
    }
}
```

## Error Handling

Always wrap parser calls in try-catch blocks:

```csharp
try
{
    Build build = parser.ParseFromPasteCode(pasteCode);
}
catch (ArgumentException ex)
{
    // Invalid input (empty paste code, etc.)
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Parsing failed (corrupted data, wrong format)
    Console.WriteLine($"Parse error: {ex.Message}");
}
catch (PobParserException ex)
{
    // Custom parser error with error type
    Console.WriteLine($"Parser error: {ex.Message} (Type: {ex.ErrorType})");
}
```

## What Gets Parsed

### ✅ Currently Supported
- Build metadata (name, class, ascendancy, level)
- Passive skill tree (allocated nodes)
- Skill gems (name, level, quality, link groups)
- Items (name, rarity, slot, socket colors/links)
- Build notes

### ⚠️ Partial Support
- Gem types (basic heuristics)
- Socket colors (inferred from gem names)
- Item mods (parsed but not categorized)

### ❌ Not Yet Supported
- Detailed passive node stats
- Gem quality types (Anomalous, Divergent, etc.)
- Jewel effects
- Cluster jewels
- Configuration settings
- DPS calculations

## Next Steps

After parsing a build, you typically want to:

1. Calculate progression steps (which passives to allocate next)
2. Determine which gems to acquire and when
3. Show gear requirements with socket/link info
4. Compare current character state with target build

See the Services documentation for progression calculation.
