# Phase 3: Navigation - Research

**Researched:** 2026-02-04
**Domain:** Interactive canvas navigation (pan/zoom) with SkiaSharp and Avalonia
**Confidence:** HIGH

## Summary

Navigation for large canvas-based visualizations like the Path of Exile skill tree requires implementing mouse wheel zoom and drag-based panning using coordinate transformations. The standard approach uses SkiaSharp's matrix transformation system combined with Avalonia's pointer events to provide smooth, intuitive navigation.

The core challenge is managing coordinate system transformations between screen space (where pointer events occur) and world space (where tree nodes exist). The existing implementation already has SkiaSharp rendering infrastructure and a ZoomLevel property, providing a solid foundation for adding navigation.

**Primary recommendation:** Use SKMatrix for zoom/pan transformations, implement zoom-to-point with proper offset correction, handle Avalonia pointer events (PointerPressed/Moved/Released/WheelChanged) directly on the canvas control, and center the initial view by calculating allocated node bounds.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SkiaSharp | 2.88.x | Canvas rendering with matrix transforms | GPU-accelerated, built into Avalonia, provides complete 2D transformation API |
| Avalonia.Skia | 11.x | SkiaSharp integration for Avalonia | Official Avalonia rendering backend, provides ISkiaSharpApiLeaseFeature |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Avalonia.Controls.PanAndZoom | 11.3.0 | Pre-built pan/zoom control | When wrapping existing controls (not applicable for ICustomDrawOperation) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SKMatrix transformations | Avalonia RenderTransform | RenderTransform only scales visually, doesn't update coordinates for hit testing |
| Direct pointer events | ScrollViewer with scaled content | ScrollViewer approach used in current implementation but doesn't provide smooth zoom-to-point |
| Custom coordinate tracking | Third-party pan/zoom library | PanAndZoom control doesn't work with ICustomDrawOperation rendering |

**Installation:**
No additional packages needed - already available in existing project.

## Architecture Patterns

### Recommended State Management
```
SkillTreeCanvas (Control):
├── Transformation State
│   ├── _offsetX, _offsetY (float)     # Pan offset in world space
│   ├── _zoomLevel (float)             # Current zoom (0.02 to 2.0)
│   └── _transformMatrix (SKMatrix)    # Combined transformation matrix
├── Interaction State
│   ├── _isPanning (bool)              # Drag in progress
│   ├── _lastPointerPos (Point)        # Last drag position
│   └── _pointerCaptured (bool)        # Pointer captured for drag
└── Content Bounds
    ├── _contentBounds (SKRect)        # Bounding box of all content
    └── _allocatedNodeBounds (SKRect)  # Bounding box of allocated nodes
```

### Pattern 1: Zoom-to-Point with Matrix Transformation
**What:** Zoom centered on mouse cursor position, maintaining the point under cursor
**When to use:** Mouse wheel events
**Example:**
```csharp
// Source: Medium article on affine transformations + SkiaSharp matrix docs
private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
{
    var pointerPos = e.GetCurrentPoint(this).Position;

    // Get world coordinates of pointer BEFORE zoom
    var worldPosBefore = ScreenToWorld(pointerPos);

    // Update zoom level (clamped)
    var delta = e.Delta.Y;
    var zoomFactor = delta > 0 ? 1.1f : 0.9f;
    _zoomLevel = Math.Clamp(_zoomLevel * zoomFactor, MinZoom, MaxZoom);

    // Get world coordinates of pointer AFTER zoom
    var worldPosAfter = ScreenToWorld(pointerPos);

    // Correct offset to keep world position under cursor
    _offsetX += (worldPosBefore.X - worldPosAfter.X);
    _offsetY += (worldPosBefore.Y - worldPosAfter.Y);

    UpdateTransformMatrix();
    InvalidateVisual();
}

private SKPoint ScreenToWorld(Point screenPos)
{
    // Transform: world = (screen / zoom) + offset
    return new SKPoint(
        (float)(screenPos.X / _zoomLevel + _offsetX),
        (float)(screenPos.Y / _zoomLevel + _offsetY)
    );
}

private Point WorldToScreen(SKPoint worldPos)
{
    // Transform: screen = (world - offset) * zoom
    return new Point(
        (worldPos.X - _offsetX) * _zoomLevel,
        (worldPos.Y - _offsetY) * _zoomLevel
    );
}
```

