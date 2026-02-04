# Phase 4: Interaction - Research

**Researched:** 2026-02-04
**Domain:** Avalonia UI hover interaction, tooltip display, spatial hit testing
**Confidence:** HIGH

## Summary

This phase implements hover-based interaction for the skill tree viewer, allowing users to hover over nodes to see tooltips with node information. The existing SkillTreeCanvas already handles pointer events for pan/zoom navigation, so the challenge is adding hover detection without interfering with existing pointer capture behavior.

The standard approach uses Avalonia's built-in ToolTip system with programmatic control via `ToolTip.SetTip()` and `ToolTip.SetIsOpen()`. For hit testing against ~1,384 circular nodes in the PoE 1 skill tree, simple point-to-circle distance testing is sufficient without requiring spatial indexing. The key technical challenge is coordinate transformation - converting screen coordinates to world coordinates using the existing zoom/pan transformation that's already implemented in Phase 3.

The PathPilot project already has all necessary infrastructure: pointer event handling (OnPointerMoved), coordinate transformation (ScreenToWorld), and node data including names, stats, and connections. This phase primarily adds hover detection logic and tooltip display.

**Primary recommendation:** Use OnPointerMoved to detect hovered nodes via point-to-circle distance testing, programmatically show tooltips with ToolTip.SetTip/SetIsOpen, and track hover state to avoid redundant tooltip updates.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Avalonia.Controls | 11.1+ | Built-in ToolTip control | Native tooltip system with programmatic control, follows OS conventions |
| SkiaSharp | 2.88+ | Already used for rendering | Reuse existing coordinate space for hit testing |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | No additional libraries needed | Existing infrastructure is sufficient |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Avalonia ToolTip | Custom popup control | ToolTip handles timing, positioning, and OS integration automatically |
| Linear search hit testing | Quadtree spatial index | ~1,400 nodes is small enough for linear search; quadtree adds complexity for minimal gain |
| Separate hover state tracking | Tooltip visibility check | State tracking prevents redundant SetTip calls and flicker |

**Installation:**
No additional packages required. All functionality available in existing dependencies.

## Architecture Patterns

### Recommended Project Structure
```
src/PathPilot.Desktop/
├── Controls/
│   └── SkillTreeCanvas.cs      # Add hover detection and tooltip logic here
└── Models/ (PathPilot.Core)
    └── SkillTree.cs             # PassiveNode already has Name, Stats, ConnectedNodes
```

### Pattern 1: Hover State Tracking with Tooltip Display
**What:** Track currently hovered node ID to avoid redundant tooltip updates
**When to use:** OnPointerMoved fires frequently; only update tooltip when hovered node changes
**Example:**
```csharp
// Source: Avalonia best practices + PathPilot existing patterns
public class SkillTreeCanvas : Control
{
    private int? _hoveredNodeId = null;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Only process hover when NOT panning
        if (!_isPanning && TreeData != null)
        {
            var screenPos = e.GetCurrentPoint(this).Position;
            var worldPos = ScreenToWorld(screenPos);

            var newHoveredNode = FindNodeAtPosition(worldPos);

            if (newHoveredNode != _hoveredNodeId)
            {
                _hoveredNodeId = newHoveredNode;
                UpdateTooltip();
            }
        }
        else if (_hoveredNodeId.HasValue)
        {
            // Clear tooltip when panning starts
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }
    }
}
```

### Pattern 2: Point-to-Circle Distance Testing
**What:** Calculate Euclidean distance from point to node center, compare to radius
**When to use:** Hit testing for circular nodes in 2D space
**Example:**
```csharp
// Source: https://www.jeffreythompson.org/collision-detection/point-circle.php
private int? FindNodeAtPosition(SKPoint worldPos)
{
    if (TreeData == null) return null;

    foreach (var node in TreeData.Nodes.Values)
    {
        if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
            continue;

        // Calculate distance from point to node center
        float distX = worldPos.X - node.CalculatedX.Value;
        float distY = worldPos.Y - node.CalculatedY.Value;
        float distance = (float)Math.Sqrt(distX * distX + distY * distY);

        // Determine radius based on node type (from Phase 2)
        float radius = node.IsKeystone ? 18f :
                      node.IsNotable ? 12f :
                      node.IsJewelSocket ? 10f : 6f;

        // Hit test: distance <= radius means point is inside circle
        if (distance <= radius)
            return node.Id;
    }

    return null;
}
```

