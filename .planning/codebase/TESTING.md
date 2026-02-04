# Testing Patterns

**Analysis Date:** 2026-02-04

## Test Framework

**Runner:**
- xUnit 2.9.3
- Config: `PathPilot.Core.Tests.csproj`
- Microsoft.NET.Test.Sdk 17.14.1
- xunit.runner.visualstudio 3.1.4

**Assertion Library:**
- xUnit built-in assertions (no Fluent Assertions or similar library detected)

**Coverage:**
- coverlet.collector 6.0.4 installed
- No explicit coverage threshold enforced

**Run Commands:**
```bash
# Run all tests
dotnet test

# Watch mode
dotnet watch test

# With coverage
dotnet test /p:CollectCoverage=true
```

## Test File Organization

**Location:**
- Tests are co-located in separate `tests/` directory
- Structure mirrors `src/` layout: `tests/PathPilot.Core.Tests/` for core library tests

**Naming:**
- Test class: `UnitTest1.cs` (currently placeholder, needs renaming to descriptive names)
- Test methods: `[Fact]` attribute for fact tests, `[Theory]` for parameterized tests (pattern not yet used)

**Structure:**
```
tests/
└── PathPilot.Core.Tests/
    ├── PathPilot.Core.Tests.csproj
    └── UnitTest1.cs              # Placeholder - should be renamed
```

## Test Structure

**Current State:**
- Minimal test implementation exists
- Only placeholder test detected in `UnitTest1.cs`:
```csharp
namespace PathPilot.Core.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        // No test logic
    }
}
```

**Expected Patterns (to implement):**
- Arrange-Act-Assert (AAA) pattern
- One assertion per test where possible (xUnit philosophy)
- Descriptive test method names: `[MethodName]_[Scenario]_[Expected]`

**Example structure for Gem parser:**
```csharp
public class GemParsingTests
{
    [Fact]
    public void ParseGem_WithValidInput_ReturnsGemWithCorrectLevel()
    {
        // Arrange
        var parser = new PobXmlParser(gemDataService);
        var xmlContent = "<Gem nameSpec='Fireball' level='20' />";

        // Act
        var gem = parser.ParseGem(xmlContent);

        // Assert
        Assert.Equal("Fireball", gem.Name);
        Assert.Equal(20, gem.Level);
    }
}
```

## Mocking

**Framework:**
- No mocking library detected (Moq, NSubstitute not in dependencies)

**Pattern (to establish):**
- Create test doubles manually for `GemDataService`
- Dependency injection simplifies testing: `PobXmlParser(GemDataService gemDataService)`
- Pass `null` or mock implementation in tests

**Example Approach:**
```csharp
// Manual mock for GemDataService
public class MockGemDataService : GemDataService
{
    public override GemInfo GetGemInfo(string name)
    {
        return new GemInfo { Name = name, Color = "Red" };
    }
}

// In test
var mockService = new MockGemDataService();
var parser = new PobXmlParser(mockService);
```

**What to Mock:**
- External services: `GemDataService` (reads from database)
- File I/O: Use in-memory paths or temp files
- Network calls: `PobUrlImporter` (calls pobb.in API)

**What NOT to Mock:**
- Domain models: Test with real `Gem`, `Build`, `Item` objects
- Parsing logic: Test real XML parsing paths
- Data structures: Use actual `List<>`, `Dictionary<>` collections

## Fixtures and Factories

**Test Data Location:**
- Not yet established; create in: `tests/PathPilot.Core.Tests/Fixtures/`

**Recommended Pattern:**
```csharp
// File: tests/PathPilot.Core.Tests/Fixtures/GemFixtures.cs
public static class GemFixtures
{
    public static Gem CreateTestGem(string name = "Fireball", int level = 1)
    {
        return new Gem
        {
            Name = name,
            Level = level,
            Quality = 0,
            Type = GemType.Active,
            Color = SocketColor.Red
        };
    }
}

// Usage in test
var gem = GemFixtures.CreateTestGem("Fireball", 20);
```

**PoB XML Fixtures:**
- Create minimal valid PoB XML files in `tests/PathPilot.Core.Tests/Data/`
- Include complete and minimal examples for each parser test

```csharp
public static class PobXmlFixtures
{
    public const string MinimalBuild = @"
        <PathOfBuilding>
            <Build level='1' className='Witch' />
            <Skills>
                <Skill slot='MainHand'>
                    <Gem nameSpec='Fireball' level='1' quality='0' enabled='1' />
                </Skill>
            </Skills>
        </PathOfBuilding>";
}
```

## Coverage

