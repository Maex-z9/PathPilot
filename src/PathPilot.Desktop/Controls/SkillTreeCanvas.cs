using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using PathPilot.Core.Models;
using PathPilot.Core.Services;
using SkiaSharp;

namespace PathPilot.Desktop.Controls;

/// <summary>
/// Custom Avalonia control for rendering Path of Exile skill tree using SkiaSharp
/// </summary>
public class SkillTreeCanvas : Control
{
    /// <summary>
    /// The parsed skill tree data to render
    /// </summary>
    public static readonly StyledProperty<SkillTreeData?> TreeDataProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, SkillTreeData?>(nameof(TreeData));

    /// <summary>
    /// IDs of allocated nodes to highlight
    /// </summary>
    public static readonly StyledProperty<HashSet<int>?> AllocatedNodeIdsProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, HashSet<int>?>(nameof(AllocatedNodeIds));

    /// <summary>
    /// Zoom level for rendering (1.0 = 100%)
    /// </summary>
    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, double>(nameof(ZoomLevel), 1.0);

    /// <summary>
    /// Background brush for the control (enables pointer event reception)
    /// </summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, IBrush?>(nameof(Background));

    public SkillTreeData? TreeData
    {
        get => GetValue(TreeDataProperty);
        set => SetValue(TreeDataProperty, value);
    }