### Pattern 2: Drag-Based Panning with Pointer Capture
**What:** Pan viewport by dragging with mouse
**When to use:** Left mouse button drag
**Example:**
```csharp
// Source: Avalonia pointer events documentation + pan/zoom algorithm
protected override void OnPointerPressed(PointerPressedEventArgs e)
{
    base.OnPointerPressed(e);

    var point = e.GetCurrentPoint(this);
    if (point.Properties.IsLeftButtonPressed)
    {
        _isPanning = true;
        _lastPointerPos = point.Position;
        e.Pointer.Capture(this);
        _pointerCaptured = true;
    }
}

protected override void OnPointerMoved(PointerEventArgs e)
{
    base.OnPointerMoved(e);

    if (!_isPanning) return;

    var currentPos = e.GetCurrentPoint(this).Position;

    // Calculate delta in screen space
    var deltaX = currentPos.X - _lastPointerPos.X;
    var deltaY = currentPos.Y - _lastPointerPos.Y;

    // Convert to world space (divide by zoom for consistent pan feel)
    _offsetX -= (float)(deltaX / _zoomLevel);
    _offsetY -= (float)(deltaY / _zoomLevel);

    _lastPointerPos = currentPos;

    UpdateTransformMatrix();
    InvalidateVisual();
}

protected override void OnPointerReleased(PointerReleasedEventArgs e)
{
    base.OnPointerReleased(e);

    if (_isPanning)
    {
        _isPanning = false;
        if (_pointerCaptured)
        {
            e.Pointer.Capture(null);
            _pointerCaptured = false;
        }
    }
}
```

### Pattern 3: Center View on Content
**What:** Calculate bounding box of allocated nodes and center viewport
**When to use:** Initial view setup when tree loads
**Example:**
```csharp
// Source: Bounding box algorithms + SkiaSharp path bounds
private void CenterOnAllocatedNodes()
{
    if (AllocatedNodeIds == null || !AllocatedNodeIds.Any() || TreeData == null)
    {
        CenterOnStartNode();
        return;
    }

    // Calculate bounding box of allocated nodes
    float minX = float.MaxValue, minY = float.MaxValue;
    float maxX = float.MinValue, maxY = float.MinValue;

    foreach (var nodeId in AllocatedNodeIds)
    {
        if (TreeData.Nodes.TryGetValue(nodeId, out var node) &&
            node.CalculatedX.HasValue && node.CalculatedY.HasValue)
        {
            minX = Math.Min(minX, node.CalculatedX.Value);
            maxX = Math.Max(maxX, node.CalculatedX.Value);
            minY = Math.Min(minY, node.CalculatedY.Value);
            maxY = Math.Max(maxY, node.CalculatedY.Value);
        }
    }

    if (minX == float.MaxValue)
    {
        CenterOnStartNode();
        return;
    }

    // Calculate center of bounding box
    var centerX = (minX + maxX) / 2f;
    var centerY = (minY + maxY) / 2f;

    // Set offset to center this point in viewport
    _offsetX = centerX - (float)(Bounds.Width / 2 / _zoomLevel);
    _offsetY = centerY - (float)(Bounds.Height / 2 / _zoomLevel);

    UpdateTransformMatrix();
    InvalidateVisual();
}

private void CenterOnStartNode()
{
    // Fallback: center on class start node (typically near center already)
    _offsetX = 14000f - (float)(Bounds.Width / 2 / _zoomLevel);
    _offsetY = 11000f - (float)(Bounds.Height / 2 / _zoomLevel);
}
```

