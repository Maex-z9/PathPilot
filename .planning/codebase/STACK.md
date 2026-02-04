# Technology Stack

**Analysis Date:** 2026-02-04

## Languages

**Primary:**
- C# 12 (.NET 10.0) - Core library and desktop application
- XAML (Avalonia) - UI markup for desktop interface
- JSON - Data serialization and configuration

**Secondary:**
- HTML/CSS/JavaScript - Embedded via WebView for skill tree display

## Runtime

**Environment:**
- .NET 10.0 (LTS)
- Windows primary target (overlay and hotkey services Windows-only)
- Cross-platform support via Avalonia (code can run on Linux/macOS with reduced features)

**Package Manager:**
- NuGet
- Lockfile: Project file format (`*.csproj`) with pinned versions

## Frameworks

**Core UI:**
- Avalonia 11.3.11 - Cross-platform XAML UI framework
- Avalonia.Desktop 11.3.11 - Desktop platform integration
- Avalonia.Themes.Fluent 11.3.11 - Windows Fluent design theme
- Avalonia.Fonts.Inter 11.3.11 - Font package
- Avalonia.Diagnostics 11.3.11 - Debug tools (Debug only)

**Browser/Web:**
- WebViewControl-Avalonia 3.120.11 - Chromium-based WebView for embedded browser (skill tree viewer)

**Testing:**
- xUnit 2.9.3 - Test runner framework
- xunit.runner.visualstudio 3.1.4 - Visual Studio test explorer integration
- Microsoft.NET.Test.Sdk 17.14.1 - Test SDK
- coverlet.collector 6.0.4 - Code coverage collection

**Build/Dev:**
- Standard MSBuild (included in .NET SDK)

## Key Dependencies

**Critical:**
- Avalonia ecosystem (11.3.11) - Core UI rendering and platform integration
- WebViewControl-Avalonia (3.120.11) - Chromium integration for embedded browser (enables skill tree display)

**Data Processing:**
- System.Text.Json (built-in) - JSON serialization for builds and settings
- System.IO.Compression (built-in) - Deflate compression for PoB data decoding
- System.Xml.Linq (built-in) - XML parsing for Path of Building imports

**GemScraper Tool Only:**
- HtmlAgilityPack 1.12.4 - HTML parsing from PoE Wiki
- Newtonsoft.Json 13.0.4 - JSON serialization in scraper

## Configuration

**Environment:**
- Builds located in: `~/.config/PathPilot/Builds/` (Windows AppData)
- Overlay settings: `~/.config/PathPilot/overlay-settings.json`
- Quest progress: `~/.config/PathPilot/quest-progress.json` (planned)
- Gem database: Embedded as `Data/gems-database.json` in application directory

**Project Configuration:**
- `pathPilot.slnx` - Solution file (modern VS project format)
- Target Framework: net10.0
- Implicit usings enabled
- Nullable reference types enabled
- Compiled bindings enabled (Avalonia optimization)

**Platform Configuration:**
- Windows manifest: `app.manifest` for application metadata
- Output type: WinExe (Windows desktop executable)

## Platform Requirements

**Development:**
- .NET 10.0 SDK
- Visual Studio 2022 (recommended) or VS Code with C# extension
- Windows for full development (overlay/hotkey features)

**Runtime - Core:**
- .NET 10.0 Runtime
- Windows 7+ for Path of Exile integration (overlay, hotkeys)
- 50MB+ disk space for application and gem database

**Runtime - Optional Features:**
- Chromium browser (bundled with WebViewControl, ~200MB)
- Internet connection for importing builds from pobb.in/pastebin and Path of Building

## Build Output

**Artifacts:**
- `src/PathPilot.Desktop/bin/Debug/net10.0/PathPilot.Desktop.exe` - Debug executable
- `src/PathPilot.Desktop/bin/Release/net10.0/PathPilot.Desktop.exe` - Release executable
- Related assemblies and dependencies auto-bundled by .NET

**Data Bundling:**
- Gem database JSON included at compile time in output directory
- Avalonia assets embedded in executable

## Dependency Summary

| Component | Type | Version | Purpose |
|-----------|------|---------|---------|
| Avalonia | UI Framework | 11.3.11 | Desktop XAML rendering |
| WebViewControl-Avalonia | Browser | 3.120.11 | Embedded Chromium for skill tree |
| xUnit | Testing | 2.9.3 | Unit test execution |
| HtmlAgilityPack | Scraping | 1.12.4 | Wiki HTML parsing (GemScraper only) |
| Newtonsoft.Json | Serialization | 13.0.4 | JSON handling (GemScraper only) |

---

*Stack analysis: 2026-02-04*
