# Coding Conventions

**Analysis Date:** 2026-02-04

## Naming Patterns

**Files:**
- PascalCase for all C# files: `Gem.cs`, `PobXmlParser.cs`, `MainWindow.axaml.cs`
- Converters use descriptive names ending in "Converter": `RarityColorConverter.cs`, `GemColorConverter.cs`
- Exception classes end in "Exception": `PobParserException.cs`
- Services end in "Service": `BuildStorage.cs`, `GemDataService.cs`, `QuestDataService.cs`
- Models in `Models/` directory with singular names: `Gem.cs`, `Build.cs`, `Item.cs`, `Quest.cs`
- Parsers in `Parsers/` directory with descriptive names: `PobXmlParser.cs`, `PobDecoder.cs`, `PobUrlImporter.cs`

**Classes and Types:**
- PascalCase for all class names: `Build`, `Gem`, `Item`, `SkillSet`, `ItemSet`
- Enums use PascalCase: `GemType`, `SocketColor`, `QuestReward`, `ItemSlot`, `ItemRarity`
- Enum values use PascalCase: `SkillPoint`, `AscendancyTrial`, `Labyrinth`

**Properties:**
- PascalCase for public properties: `Name`, `Level`, `Quality`, `LinkGroups`, `AcquisitionInfo`
- Auto-properties with initializers: `public string Name { get; set; } = string.Empty;`
- Computed properties (get-only) use readable names: `DisplayName`, `ColorName`, `CharacterDescription`, `ActiveSkillSet`

**Methods:**
- PascalCase for public methods: `Parse()`, `LoadBuild()`, `SaveBuild()`, `GetAllQuests()`
- Descriptive verb-first pattern: `ParseBuildMetadata()`, `EnrichGemFromDatabase()`, `NormalizeSlotName()`
- Private helper methods start with descriptive verbs: `CleanGemName()`, `ParseBoolAttribute()`, `ProcessLine()`
- Boolean methods use "Is" prefix: `IsValidPobCode()`, `ParseBoolAttribute()` pattern

**Variables:**
- camelCase for local variables and parameters: `pasteCode`, `compressedBytes`, `gemName`, `itemsById`
- Descriptive names avoiding single letters except in loops: `result`, `processed`, `item`, `gem`
- Collections use plural form: `SkillSets`, `ItemSets`, `TreeSets`, `gems`, `items`, `mods`

**Constants:**
- UPPER_SNAKE_CASE for Windows API constants: `WH_KEYBOARD_LL`, `WM_KEYDOWN`, `WM_SYSKEYDOWN`
- Hex values for virtual keys: `0x70` for F1, `0x41` for A

## Code Style

**Formatting:**
- Target: .NET 10.0, C# latest
- Implicit usings enabled: `using` statements consolidated at top
- Nullable reference types enabled: `#nullable enable` in projects
- File-scoped namespaces: `namespace PathPilot.Core.Models;` (no braces)
- Indentation: 4 spaces (inferred from codebase)

**Bracing:**
- Allman style braces for classes and methods: opening brace on new line
- Compact braces for properties: `public string Name { get; set; }`
- Single-statement if/else: no braces required if one line: `if (value != null) return;`

**Comments:**
- Triple-slash XML documentation for public classes and properties: `/// <summary>`
- Summary, parameter, return, and remarks tags
- No inline comments for self-documenting code
- Comments explain "why" not "what": business logic, parsing rules, edge cases

**Linting:**
- No formal linter configuration detected in .editorconfig or stylecop.json
- StyleCop conventions inferred from code patterns
- Nullable reference types enforced by compiler (`#nullable enable`)

## Import Organization

**Order (observed pattern):**
1. System namespaces: `using System;`, `using System.Collections.Generic;`
2. System.Xml/IO namespaces: `using System.Xml.Linq;`, `using System.IO.Compression;`
3. Third-party Avalonia: `using Avalonia.Controls;`, `using Avalonia.Media;`
4. Project namespaces: `using PathPilot.Core.Models;`, `using PathPilot.Core.Services;`