### Pattern 4: Apply Transform in SkiaSharp Rendering
**What:** Apply pan/zoom transformation to SkiaSharp canvas
**When to use:** Every render call in ICustomDrawOperation
**Example:**
```csharp
// Source: SkiaSharp canvas transformation documentation
public void Render(ImmediateDrawingContext context)
{
    var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature))
        as ISkiaSharpApiLeaseFeature;
    if (leaseFeature == null) return;

    using var lease = leaseFeature.Lease();
    var canvas = lease.SkCanvas;

    canvas.Save();
    try
    {
        // Apply transformation matrix
        // Order matters: scale first, then translate
        canvas.Translate(-_offsetX * _zoomLevel, -_offsetY * _zoomLevel);
        canvas.Scale(_zoomLevel, _zoomLevel);

        // OR use matrix directly:
        // canvas.SetMatrix(_transformMatrix);

        RenderTree(canvas);
    }
    finally
    {
        canvas.Restore();
    }
}

private void UpdateTransformMatrix()
{
    // Create combined transformation matrix
    // Apply in correct order: Scale, then Translate
    var scaleMatrix = SKMatrix.CreateScale(_zoomLevel, _zoomLevel);
    var translateMatrix = SKMatrix.CreateTranslation(-_offsetX * _zoomLevel, -_offsetY * _zoomLevel);

    _transformMatrix = SKMatrix.Concat(translateMatrix, scaleMatrix);
}
```

### Anti-Patterns to Avoid
- **Scaling at origin without offset correction**: Always correct pan offset when zooming to prevent content jumping away from cursor
- **Using RenderTransform for zoom**: Only affects visual appearance, doesn't update coordinates for interaction
- **Calling InvalidateVisual on every PointerMoved**: Only invalidate when actually panning/zooming
- **Forgetting to divide pan delta by zoom**: Pan speed should be consistent regardless of zoom level
- **Not clamping zoom levels**: Users can zoom too far in/out causing performance issues or invisible content

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Pan/zoom for wrapped controls | Custom pan implementation | Avalonia.Controls.PanAndZoom | Handles touch gestures, animations, and edge cases |
| Matrix mathematics | Manual matrix multiplication | SKMatrix.Concat/CreateScale/CreateTranslation | Handles numerical precision, already optimized |
| Smooth zoom animation | Custom easing with timers | Not needed for this phase | Can add in polish phase if needed |
| Coordinate space conversion with perspective | Custom 3D math | SKMatrix with Persp properties | Handles 3D perspective transforms correctly |

**Key insight:** Matrix transformations for 2D pan/zoom are well-understood and implemented in SkiaSharp. The complexity is in the coordinate system management and proper event handling, not the math itself.

## Common Pitfalls

### Pitfall 1: Zoom Origin at (0,0) Instead of Mouse Position
**What goes wrong:** When user scrolls to zoom, content zooms toward/away from canvas origin instead of staying under cursor
**Why it happens:** Naive implementation only changes scale without adjusting pan offset
**How to avoid:** Implement zoom-to-point algorithm (see Pattern 1):
  1. Convert mouse position to world coordinates BEFORE zoom
  2. Apply new zoom level
  3. Convert same mouse position to world coordinates AFTER zoom
  4. Correct pan offset by the difference
**Warning signs:** Content "jumps" away from cursor when zooming

### Pitfall 2: Transform Order Confusion
**What goes wrong:** Applying transformations in wrong order causes incorrect rendering
**Why it happens:** Matrix multiplication is not commutative - order matters
**How to avoid:** Always apply in correct order:
  - For zoom-to-point: Translate to mouse position, Scale, Translate back
  - For rendering: Scale first (zoom), then Translate (pan)
  - Use SKMatrix.PreConcat for "apply before" and PostConcat for "apply after"
**Warning signs:** Content appears in wrong position, or scaling doesn't center correctly

### Pitfall 3: Screen Space vs World Space Coordinate Confusion
**What goes wrong:** Using screen coordinates where world coordinates expected (or vice versa)
**Why it happens:** Multiple coordinate systems in play - pointer events are in screen space, tree nodes are in world space
**How to avoid:**
  - Clearly name variables: `screenPos`, `worldPos`
  - Use conversion functions: `ScreenToWorld()`, `WorldToScreen()`
  - Formulas: `world = (screen / zoom) + offset`, `screen = (world - offset) * zoom`
**Warning signs:** Pan or zoom behaves inconsistently, nodes appear at wrong positions

### Pitfall 4: Not Dividing Pan Delta by Zoom Level
**What goes wrong:** Panning feels extremely fast when zoomed in, extremely slow when zoomed out
**Why it happens:** Mouse movement is in screen pixels, but pan offset is in world units
**How to avoid:** Always divide pan delta by current zoom level: `offsetX -= deltaX / zoomLevel`
**Warning signs:** Pan speed changes dramatically with zoom level