**Requirements:**
- Not enforced; no threshold set in .csproj

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

**Current Coverage:**
- Estimated 5% (only placeholder test exists)

**Recommended Target:**
- Core parsers (PobXmlParser, PobDecoder): 80%+
- Models: 100% (simple property containers)
- Services (BuildStorage): 80%+ (file I/O tested with temp files)
- Converters: 80%+ (UI logic)

## Test Types

**Unit Tests:**
- Scope: Individual methods in isolation
- Approach: Test parsing methods with fixtures
- Location: `[Namespace]Tests.cs` files (e.g., `PobXmlParserTests.cs`)
- Example targets:
  - `PobDecoder.DecodeToXml()` - Base64/Deflate decode
  - `PobXmlParser.ParseGem()` - Gem extraction
  - `Item.FormatRawText()` - Text processing
  - `BuildStorage.SanitizeFileName()` - Path sanitization

**Integration Tests:**
- Scope: Multiple components working together
- Approach: Load complete PoB XML, verify full Build object created
- Location: `[Feature]IntegrationTests.cs` files
- Example targets:
  - Full PoB file import workflow (XML → Build)
  - Build save/load round-trip
  - Gem data enrichment from database

**E2E Tests:**
- Framework: Not yet used
- Scope: Full application flows
- Recommendation: Use UI automation testing framework if added later

## Common Patterns

**Async Testing:**
- Not applicable yet; project uses minimal async
- When added, use `public async Task TestAsync()`

**Error Testing:**
```csharp
[Fact]
public void DecodeToXml_WithInvalidBase64_ThrowsFormatException()
{
    // Arrange
    var invalidCode = "!!!invalid!!!";

    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() =>
        PobDecoder.DecodeToXml(invalidCode));
    Assert.Contains("Invalid PoB paste code format", ex.Message);
}

[Fact]
public void Parse_WithMissingBuildElement_ThrowsInvalidOperationException()
{
    // Arrange
    var invalidXml = "<PathOfBuilding></PathOfBuilding>";
    var parser = new PobXmlParser(mockGemDataService);

    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() =>
        parser.Parse(invalidXml));
}
```

**Parameterized Testing (xUnit Theory):**
```csharp
[Theory]
[InlineData("UNIQUE", "#af6025")]
[InlineData("RARE", "#ffff77")]
[InlineData("MAGIC", "#8888ff")]
[InlineData("NORMAL", "#c8c8c8")]
public void RarityColorConverter_ReturnsCorrectColor(string rarity, string expectedHex)
{
    // Arrange
    var converter = new RarityColorConverter();

    // Act
    var result = converter.Convert(rarity, typeof(SolidColorBrush), null, null);

    // Assert
    var brush = Assert.IsType<SolidColorBrush>(result);
    Assert.Equal(expectedHex, brush.Color.ToString());
}
```

## Files to Test

**High Priority (core logic):**
- `src/PathPilot.Core/Parsers/PobDecoder.cs` - Base64/Deflate decoding
- `src/PathPilot.Core/Parsers/PobXmlParser.cs` - XML to object mapping
- `src/PathPilot.Core/Services/BuildStorage.cs` - Save/load persistence
- `src/PathPilot.Core/Models/Item.cs` - RawText formatting with regex

**Medium Priority (supporting):**
- `src/PathPilot.Core/Parsers/PobUrlImporter.cs` - URL fetching
- `src/PathPilot.Core/Services/GemDataService.cs` - Database loading
- `src/PathPilot.Desktop/Converters/*.cs` - Value converter logic

**Low Priority (UI-heavy, harder to test):**
- `src/PathPilot.Desktop/Services/HotkeyService.cs` - Windows API hooks
- `src/PathPilot.Desktop/MainWindow.axaml.cs` - MVVM event handling
- `src/PathPilot.Desktop/OverlayWindow.axaml.cs` - Overlay rendering

## Testing Checklist

- [ ] Rename `UnitTest1.cs` to descriptive name (e.g., `PobDecoderTests.cs`)
- [ ] Create test fixtures: `Fixtures/` directory for test data
- [ ] Add xUnit `[Theory]` and `[InlineData]` parameterized tests
- [ ] Test all public Parse methods in parsers
- [ ] Test error paths: invalid inputs, malformed XML, network errors
- [ ] Test edge cases: empty inputs, null values, boundary conditions
- [ ] Add integration tests for complete workflows
- [ ] Set up CI/CD to run tests on commit (GitHub Actions)
- [ ] Add coverage reporting (coverage badges in README)

---

*Testing analysis: 2026-02-04*