**Example from `PobXmlParser.cs`:**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PathPilot.Core.Models;
using PathPilot.Core.Services;
```

**Path Aliases:**
- No custom import aliases detected
- Fully qualified namespaces used throughout

## Error Handling

**Custom Exceptions:**
- Inherit from `Exception` with custom type tracking: `PobParserException` with `ErrorType` enum
- Enum types for error categorization: `PobParserErrorType` (InvalidFormat, DecompressionFailed, etc.)
- Contextual inner exceptions: `throw new InvalidOperationException("message", ex);`

**Pattern for parsing:**
```csharp
try
{
    // Parsing logic
    var doc = XDocument.Parse(xmlContent);
}
catch (Exception ex)
{
    throw new InvalidOperationException($"Failed to parse PoB XML: {ex.Message}", ex);
}
```

**Null Coalescing:**
- Consistent use of `??` for defaults: `buildElement.Attribute("level")?.Value ?? "Unknown"`
- Safe navigation `?.` for nullable properties
- `string.IsNullOrWhiteSpace()` for string validation
- `string.IsNullOrEmpty()` for empty checks

**Silent Failures:**
- Catch-all silently skip invalid data: `catch { /* Skip invalid files */ }`
- Used in listing scenarios (e.g., `BuildStorage.GetSavedBuilds()` skips corrupted JSON)
- Console.WriteLine for debug output: used throughout parsers for diagnostics

## Logging

**Framework:** `Console.WriteLine()` only

**Patterns:**
- Informational startup logs: `Console.WriteLine("Global keyboard hook installed");`
- Debug output during parsing: `Console.WriteLine($"Parsed {itemsById.Count} items from Items element");`
- Error logs with context: `Console.WriteLine($"Failed to set keyboard hook: {Marshal.GetLastWin32Error()}");`
- Hotkey display: `Console.WriteLine($"  Toggle Overlay: {FormatHotkey(...)}`);`

**Note:** No structured logging framework (Serilog, NLog). All logging is printf-style to console.

## Comments & Documentation

**When to Comment:**
- Public API: Always include XML summary
- Complex parsing logic: Explain format expectations (e.g., "PoB format: Line 0 = Rarity")
- Non-obvious algorithms: Regex patterns, range calculations, encoding/decoding
- Edge cases and workarounds: XML parsing fallbacks, enum conversions

**Examples:**
```csharp
/// <summary>
/// Parses the decompressed PoB XML into a Build object with proper link groups
/// </summary>
public Build Parse(string xmlContent)

// PoB format:
// Line 0: Rarity: RARE/UNIQUE/MAGIC/NORMAL
// Line 1: Item Name (for rare/unique) or Base Type (for normal/magic)
```

**JSDoc/TSDoc:**
- Not applicable (C# project, no TypeScript)
- Use XML documentation tags: `<summary>`, `<param>`, `<returns>`, `<remarks>`

## Function Design

**Size:**
- Methods range 10-50 lines typically
- Largest: `PobXmlParser.ParseXml()` = 50 lines (but has clear step structure)
- Private helpers break out complex logic: `ParseGem()`, `EnrichGemFromDatabase()`, `ProcessLine()`

**Parameters:**
- Limited to 3-4 parameters typically
- Use context objects for related data: `PobXmlParser(GemDataService gemDataService)`
- Fluent parameter names in tuples/records not observed (uses explicit objects)

**Return Values:**
- Return early on validation failures: `if (string.IsNullOrWhiteSpace(pasteCode)) throw;`
- Nullable returns when data may not exist: `Quest?`, `Build?`, `SkillSet?`
- Collections never null, always initialized: `public List<Quest> GetAllQuests() => new List<Quest> { ... }`

## Module Design

**Exports (Public API):**
- Services expose static factory methods: `BuildStorage.SaveBuild()`, `BuildStorage.LoadBuild()`
- Parsers expose Parse methods: `PobXmlParser.Parse(string xmlContent)`
- Models expose as data containers with computed properties

**Barrel Files:**
- Not used; imports are fully qualified from file paths

**Namespace Patterns:**
- `PathPilot.Core.Models` - All domain models
- `PathPilot.Core.Parsers` - PoB import logic
- `PathPilot.Core.Services` - Business logic (BuildStorage, GemDataService, QuestDataService)
- `PathPilot.Desktop.Converters` - Avalonia value converters (one per file)
- `PathPilot.Desktop.Services` - UI services (HotkeyService, OverlayService)
- `PathPilot.Desktop.Platform` - Platform-specific code (WindowsOverlayPlatform, IOverlayPlatform interface)
- `PathPilot.Desktop.Settings` - Configuration (OverlaySettings)

**Visibility:**
- Public for public APIs, internal/private for implementation details
- Converters are public (used in XAML bindings)
- Services are public (injected/instantiated by UI)
- Models are public (serialized/deserialized)

## Async/Await

**Pattern:**
- Very limited async usage observed
- `ImportButton_Click()` marked `async void` but contains no `await` (potential bug)
- No Task-based APIs in core logic; file I/O is synchronous

**Recommendation:** Not yet a convention since async is minimal in codebase.

---

*Convention analysis: 2026-02-04*