### Pitfall 5: Forgetting Pointer Capture Release
**What goes wrong:** Drag continues even after mouse button released, or can't start new drag
**Why it happens:** Pointer capture not released on PointerReleased
**How to avoid:**
  - Always release capture in OnPointerReleased: `e.Pointer.Capture(null)`
  - Track capture state with boolean flag
  - Release capture in OnPointerCaptureLost as safety net
**Warning signs:** Control "stuck" in drag mode, or drags don't start

### Pitfall 6: InvalidateVisual Performance Impact
**What goes wrong:** UI becomes sluggish during pan/zoom operations
**Why it happens:** Calling InvalidateVisual triggers full redraw - expensive for complex trees
**How to avoid:** This is expected behavior for real-time pan/zoom - SkiaSharp is GPU-accelerated so should be acceptable. Only call InvalidateVisual when transformation actually changes.
**Warning signs:** Noticeable lag or frame drops during pan/zoom

### Pitfall 7: Zoom Level Clamping Edge Cases
**What goes wrong:** Can zoom infinitely in/out, causing rendering issues or invisible content
**Why it happens:** No bounds checking on zoom level
**How to avoid:**
  - Define reasonable min/max zoom (e.g., 0.02 to 2.0)
  - Use Math.Clamp when updating zoom
  - Consider content-aware limits (don't zoom out past seeing whole tree)
**Warning signs:** Nodes become invisible dots or screen fills with single node

## Code Examples

Verified patterns from official sources:

### Complete Coordinate Transformation Functions
```csharp
// Source: Pan/zoom algorithms + SkiaSharp matrix documentation
// https://www.sunshine2k.de/articles/algorithm/panzoom/panzoom.html

// Convert screen coordinates (where pointer is) to world coordinates (where nodes are)
private SKPoint ScreenToWorld(Point screenPos)
{
    // Formula: world = (screen / zoom) + offset
    return new SKPoint(
        (float)(screenPos.X / _zoomLevel + _offsetX),
        (float)(screenPos.Y / _zoomLevel + _offsetY)
    );
}

// Convert world coordinates (where nodes are) to screen coordinates (for rendering)
private Point WorldToScreen(SKPoint worldPos)
{
    // Formula: screen = (world - offset) * zoom
    return new Point(
        (worldPos.X - _offsetX) * _zoomLevel,
        (worldPos.Y - _offsetY) * _zoomLevel
    );
}

// Alternative: Use SKMatrix.TryInvert for transformation
private SKPoint ScreenToWorldWithMatrix(Point screenPos)
{
    if (_transformMatrix.TryInvert(out var inverse))
    {
        return inverse.MapPoint(new SKPoint((float)screenPos.X, (float)screenPos.Y));
    }
    return new SKPoint((float)screenPos.X, (float)screenPos.Y);
}
```

### Zoom Level Clamping
```csharp
// Source: CSS clamp best practices + common zoom UI patterns
private const float MinZoom = 0.02f;  // Can see entire tree
private const float MaxZoom = 2.0f;   // 200% - individual nodes clear

private void UpdateZoomLevel(float newZoom)
{
    _zoomLevel = Math.Clamp(newZoom, MinZoom, MaxZoom);
}
```

### Avalonia Pointer Event Override Pattern
```csharp
// Source: Avalonia documentation + custom control patterns
// https://docs.avaloniaui.net/docs/concepts/input/pointer

// Override in custom Control subclass
protected override void OnPointerPressed(PointerPressedEventArgs e)
{
    base.OnPointerPressed(e);
    // Handle event
}

protected override void OnPointerMoved(PointerEventArgs e)
{
    base.OnPointerMoved(e);
    // Handle event
}

protected override void OnPointerReleased(PointerReleasedEventArgs e)
{
    base.OnPointerReleased(e);
    // Handle event
}

protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
{
    base.OnPointerWheelChanged(e);
    // Handle event
}

protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
{
    base.OnPointerCaptureLost(e);
    // Safety: clean up any drag state
    _isPanning = false;
    _pointerCaptured = false;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ScrollViewer with scaled canvas | Direct transformation with pointer events | Modern interactive canvases | More control, better zoom-to-point UX |
| RenderTransform scaling | SKMatrix transformations in render | SkiaSharp adoption | Proper coordinate transformation for hit testing |
| Manual matrix math | SKMatrix.CreateScale/CreateTranslation | SkiaSharp 2.x | Cleaner code, better precision |
| Mouse events | Pointer events (unified touch/pen/mouse) | Avalonia design | Cross-platform touch support |

**Deprecated/outdated:**
- **MakeScale/MakeTranslation**: Obsolete in SkiaSharp 2.88+, use CreateScale/CreateTranslation
- **WPF CaptureMouse/ReleaseMouseCapture**: Use Avalonia's e.Pointer.Capture() pattern

## Open Questions

Things that couldn't be fully resolved:

1. **Should pan boundaries be enforced?**
   - What we know: Can calculate content bounds and prevent panning beyond them
   - What's unclear: User may want to pan beyond tree for overview, or requirements don't specify this
   - Recommendation: Implement without hard boundaries initially (Phase 3), add optional boundaries in polish phase if needed

2. **Should zoom animation/easing be added?**
   - What we know: Smooth animation possible with Avalonia animation system or manual interpolation
   - What's unclear: Adds complexity, may not be needed for good UX
   - Recommendation: Start with instant zoom (Phase 3 requirement is just "zoom in/out"), add animation as enhancement if user testing shows it's needed

3. **Touch gesture support (pinch-to-zoom)?**
   - What we know: Avalonia supports GestureRecognizers for touch
   - What's unclear: Not in Phase 3 requirements, primarily desktop app
   - Recommendation: Defer to future phase if touch support becomes priority

## Sources

### Primary (HIGH confidence)
- [SkiaSharp SKCanvas API Documentation](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas?view=skiasharp-2.88) - Canvas transformation methods
- [SkiaSharp SKMatrix API Documentation](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skmatrix?view=skiasharp-2.88) - Matrix operations and methods
- [Avalonia Pointer Events Documentation](https://docs.avaloniaui.net/docs/concepts/input/pointer) - Official pointer event handling
- [SKMatrix Reference (Skia)](https://api.skia.org/classSkMatrix.html) - PreTranslate, PostTranslate, PreScale, PostScale documentation

### Secondary (MEDIUM confidence)
- [Pan and Zoom Algorithms](https://www.sunshine2k.de/articles/algorithm/panzoom/panzoom.html) - Mathematical formulas for coordinate transformations
- [Zooming at Mouse Coordinates with Affine Transformations](https://medium.com/@benjamin.botto/zooming-at-the-mouse-coordinates-with-affine-transformations-86e7312fd50b) - Zoom-to-point algorithm
- [Affine Transformations — Pan, Zoom, Skew](https://farazzshaikh.medium.com/affine-transformations-pan-zoom-skew-96a3adf38eb2) - Transform order and combination
- [How to zoom in on a point using scale and translate](https://www.geeksforgeeks.org/javascript/how-to-zoom-in-on-a-point-using-scale-and-translate/) - JavaScript example applicable to canvas
- [Avalonia PanAndZoom Control](https://github.com/wieslawsoltes/PanAndZoom) - Reference implementation for wrapped controls
- [Avalonia Custom Control Pointer Events Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/9794) - Community patterns
- [Avalonia Pointer Capture Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/6201) - Capture behavior and patterns
- [Bounding Box Center Calculation](https://medium.com/@egimata/understanding-and-creating-the-bounding-box-of-a-geometry-d6358a9f7121) - Algorithm for centering on content
- [Mapbox Restrict Bounds Example](https://docs.mapbox.com/mapbox-gl-js/example/restrict-bounds/) - Pan boundary constraints
- [Windows Panning Guidelines](https://learn.microsoft.com/en-us/windows/apps/design/input/guidelines-for-panning) - UX best practices

### Tertiary (LOW confidence)
- Various Stack Overflow and forum discussions on pan/zoom implementations
- WebSearch results for general pan/zoom patterns (multiple sources agreeing increases confidence)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - SkiaSharp and Avalonia pointer events are official, documented APIs already in use
- Architecture: HIGH - Patterns verified against official documentation and multiple authoritative sources
- Pitfalls: HIGH - Based on official docs, common issues documented in community discussions, and general pan/zoom implementation knowledge

**Research date:** 2026-02-04
**Valid until:** ~2026-03-04 (30 days) - Avalonia and SkiaSharp APIs are stable, unlikely to change significantly