### Pattern 3: Programmatic Tooltip Control
**What:** Use ToolTip.SetTip() and SetIsOpen() to show/hide tooltips from code
**When to use:** When tooltip content/visibility needs to be controlled programmatically based on hover state
**Example:**
```csharp
// Source: Avalonia ToolTip API documentation
private void UpdateTooltip()
{
    if (_hoveredNodeId.HasValue && TreeData != null)
    {
        if (TreeData.Nodes.TryGetValue(_hoveredNodeId.Value, out var node))
        {
            // Build tooltip content
            var tooltipContent = BuildTooltipContent(node);

            // Set tooltip content and show it
            ToolTip.SetTip(this, tooltipContent);
            ToolTip.SetIsOpen(this, true);
            return;
        }
    }

    // No node hovered - hide tooltip
    ToolTip.SetIsOpen(this, false);
}

private object BuildTooltipContent(PassiveNode node)
{
    // For INT-01: Show node name
    // For INT-02: Show connected node names

    var panel = new StackPanel();

    // Node name (bold/larger)
    panel.Children.Add(new TextBlock
    {
        Text = node.Name,
        FontWeight = FontWeight.Bold,
        FontSize = 14
    });

    // Node stats (if any)
    if (node.Stats.Count > 0)
    {
        foreach (var stat in node.Stats)
        {
            panel.Children.Add(new TextBlock { Text = stat });
        }
    }

    // Connected nodes (INT-02)
    if (node.ConnectedNodes.Count > 0)
    {
        panel.Children.Add(new TextBlock
        {
            Text = "\nConnected to:",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var connectedId in node.ConnectedNodes)
        {
            if (TreeData.Nodes.TryGetValue(connectedId, out var connectedNode))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"  • {connectedNode.Name}",
                    FontSize = 12
                });
            }
        }
    }

    return panel;
}
```

### Pattern 4: Coordinate Space Reuse
**What:** Reuse existing ScreenToWorld transformation for hit testing
**When to use:** Hit testing must use same coordinate space as rendering
**Example:**
```csharp
// Source: PathPilot Phase 3 implementation (existing code)
private SKPoint ScreenToWorld(Point screenPos)
{
    // Already implemented in SkillTreeCanvas
    return new SKPoint(
        (float)(screenPos.X / ZoomLevel + _offsetX),
        (float)(screenPos.Y / ZoomLevel + _offsetY));
}
```

### Anti-Patterns to Avoid
- **Processing hover during pan:** Interferes with pan gesture, creates flickering tooltips. Check `_isPanning` flag before hit testing.
- **Updating tooltip every PointerMoved:** PointerMoved fires 60+ times/second. Track hover state and only update on changes.
- **Creating new tooltip content on every frame:** Allocates objects rapidly. Only rebuild tooltip when hovered node changes.
- **Using RenderTransform for coordinate conversion:** Phase 3 uses canvas.Translate/Scale in SkiaSharp. Must match this transformation.
- **Hit testing during drag:** Tooltip should hide when user starts panning/interacting with tree.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tooltip popup system | Custom Popup/Window with positioning logic | Avalonia ToolTip.SetTip/SetIsOpen | Handles show/hide timing, OS-native positioning, delay, pointer following, cleanup |
| Tooltip styling | Hardcoded styles in code | Avalonia theme system with ToolTip styles | Respects user's theme, supports light/dark mode, consistent with OS |
| Spatial indexing | Custom quadtree for 1,384 nodes | Linear search through node dictionary | Quadtree overhead exceeds benefits at this scale; premature optimization |
| Distance calculation optimization | Custom fast-sqrt or lookup tables | Math.Sqrt() | Modern CPUs are fast enough for ~1,400 checks per hover; clarity over micro-optimization |

**Key insight:** Avalonia's ToolTip system handles all the complexity of popup timing, positioning, and lifecycle. Using it programmatically (SetTip/SetIsOpen) gives full control while retaining built-in behavior. The scale of nodes (~1,400) is small enough that simple linear search with distance checks is faster than complex spatial indexing.

## Common Pitfalls

