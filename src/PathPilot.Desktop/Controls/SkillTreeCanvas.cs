using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
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

    static SkillTreeCanvas()
    {
        // Trigger redraw when properties change
        AffectsRender<SkillTreeCanvas>(TreeDataProperty, AllocatedNodeIdsProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Return early if no tree data
        if (TreeData == null)
            return;

        // Create custom draw operation for SkiaSharp rendering
        var operation = new SkillTreeDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            TreeData,
            AllocatedNodeIds ?? new HashSet<int>());

        context.Custom(operation);
    }

    /// <summary>
    /// Custom draw operation that renders directly to SkiaSharp canvas
    /// </summary>
    private class SkillTreeDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly SkillTreeData _treeData;
        private readonly HashSet<int> _allocatedNodeIds;

        public SkillTreeDrawOperation(Rect bounds, SkillTreeData treeData, HashSet<int> allocatedNodeIds)
        {
            _bounds = bounds;
            _treeData = treeData;
            _allocatedNodeIds = allocatedNodeIds;
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