    public HashSet<int>? AllocatedNodeIds
    {
        get => GetValue(AllocatedNodeIdsProperty);
        set => SetValue(AllocatedNodeIdsProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    // Navigation state
    private float _offsetX = 0f;
    private float _offsetY = 0f;
    private bool _isPanning = false;
    private Point _lastPointerPos;

    // Hover state
    private int? _hoveredNodeId = null;

    // Zoom limits
    private const float MinZoom = 0.02f;
    private const float MaxZoom = 2.0f;

    // Debug flag to only log bounds once
    private bool _boundsLogged = false;

    // Sprite service for rendering
    private SkillTreeSpriteService? _spriteService = null;
    private string _currentSpriteZoomKey = "0.3835"; // Default to highest quality

    static SkillTreeCanvas()
    {
        // Trigger redraw when properties change
        AffectsRender<SkillTreeCanvas>(TreeDataProperty, AllocatedNodeIdsProperty, ZoomLevelProperty, BackgroundProperty);
    }

    /// <summary>
    /// Sets the sprite service for sprite-based rendering
    /// </summary>
    public void SetSpriteService(SkillTreeSpriteService spriteService)
    {
        _spriteService = spriteService;
        InvalidateVisual();
    }

    /// <summary>
    /// Maps current zoom level to nearest GGG sprite quality
    /// GGG zoom thresholds: 0.1246, 0.2109, 0.2972, 0.3835
    /// Midpoints for switching: 0.1728, 0.2540, 0.3403
    /// </summary>
    private static string GetSpriteZoomKey(float currentZoom)
    {
        if (currentZoom < 0.1728f) return "0.1246";
        if (currentZoom < 0.2540f) return "0.2109";
        if (currentZoom < 0.3403f) return "0.2972";
        return "0.3835";
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Log bounds once on first render to debug
        if (!_boundsLogged && Bounds.Width > 0 && Bounds.Height > 0)
        {
            Console.WriteLine($"[SkillTreeCanvas] Initial Render: Bounds={Bounds}, Background={Background}, IsHitTestVisible={IsHitTestVisible}");
            _boundsLogged = true;
        }

        // Render background to make control hit-testable for pointer events
        if (Background != null)
        {
            context.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }

        // Return early if no tree data
        if (TreeData == null)
            return;

        // Build sprite bitmap and coord dictionaries for current zoom level
        var spriteBitmaps = new Dictionary<string, SKBitmap>();
        var spriteCoords = new Dictionary<string, SpriteSheetData>();

        if (_spriteService != null && TreeData.SpriteSheets.Count > 0)
        {
            // Synchronously gather already-loaded bitmaps from service's in-memory cache
            foreach (var (spriteType, zoomDict) in TreeData.SpriteSheets)
            {
                if (zoomDict.TryGetValue(_currentSpriteZoomKey, out var sheetData))
                {
                    // Store coord data for lookup
                    spriteCoords[spriteType] = sheetData;

                    // Try to get already-loaded bitmap (service caches them)
                    if (!string.IsNullOrEmpty(sheetData.Filename))
                    {
                        var bitmap = _spriteService.GetSpriteSheetAsync(sheetData.Filename).Result;
                        if (bitmap != null)
                        {
                            spriteBitmaps[spriteType] = bitmap;
                        }
                    }
                }
            }
        }

        // Create custom draw operation for SkiaSharp rendering
        var operation = new SkillTreeDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            TreeData,
            AllocatedNodeIds ?? new HashSet<int>(),
            (float)ZoomLevel,
            _offsetX,
            _offsetY,
            spriteBitmaps,
            spriteCoords);

        context.Custom(operation);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Get pointer position
        var pointerPos = e.GetCurrentPoint(this).Position;

        // Convert to world coords BEFORE zoom
        var worldBefore = ScreenToWorld(pointerPos);

        // Calculate zoom factor
        var delta = e.Delta.Y;
        var zoomFactor = delta > 0 ? 1.1f : 0.9f;

        // Update zoom level with clamping
        var newZoom = (float)ZoomLevel * zoomFactor;
        ZoomLevel = Math.Clamp(newZoom, MinZoom, MaxZoom);

        // Convert same pointer position to world coords AFTER zoom
        var worldAfter = ScreenToWorld(pointerPos);

        // Correct offset to keep content under cursor
        _offsetX += worldBefore.X - worldAfter.X;
        _offsetY += worldBefore.Y - worldAfter.Y;

        // Clear hover state during zoom
        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }

        // Check if we crossed a sprite LOD threshold
        var newZoomKey = GetSpriteZoomKey((float)ZoomLevel);
        if (newZoomKey != _currentSpriteZoomKey && _spriteService != null && TreeData != null)
        {
            _currentSpriteZoomKey = newZoomKey;
            Console.WriteLine($"Sprite LOD switched to: {newZoomKey}");

            // Preload new zoom level sprites asynchronously (non-blocking)
            var treeData = TreeData; // Capture for async context
            Task.Run(async () =>
            {
                await _spriteService.PreloadSpriteSheetsAsync(treeData, newZoomKey);
                // Trigger re-render on UI thread after sprites loaded
                Avalonia.Threading.Dispatcher.UIThread.Post(() => InvalidateVisual());
            });
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var currentPoint = e.GetCurrentPoint(this);
        Console.WriteLine($"[SkillTreeCanvas] OnPointerPressed: Position={currentPoint.Position}, LeftButton={currentPoint.Properties.IsLeftButtonPressed}, Bounds={Bounds}");

        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPointerPos = currentPoint.Position;
            e.Pointer.Capture(this);
            e.Handled = true;
            Console.WriteLine($"[SkillTreeCanvas] Started panning, captured pointer");
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Handle hover detection when NOT panning
        if (!_isPanning && TreeData != null)
        {
            var currentPos = e.GetCurrentPoint(this).Position;
            var worldPos = ScreenToWorld(currentPos);
            var nodeId = FindNodeAtPosition(worldPos);

            if (nodeId != _hoveredNodeId)
            {
                _hoveredNodeId = nodeId;
                UpdateTooltip();
            }
        }
        else if (_isPanning)
        {
            // Clear hover state during panning
            if (_hoveredNodeId.HasValue)
            {
                _hoveredNodeId = null;
                ToolTip.SetIsOpen(this, false);
            }

            var currentPos = e.GetCurrentPoint(this).Position;
            var delta = currentPos - _lastPointerPos;

            Console.WriteLine($"[SkillTreeCanvas] OnPointerMoved (panning): Delta={delta}, CurrentPos={currentPos}, LastPos={_lastPointerPos}");

            // Update offsets to pan in same direction as drag
            // Since render uses: Translate(-offsetX * zoom, -offsetY * zoom)
            // We need to move offset in opposite direction of drag
            _offsetX -= (float)(delta.X / ZoomLevel);
            _offsetY -= (float)(delta.Y / ZoomLevel);

            Console.WriteLine($"[SkillTreeCanvas] Updated offsets: X={_offsetX}, Y={_offsetY}");

            _lastPointerPos = currentPos;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        Console.WriteLine($"[SkillTreeCanvas] OnPointerReleased: _isPanning={_isPanning}");

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
            Console.WriteLine($"[SkillTreeCanvas] Stopped panning, released pointer capture");
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPanning = false;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Clear hover state when pointer leaves canvas
        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }
    }

    public void ZoomIn()
    {
        // Zoom toward center of viewport
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;

        // Calculate world position at center BEFORE zoom change
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        // Update zoom level
        var newZoom = Math.Clamp((float)ZoomLevel * 1.15f, MinZoom, MaxZoom);
        ZoomLevel = newZoom;

        // Calculate world position at center AFTER zoom change
        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);

        // Adjust offset to keep center point fixed in world space
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        InvalidateVisual();
    }