### Pitfall 1: Tooltip Flicker During Pan
**What goes wrong:** Tooltip appears/disappears rapidly while user drags tree
**Why it happens:** OnPointerMoved fires during pan gesture, triggering hover detection on each frame
**How to avoid:** Check `_isPanning` flag before processing hover detection; clear tooltip when panning starts
**Warning signs:** Tooltip content flashes visible/invisible rapidly, tooltip jumps between nodes during drag

### Pitfall 2: Performance Degradation from Redundant Updates
**What goes wrong:** Frame rate drops, UI feels sluggish during hover
**Why it happens:** Creating new tooltip content (StackPanel, TextBlocks) on every PointerMoved event allocates many objects
**How to avoid:** Track `_hoveredNodeId` and only call `UpdateTooltip()` when ID changes
**Warning signs:** High GC pressure in profiler, dropped frames (< 60 FPS) when moving mouse over tree

### Pitfall 3: Incorrect Hit Testing Due to Coordinate Mismatch
**What goes wrong:** Hover detection is offset from visible nodes, tooltip shows wrong node
**Why it happens:** Using different coordinate transformation for hit testing vs rendering
**How to avoid:** Reuse exact same ScreenToWorld method that matches SkiaSharp canvas.Translate/Scale order
**Warning signs:** Tooltip appears for wrong node, offset increases with zoom level, offset direction changes when panning

### Pitfall 4: Tooltip Shows During Zoom
**What goes wrong:** Tooltip visible during mouse wheel zoom, covers UI, feels unresponsive
**Why it happens:** OnPointerWheelChanged doesn't clear hover state, tooltip persists through zoom operation
**How to avoid:** Clear `_hoveredNodeId` and hide tooltip in OnPointerWheelChanged handler
**Warning signs:** Tooltip visible during zoom gesture, tooltip position looks wrong after zoom

### Pitfall 5: Many Connected Nodes Overflow Tooltip
**What goes wrong:** Tooltip becomes huge (100+ connected nodes for cluster jewel sockets), unusable
**Why it happens:** PassiveNode.ConnectedNodes can have many connections, especially for cluster jewels or highway paths
**How to avoid:** Limit displayed connections (e.g., first 10 + "... and N more"), or use ScrollViewer for overflow
**Warning signs:** Tooltip extends off screen, takes seconds to render, extremely tall tooltip window

### Pitfall 6: Tooltip Doesn't Appear at All
**What goes wrong:** Hover detection works (via debug logging) but tooltip never shows
**Why it happens:** Control's `Background` property is null, making control not hit-testable for tooltips
**How to avoid:** Ensure SkillTreeCanvas.Background is set (even transparent: `Brushes.Transparent`)
**Warning signs:** OnPointerMoved fires, FindNodeAtPosition returns valid node, but ToolTip.SetIsOpen has no effect

## Code Examples

Verified patterns from official sources:

### Basic Tooltip API Usage
```csharp
// Source: https://github.com/AvaloniaUI/Avalonia/discussions/12018
// Avalonia ToolTip API for programmatic control

// Set tooltip content (string or control)
ToolTip.SetTip(myControl, "Simple text");
ToolTip.SetTip(myControl, new StackPanel { /* ... */ });

// Show/hide tooltip programmatically
ToolTip.SetIsOpen(myControl, true);   // Show immediately
ToolTip.SetIsOpen(myControl, false);  // Hide immediately
```

### Get Pointer Position from Event
```csharp
// Source: https://docs.avaloniaui.net/docs/concepts/input/pointer
// PointerEventArgs provides GetCurrentPoint relative to a control

protected override void OnPointerMoved(PointerEventArgs e)
{
    var point = e.GetCurrentPoint(this);  // Relative to 'this' control
    var x = point.Position.X;
    var y = point.Position.Y;

    // Check if button is pressed during move (for distinguishing pan)
    var isLeftPressed = point.Properties.IsLeftButtonPressed;
}
```

### Circle-Point Collision Detection
```csharp
// Source: https://www.jeffreythompson.org/collision-detection/point-circle.php
// Pythagorean theorem to test if point is inside circle

bool IsPointInCircle(float px, float py, float cx, float cy, float radius)
{
    float distX = px - cx;
    float distY = py - cy;
    float distance = (float)Math.Sqrt(distX * distX + distY * distY);

    return distance <= radius;
}
```

