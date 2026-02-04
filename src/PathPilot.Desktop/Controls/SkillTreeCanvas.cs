using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using PathPilot.Core.Models;
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

    static SkillTreeCanvas()
    {
        // Trigger redraw when properties change
        AffectsRender<SkillTreeCanvas>(TreeDataProperty, AllocatedNodeIdsProperty, ZoomLevelProperty, BackgroundProperty);
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

        // Create custom draw operation for SkiaSharp rendering
        var operation = new SkillTreeDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            TreeData,
            AllocatedNodeIds ?? new HashSet<int>(),
            (float)ZoomLevel,
            _offsetX,
            _offsetY);

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

            // Determine radius based on node type (matches DrawNodes)
            float radius;
            if (node.IsKeystone)
                radius = 18f;
            else if (node.IsNotable)
                radius = 12f;
            else if (node.IsJewelSocket)
                radius = 10f;
            else
                radius = 6f;

            // Calculate distance from world position to node center
            var dx = worldPos.X - nodeX;
            var dy = worldPos.Y - nodeY;
            var distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance <= radius)
                return node.Id;
        }

        return null;
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

        public SkillTreeDrawOperation(Rect bounds, SkillTreeData treeData, HashSet<int> allocatedNodeIds, float zoomLevel, float offsetX, float offsetY)
        {
            _bounds = bounds;
            _treeData = treeData;
            _allocatedNodeIds = allocatedNodeIds;
            _zoomLevel = zoomLevel;
            _offsetX = offsetX;
            _offsetY = offsetY;
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
            // Draw connections first (so nodes appear on top)
            DrawConnections(canvas);

            // Draw nodes on top of connections
            DrawNodes(canvas);
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
            // Create paint objects for allocated and unallocated nodes
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

                // Determine radius based on node type
                float radius;
                if (node.IsKeystone)
                    radius = 18f;
                else if (node.IsNotable)
                    radius = 12f;
                else if (node.IsJewelSocket)
                    radius = 10f;
                else
                    radius = 6f;

                // Use gold color for allocated nodes, dark gray for unallocated
                var paint = _allocatedNodeIds.Contains(node.Id) ? allocatedPaint : unallocatedPaint;

                canvas.DrawCircle(x, y, radius, paint);
            }
        }
    }
}