    public void ZoomOut()
    {
        // Zoom from center of viewport
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;

        // Calculate world position at center BEFORE zoom change
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        // Update zoom level
        var newZoom = Math.Clamp((float)ZoomLevel * 0.85f, MinZoom, MaxZoom);
        ZoomLevel = newZoom;

        // Calculate world position at center AFTER zoom change
        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);

        // Adjust offset to keep center point fixed in world space
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        InvalidateVisual();
    }

    public void SetZoom(double targetZoom)
    {
        // Zoom toward center of viewport
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;

        // Calculate world position at center BEFORE zoom change
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        // Update zoom level
        ZoomLevel = Math.Clamp(targetZoom, MinZoom, MaxZoom);

        // Calculate world position at center AFTER zoom change
        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);

        // Adjust offset to keep center point fixed in world space
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        InvalidateVisual();
    }

    private SKPoint ScreenToWorld(Point screenPos)
    {
        return new SKPoint(
            (float)(screenPos.X / ZoomLevel + _offsetX),
            (float)(screenPos.Y / ZoomLevel + _offsetY));
    }

    private int? FindNodeAtPosition(SKPoint worldPos)
    {
        if (TreeData == null)
            return null;

        foreach (var node in TreeData.Nodes.Values)
        {
            if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
                continue;

            var nodeX = node.CalculatedX.Value;
            var nodeY = node.CalculatedY.Value;

            // Try to get radius from sprite dimensions if available
            float radius = GetNodeHitTestRadius(node);

            // Calculate distance from world position to node center
            var dx = worldPos.X - nodeX;
            var dy = worldPos.Y - nodeY;
            var distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance <= radius)
                return node.Id;
        }

        return null;
    }

    private float GetNodeHitTestRadius(PassiveNode node)
    {
        // Try to get sprite dimensions for more accurate hit testing
        if (_spriteService != null && TreeData != null)
        {
            // Determine sprite type
            string spriteType;
            if (node.IsKeystone)
                spriteType = "keystoneActive"; // Use active for size reference
            else if (node.IsNotable)
                spriteType = "notableActive";
            else if (node.IsJewelSocket)
                spriteType = "jewel";
            else if (node.IsMastery)
                spriteType = ""; // No mastery sprites yet
            else
                spriteType = "normalActive";

            if (!string.IsNullOrEmpty(spriteType) &&
                TreeData.SpriteSheets.TryGetValue(spriteType, out var zoomDict) &&
                zoomDict.TryGetValue(_currentSpriteZoomKey, out var sheetData))
            {
                // Get icon key
                string iconKey = node.IsJewelSocket ? "JewelSocketActiveBlue" :
                    (!string.IsNullOrEmpty(node.Icon) ? node.Icon : "");

                if (!string.IsNullOrEmpty(iconKey) &&
                    sheetData.Coords.TryGetValue(iconKey, out var coord))
                {
                    // Use max of width/height divided by 2 as radius
                    return Math.Max(coord.W, coord.H) / 2f;
                }
            }
        }

        // Fallback to hardcoded radii
        if (node.IsKeystone)
            return 18f;
        else if (node.IsNotable)
            return 12f;
        else if (node.IsJewelSocket)
            return 10f;
        else
            return 6f;
    }

    private void UpdateTooltip()
    {
        if (_hoveredNodeId.HasValue && TreeData != null)
        {
            if (TreeData.Nodes.TryGetValue(_hoveredNodeId.Value, out var node))
            {
                var content = BuildTooltipContent(node);
                ToolTip.SetTip(this, content);
                ToolTip.SetIsOpen(this, true);
                return;
            }
        }

        ToolTip.SetIsOpen(this, false);
    }

    private object BuildTooltipContent(PassiveNode node)
    {
        var panel = new StackPanel
        {
            Spacing = 4
        };

        // Node name (bold, larger font)
        panel.Children.Add(new TextBlock
        {
            Text = node.Name,
            FontWeight = FontWeight.Bold,
            FontSize = 14
        });

        // Node stats
        if (node.Stats != null && node.Stats.Count > 0)
        {
            foreach (var stat in node.Stats)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = stat,
                    FontSize = 12
                });
            }
        }

        // Connected nodes section
        if (node.ConnectedNodes != null && node.ConnectedNodes.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Connected to:",
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            // Limit to first 15 connections
            var connectionsToShow = node.ConnectedNodes.Take(15).ToList();
            foreach (var connectedId in connectionsToShow)
            {
                if (TreeData.Nodes.TryGetValue(connectedId, out var connectedNode))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"• {connectedNode.Name}",
                        FontSize = 11
                    });
                }
            }

            // Show overflow indicator if more than 15 connections
            if (node.ConnectedNodes.Count > 15)
            {
                var remaining = node.ConnectedNodes.Count - 15;
                panel.Children.Add(new TextBlock
                {
                    Text = $"... and {remaining} more",
                    FontSize = 11
                });
            }
        }

        return panel;
    }

    public void CenterOnAllocatedNodes()
    {
        // Check if we have allocated nodes and tree data
        if (AllocatedNodeIds == null || AllocatedNodeIds.Count == 0 || TreeData == null)
        {
            CenterOnStartNode();
            return;
        }

        // Calculate bounding box of allocated nodes
        float? minX = null, maxX = null, minY = null, maxY = null;

        foreach (var nodeId in AllocatedNodeIds)
        {
            if (TreeData.Nodes.TryGetValue(nodeId, out var node))
            {
                if (node.CalculatedX.HasValue && node.CalculatedY.HasValue)
                {
                    var x = node.CalculatedX.Value;
                    var y = node.CalculatedY.Value;

                    minX = minX.HasValue ? Math.Min(minX.Value, x) : x;
                    maxX = maxX.HasValue ? Math.Max(maxX.Value, x) : x;
                    minY = minY.HasValue ? Math.Min(minY.Value, y) : y;
                    maxY = maxY.HasValue ? Math.Max(maxY.Value, y) : y;
                }
            }
        }

        // If no valid nodes found, fall back to start node
        if (!minX.HasValue || !maxX.HasValue || !minY.HasValue || !maxY.HasValue)
        {
            CenterOnStartNode();
            return;
        }

        // Calculate center of allocated nodes
        var centerX = (minX.Value + maxX.Value) / 2f;
        var centerY = (minY.Value + maxY.Value) / 2f;

        // Set initial zoom to show allocated nodes clearly
        ZoomLevel = 0.08f;

        // Center on allocated nodes (only if bounds are ready)
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _offsetX = centerX - (float)(Bounds.Width / 2 / ZoomLevel);
            _offsetY = centerY - (float)(Bounds.Height / 2 / ZoomLevel);
            InvalidateVisual();
        }
    }

    private void CenterOnStartNode()
    {
        // Center on tree origin (approximately 14000, 11000 after GGG offset applied)
        const float treeCenterX = 14000f;
        const float treeCenterY = 11000f;

        // Set default zoom
        ZoomLevel = 0.08f;

        // Center on tree origin (only if bounds are ready)
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _offsetX = treeCenterX - (float)(Bounds.Width / 2 / ZoomLevel);
            _offsetY = treeCenterY - (float)(Bounds.Height / 2 / ZoomLevel);
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Custom draw operation that renders directly to SkiaSharp canvas
    /// </summary>
    private class SkillTreeDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly SkillTreeData _treeData;
        private readonly HashSet<int> _allocatedNodeIds;
        private readonly float _zoomLevel;
        private readonly float _offsetX;
        private readonly float _offsetY;
        private readonly Dictionary<string, SKBitmap> _spriteBitmaps;
        private readonly Dictionary<string, SpriteSheetData> _spriteCoords;

        public SkillTreeDrawOperation(Rect bounds, SkillTreeData treeData, HashSet<int> allocatedNodeIds,
            float zoomLevel, float offsetX, float offsetY,
            Dictionary<string, SKBitmap> spriteBitmaps, Dictionary<string, SpriteSheetData> spriteCoords)
        {
            _bounds = bounds;
            _treeData = treeData;
            _allocatedNodeIds = allocatedNodeIds;
            _zoomLevel = zoomLevel;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _spriteBitmaps = spriteBitmaps;
            _spriteCoords = spriteCoords;
        }

        public Rect Bounds => _bounds;

        public void Dispose()
        {
            // No resources to dispose in this implementation
        }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            // Get ISkiaSharpApiLeaseFeature for direct SkiaSharp access
            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            // Save canvas state
            canvas.Save();

            try
            {
                // Apply transformations: translate first for offset, then scale for zoom
                canvas.Translate(-_offsetX * _zoomLevel, -_offsetY * _zoomLevel);
                canvas.Scale(_zoomLevel, _zoomLevel);
                RenderTree(canvas);
            }
            finally
            {
                // Always restore canvas state
                canvas.Restore();
            }
        }

        private void RenderTree(SKCanvas canvas)
        {
            // Draw group backgrounds first (behind everything)
            DrawGroupBackgrounds(canvas);

            // Draw connections on top of backgrounds
            DrawConnections(canvas);

            // Draw nodes on top of connections
            DrawNodes(canvas);
        }

        private void DrawGroupBackgrounds(SKCanvas canvas)
        {
            // Check if we have group background sprite sheet
            if (!_spriteCoords.TryGetValue("groupBackground", out var bgCoords) ||
                !_spriteBitmaps.TryGetValue("groupBackground", out var bgBitmap))
            {
                return; // No background sprites available, skip
            }

            foreach (var group in _treeData.Groups.Values)
            {
                if (group.Background == null || string.IsNullOrEmpty(group.Background.Image))
                    continue;

                // Look up background sprite coordinates
                if (!bgCoords.Coords.TryGetValue(group.Background.Image, out var coord))
                    continue;

                // Source rect from sprite sheet
                var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);

                // Destination rect centered on group position
                var destRect = new SKRect(
                    group.X - coord.W / 2f,
                    group.Y - coord.H / 2f,
                    group.X + coord.W / 2f,
                    group.Y + coord.H / 2f);

                // Draw background
                canvas.DrawBitmap(bgBitmap, srcRect, destRect);

                // If half image, draw mirrored copy
                if (group.Background.IsHalfImage)
                {
                    canvas.Save();
                    canvas.Translate(group.X, group.Y);
                    canvas.Scale(-1, 1);
                    canvas.Translate(-group.X, -group.Y);
                    canvas.DrawBitmap(bgBitmap, srcRect, destRect);
                    canvas.Restore();
                }
            }
        }

        private void DrawConnections(SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(80, 80, 80),
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            // Batch all connections into single path for performance
            using var path = new SKPath();

            // Track drawn connections to avoid duplicates (connections are bidirectional)
            var drawnConnections = new HashSet<(int, int)>();

            foreach (var node in _treeData.Nodes.Values)
            {
                if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
                    continue;

                // Skip connections from ascendancy nodes (eliminates long lines from center to edges)
                if (node.IsAscendancy)
                    continue;

                var startX = node.CalculatedX.Value;
                var startY = node.CalculatedY.Value;

                foreach (var connectedId in node.ConnectedNodes)
                {
                    // Create ordered pair to avoid drawing same connection twice
                    var pair = node.Id < connectedId
                        ? (node.Id, connectedId)
                        : (connectedId, node.Id);

                    if (drawnConnections.Contains(pair))
                        continue;

                    drawnConnections.Add(pair);

                    if (_treeData.Nodes.TryGetValue(connectedId, out var connectedNode))
                    {
                        if (!connectedNode.CalculatedX.HasValue || !connectedNode.CalculatedY.HasValue)
                            continue;

                        // Skip connections to ascendancy nodes
                        if (connectedNode.IsAscendancy)
                            continue;

                        // Add line to batched path
                        path.MoveTo(startX, startY);
                        path.LineTo(connectedNode.CalculatedX.Value, connectedNode.CalculatedY.Value);
                    }
                }
            }

            // Single draw call for ALL connections
            canvas.DrawPath(path, paint);
        }

        private void DrawNodes(SKCanvas canvas)
        {
            // Fallback paint objects if sprites unavailable
            using var allocatedPaint = new SKPaint
            {
                Color = new SKColor(200, 150, 50), // Gold
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var unallocatedPaint = new SKPaint
            {
                Color = new SKColor(60, 60, 60), // Dark gray
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            foreach (var node in _treeData.Nodes.Values)
            {
                if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
                    continue;

                var x = node.CalculatedX.Value;
                var y = node.CalculatedY.Value;
                var isAllocated = _allocatedNodeIds.Contains(node.Id);

                // Try sprite-based rendering first
                if (TryDrawNodeSprite(canvas, node, x, y, isAllocated))
                {
                    // Successfully drew sprite, also draw frame on top
                    TryDrawNodeFrame(canvas, node, x, y, isAllocated);
                    continue;
                }

                // Fallback to colored circle rendering
                float radius;
                if (node.IsKeystone)
                    radius = 18f;
                else if (node.IsNotable)
                    radius = 12f;
                else if (node.IsJewelSocket)
                    radius = 10f;
                else
                    radius = 6f;

                var paint = isAllocated ? allocatedPaint : unallocatedPaint;
                canvas.DrawCircle(x, y, radius, paint);
            }
        }

        private bool TryDrawNodeSprite(SKCanvas canvas, PassiveNode node, float x, float y, bool isAllocated)
        {
            // Determine sprite type based on node type and allocation
            string spriteType;
            if (node.IsKeystone)
                spriteType = isAllocated ? "keystoneActive" : "keystoneInactive";
            else if (node.IsNotable)
                spriteType = isAllocated ? "notableActive" : "notableInactive";
            else if (node.IsJewelSocket)
                spriteType = "jewel"; // Jewel sprites handle active/inactive via different coord keys
            else if (node.IsMastery)
                return false; // Skip mastery sprites for now
            else
                spriteType = isAllocated ? "normalActive" : "normalInactive";

            // Get sprite sheet for this type
            if (!_spriteCoords.TryGetValue(spriteType, out var sheetData) ||
                !_spriteBitmaps.TryGetValue(spriteType, out var bitmap))
            {
                return false; // Sprite data not available
            }

            // Get node icon key - for jewel sockets, use special keys
            string iconKey;
            if (node.IsJewelSocket)
            {
                iconKey = isAllocated ? "JewelSocketActiveBlue" : "JewelSocketNormal";
            }
            else if (string.IsNullOrEmpty(node.Icon))
            {
                return false; // No icon defined for this node
            }
            else
            {
                iconKey = node.Icon;
            }

            // Look up sprite coordinates
            if (!sheetData.Coords.TryGetValue(iconKey, out var coord))
            {
                return false; // Icon not found in sprite sheet
            }

            // Source rect from sprite sheet
            var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);

            // Destination rect centered on node position
            var destRect = new SKRect(
                x - coord.W / 2f,
                y - coord.H / 2f,
                x + coord.W / 2f,
                y + coord.H / 2f);

            // Draw sprite
            canvas.DrawBitmap(bitmap, srcRect, destRect);
            return true;
        }

        private void TryDrawNodeFrame(SKCanvas canvas, PassiveNode node, float x, float y, bool isAllocated)
        {
            // Get frame sprite sheet
            if (!_spriteCoords.TryGetValue("frame", out var frameData) ||
                !_spriteBitmaps.TryGetValue("frame", out var frameBitmap))
            {
                return; // Frame sprites not available
            }

            // Determine frame key based on node type
            string frameKey;
            if (node.IsKeystone)
                frameKey = isAllocated ? "KeystoneFrameAllocated" : "KeystoneFrameUnallocated";
            else if (node.IsNotable)
                frameKey = isAllocated ? "NotableFrameAllocated" : "NotableFrameUnallocated";
            else if (node.IsJewelSocket)
                return; // Jewel sockets have frames in jewel sprite sheet
            else
                frameKey = isAllocated ? "PSSkillFrameActive" : "PSSkillFrame";

            // Look up frame coordinates
            if (!frameData.Coords.TryGetValue(frameKey, out var coord))
            {
                return; // Frame not found
            }

            // Source rect from sprite sheet
            var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);

            // Destination rect centered on node position (slightly larger than icon)
            var destRect = new SKRect(
                x - coord.W / 2f,
                y - coord.H / 2f,
                x + coord.W / 2f,
                y + coord.H / 2f);

            // Draw frame
            canvas.DrawBitmap(frameBitmap, srcRect, destRect);
        }
    }
}