### Complete Hover Detection Integration
```csharp
// Source: PathPilot existing patterns + Avalonia ToolTip API
public class SkillTreeCanvas : Control
{
    private int? _hoveredNodeId = null;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Skip hover detection during pan
        if (_isPanning)
        {
            if (_hoveredNodeId.HasValue)
            {
                _hoveredNodeId = null;
                ToolTip.SetIsOpen(this, false);
            }
            return;
        }

        // Skip if no tree data
        if (TreeData == null)
            return;

        // Convert screen to world coordinates
        var screenPos = e.GetCurrentPoint(this).Position;
        var worldPos = ScreenToWorld(screenPos);

        // Find node at position
        var newHoveredNode = FindNodeAtPosition(worldPos);

        // Update tooltip only if hover changed
        if (newHoveredNode != _hoveredNodeId)
        {
            _hoveredNodeId = newHoveredNode;
            UpdateTooltip();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Clear hover when pointer leaves control
        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // Existing zoom code...

        // Clear hover during zoom
        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }

        // ... rest of zoom logic
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WPF: ToolTipService.ShowDuration, HideDelay | Avalonia 11.1: ToolTip.ShowDelay, BetweenShowDelay, IsOpen control | Avalonia 11.1 (Aug 2024) | More granular control, tooltip chaining support, better programmatic API |
| Spatial indexing for all collision detection | Linear search for small datasets (< 10k objects) | Modern performance baseline | Modern CPUs handle 1,400 distance checks in < 1ms; avoid premature optimization |
| Custom popup positioning logic | ToolTip.Placement with Pointer mode | Standard in Avalonia | Tooltip follows pointer automatically, respects screen bounds |

**Deprecated/outdated:**
- **WPF ToolTipService APIs**: Avalonia uses ToolTip.SetTip/SetIsOpen pattern instead
- **Complex spatial partitioning for small node counts**: Quadtrees worthwhile only at 10k+ objects with frequent queries

## Open Questions

Things that couldn't be fully resolved:

1. **Connected node count limits**
   - What we know: PoE skill tree has normal passives (4-5 connections) and cluster jewel sockets (can have 50+ connections)
   - What's unclear: Typical maximum connection count in practice, whether this needs limiting
   - Recommendation: Implement tooltip with all connections initially, add limit (first 15 + "... N more") if testing shows UI problems

2. **Tooltip update throttling necessity**
   - What we know: Tracking hover state prevents updates every frame; only updates on node change
   - What's unclear: Whether even "on change" updates are fast enough, or if debouncing is needed
   - Recommendation: Implement state-tracked updates first, profile performance, add throttling only if profiling shows frame drops

3. **Touch device support**
   - What we know: Desktop PoE players use mouse primarily
   - What's unclear: Whether touch hover (via PointerEntered) should be supported, how tooltip would work on touch screens
   - Recommendation: Focus on mouse hover for MVP, consider touch in future if requested

## Sources

### Primary (HIGH confidence)
- Avalonia ToolTip API: https://docs.avaloniaui.net/docs/reference/controls/tooltip
- Avalonia Pointer Events: https://docs.avaloniaui.net/docs/concepts/input/pointer
- Avalonia GitHub ToolTip.SetTip discussion: https://github.com/AvaloniaUI/Avalonia/discussions/12018
- Circle-Point collision detection (mathematical formula): https://www.jeffreythompson.org/collision-detection/point-circle.php
- Path of Exile Wiki (passive node count: 1,384 nodes): https://www.poewiki.net/wiki/Passive_skill

### Secondary (MEDIUM confidence)
- Avalonia performance with many objects: https://github.com/AvaloniaUI/Avalonia/discussions/13042
- Quadtree collision detection overview: https://pvigier.github.io/2019/08/04/quadtree-collision-detection.html
- Avalonia 11.1 release notes (tooltip improvements): https://avaloniaui.net/blog/avalonia-11-1-a-quantum-leap-in-cross-platform-ui-development

### Tertiary (LOW confidence)
- None - all key findings verified with official sources

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Avalonia ToolTip is documented official API, PathPilot already uses SkiaSharp
- Architecture: HIGH - Patterns based on Avalonia official docs, existing PathPilot implementation, and verified collision detection math
- Pitfalls: HIGH - Based on Avalonia GitHub discussions showing real issues, known performance patterns from Draw2D reference

**Research date:** 2026-02-04
**Valid until:** ~90 days (March 2026) - Avalonia UI is mature and stable, tooltip API unlikely to change significantly
