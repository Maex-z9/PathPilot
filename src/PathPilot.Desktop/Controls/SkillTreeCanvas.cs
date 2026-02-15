using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Custom Avalonia control for rendering Path of Exile skill tree using SkiaSharp.
/// Uses SKPicture caching for smooth panning — tree is rendered once to a display list,
/// then replayed instantly during pan. Rebuilds only on zoom/allocation/sprite changes.
/// </summary>
public class SkillTreeCanvas : Control
{
    public static readonly StyledProperty<SkillTreeData?> TreeDataProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, SkillTreeData?>(nameof(TreeData));

    public static readonly StyledProperty<HashSet<int>?> AllocatedNodeIdsProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, HashSet<int>?>(nameof(AllocatedNodeIds));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<Dictionary<int, int>?> MasterySelectionsProperty =
        AvaloniaProperty.Register<SkillTreeCanvas, Dictionary<int, int>?>(nameof(MasterySelections));

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

    /// <summary>
    /// Mastery selections decoded from tree URL: nodeId → effectId
    /// </summary>
    public Dictionary<int, int>? MasterySelections
    {
        get => GetValue(MasterySelectionsProperty);
        set => SetValue(MasterySelectionsProperty, value);
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

    // Sprite service
    private SkillTreeSpriteService? _spriteService = null;
    private string _currentSpriteZoomKey = "0.1246"; // Must match initial ZoomLevel (0.15)

    // Cached sprite data — rebuilt only when zoom key changes
    private Dictionary<string, SKBitmap> _cachedSpriteBitmaps = new();
    private Dictionary<string, SpriteSheetData> _cachedSpriteCoords = new();
    private string? _cachedZoomKey = null;

    // Sprite scale: converts sprite-sheet pixels to world-space units.
    // At zoom key "0.1246", a 28px sprite → 28*scale world units.
    // Factor 0.5 because GGG sprites are designed for 2x display density.
    private float _spriteScale = 0.5f / 0.1246f;

    // SKPicture cache — the key performance optimization.
    // IMPORTANT: Only the render thread touches _cachedPicture to avoid race conditions.
    // UI thread signals rebuild via _pictureIsDirty flag.
    private SKPicture? _cachedPicture = null;
    private volatile bool _pictureIsDirty = true;
    private float _pictureZoom = 0f;
    private int _pictureAllocCount = -1;
    private int _pictureSpriteCount = -1;
    private string? _pictureZoomKey = null;

    static SkillTreeCanvas()
    {
        AffectsRender<SkillTreeCanvas>(TreeDataProperty, AllocatedNodeIdsProperty, ZoomLevelProperty, BackgroundProperty);
    }

    public void SetSpriteService(SkillTreeSpriteService spriteService)
    {
        _spriteService = spriteService;
        _cachedZoomKey = null;
        _pictureIsDirty = true;
        InvalidateVisual();
    }

    private void RebuildSpriteCache()
    {
        var bitmaps = new Dictionary<string, SKBitmap>();
        var coords = new Dictionary<string, SpriteSheetData>();

        if (_spriteService != null && TreeData?.SpriteSheets.Count > 0)
        {
            foreach (var (spriteType, zoomDict) in TreeData.SpriteSheets)
            {
                if (zoomDict.TryGetValue(_currentSpriteZoomKey, out var sheetData))
                {
                    coords[spriteType] = sheetData;
                    if (!string.IsNullOrEmpty(sheetData.Filename))
                    {
                        var bitmap = _spriteService.TryGetLoadedBitmap(sheetData.Filename);
                        if (bitmap != null)
                            bitmaps[spriteType] = bitmap;
                    }
                }
            }
        }

        _cachedSpriteBitmaps = bitmaps;
        _cachedSpriteCoords = coords;
        _cachedZoomKey = _currentSpriteZoomKey;

        Console.WriteLine($"RebuildSpriteCache: {bitmaps.Count} bitmaps, {coords.Count} coord sets for zoom key {_currentSpriteZoomKey}");
        foreach (var (k, v) in coords)
            Console.WriteLine($"  {k}: {v.Coords.Count} coords, bitmap={bitmaps.ContainsKey(k)}");

        // Update sprite scale from zoom key (0.5 factor for 2x density sprites)
        if (float.TryParse(_currentSpriteZoomKey, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoomKeyFloat) && zoomKeyFloat > 0)
            _spriteScale = 0.5f / zoomKeyFloat;
    }

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

        if (Background != null)
            context.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (TreeData == null)
            return;

        // Rebuild sprite cache if zoom key changed
        if (_cachedZoomKey != _currentSpriteZoomKey)
            RebuildSpriteCache();

        var operation = new SkillTreeDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            TreeData,
            AllocatedNodeIds ?? new HashSet<int>(),
            (float)ZoomLevel,
            _offsetX,
            _offsetY,
            _cachedSpriteBitmaps,
            _cachedSpriteCoords,
            _spriteScale,
            this);

        context.Custom(operation);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var pointerPos = e.GetCurrentPoint(this).Position;
        var worldBefore = ScreenToWorld(pointerPos);

        var delta = e.Delta.Y;
        var zoomFactor = delta > 0 ? 1.1f : 0.9f;
        var newZoom = (float)ZoomLevel * zoomFactor;
        ZoomLevel = Math.Clamp(newZoom, MinZoom, MaxZoom);

        var worldAfter = ScreenToWorld(pointerPos);
        _offsetX += worldBefore.X - worldAfter.X;
        _offsetY += worldBefore.Y - worldAfter.Y;

        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }

        // Check sprite LOD threshold crossing
        var newZoomKey = GetSpriteZoomKey((float)ZoomLevel);
        if (newZoomKey != _currentSpriteZoomKey && _spriteService != null && TreeData != null)
        {
            _currentSpriteZoomKey = newZoomKey;
            _cachedZoomKey = null; // Force sprite cache rebuild
            _pictureIsDirty = true;

            var treeData = TreeData;
            Task.Run(async () =>
            {
                await _spriteService.PreloadSpriteSheetsAsync(treeData, newZoomKey);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _cachedZoomKey = null;
                    _pictureIsDirty = true;
                    InvalidateVisual();
                });
            });
        }

        // Zoom changed → picture must be rebuilt (signal only, no dispose from UI thread)
        _pictureIsDirty = true;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var currentPoint = e.GetCurrentPoint(this);
        if (currentPoint.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPointerPos = currentPoint.Position;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

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
            if (_hoveredNodeId.HasValue)
            {
                _hoveredNodeId = null;
                ToolTip.SetIsOpen(this, false);
            }

            var currentPos = e.GetCurrentPoint(this).Position;
            var delta = currentPos - _lastPointerPos;

            _offsetX -= (float)(delta.X / ZoomLevel);
            _offsetY -= (float)(delta.Y / ZoomLevel);

            _lastPointerPos = currentPos;
            // Pan only changes offset — picture is still valid, just replay with new offset
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
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

        if (_hoveredNodeId.HasValue)
        {
            _hoveredNodeId = null;
            ToolTip.SetIsOpen(this, false);
        }
    }

    public void ZoomIn()
    {
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        var newZoom = Math.Clamp((float)ZoomLevel * 1.15f, MinZoom, MaxZoom);
        ZoomLevel = newZoom;

        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        _pictureIsDirty = true;
        InvalidateVisual();
    }

    public void ZoomOut()
    {
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        var newZoom = Math.Clamp((float)ZoomLevel * 0.85f, MinZoom, MaxZoom);
        ZoomLevel = newZoom;

        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        _pictureIsDirty = true;
        InvalidateVisual();
    }

    public void SetZoom(double targetZoom)
    {
        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;
        var worldBeforeX = (float)(centerX / ZoomLevel + _offsetX);
        var worldBeforeY = (float)(centerY / ZoomLevel + _offsetY);

        ZoomLevel = Math.Clamp(targetZoom, MinZoom, MaxZoom);

        var worldAfterX = (float)(centerX / ZoomLevel + _offsetX);
        var worldAfterY = (float)(centerY / ZoomLevel + _offsetY);
        _offsetX += worldBeforeX - worldAfterX;
        _offsetY += worldBeforeY - worldAfterY;

        _pictureIsDirty = true;
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

            float radius = GetNodeHitTestRadius(node);
            var dx = worldPos.X - node.CalculatedX.Value;
            var dy = worldPos.Y - node.CalculatedY.Value;

            if (dx * dx + dy * dy <= radius * radius)
                return node.Id;
        }

        return null;
    }

    private float GetNodeHitTestRadius(PassiveNode node)
    {
        // Use sprite-scaled sizes for hit testing
        if (_spriteService != null && TreeData != null)
        {
            string spriteType;
            if (node.IsKeystone) spriteType = "keystoneActive";
            else if (node.IsNotable) spriteType = "notableActive";
            else if (node.IsJewelSocket) spriteType = "jewel";
            else if (node.IsMastery) spriteType = "mastery";
            else spriteType = "normalActive";

            if (TreeData.SpriteSheets.TryGetValue(spriteType, out var zoomDict) &&
                zoomDict.TryGetValue(_currentSpriteZoomKey, out var sheetData))
            {
                string iconKey = node.IsJewelSocket ? "JewelSocketActiveBlue" :
                    (!string.IsNullOrEmpty(node.Icon) ? node.Icon : "");

                if (!string.IsNullOrEmpty(iconKey) &&
                    sheetData.Coords.TryGetValue(iconKey, out var coord))
                {
                    return Math.Max(coord.W, coord.H) / 2f * _spriteScale;
                }
            }
        }

        // Fallback (halved sprite scale)
        if (node.IsKeystone) return 25f;
        if (node.IsNotable) return 18f;
        if (node.IsJewelSocket) return 15f;
        if (node.IsMastery) return 25f;
        return 9f;
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
        var panel = new StackPanel { Spacing = 4 };

        panel.Children.Add(new TextBlock
        {
            Text = node.Name,
            FontWeight = FontWeight.Bold,
            FontSize = 14
        });

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

        // Mastery effects — highlight allocated effect in orange
        if (node.MasteryEffects != null && node.MasteryEffects.Count > 0)
        {
            // Check if this node has an allocated mastery effect
            int allocatedEffectId = 0;
            MasterySelections?.TryGetValue(node.Id, out allocatedEffectId);

            panel.Children.Add(new TextBlock
            {
                Text = "Mastery Effects:",
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 110))
            });

            foreach (var effect in node.MasteryEffects)
            {
                bool isAllocated = allocatedEffectId > 0 && effect.EffectId == allocatedEffectId;
                var color = isAllocated
                    ? Color.FromRgb(200, 150, 50) // Orange for allocated
                    : Color.FromRgb(170, 170, 200); // Blue-gray for unallocated
                var weight = isAllocated ? FontWeight.SemiBold : FontWeight.Normal;

                foreach (var stat in effect.Stats)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"\u2022 {stat}",
                        FontSize = 12,
                        FontWeight = weight,
                        Foreground = new SolidColorBrush(color)
                    });
                }
            }
        }

        if (node.ConnectedNodes != null && node.ConnectedNodes.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Connected to:",
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 8, 0, 0)
            });

            var connectionsToShow = node.ConnectedNodes.Take(15).ToList();
            foreach (var connectedId in connectionsToShow)
            {
                if (TreeData!.Nodes.TryGetValue(connectedId, out var connectedNode))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"\u2022 {connectedNode.Name}",
                        FontSize = 11
                    });
                }
            }

            if (node.ConnectedNodes.Count > 15)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"... and {node.ConnectedNodes.Count - 15} more",
                    FontSize = 11
                });
            }
        }

        return panel;
    }

    public void CenterOnAllocatedNodes()
    {
        if (AllocatedNodeIds == null || AllocatedNodeIds.Count == 0 || TreeData == null)
        {
            CenterOnStartNode();
            return;
        }

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

        if (!minX.HasValue || !maxX.HasValue || !minY.HasValue || !maxY.HasValue)
        {
            CenterOnStartNode();
            return;
        }

        var centerX = (minX.Value + maxX.Value) / 2f;
        var centerY = (minY.Value + maxY.Value) / 2f;

        ZoomLevel = 0.15f;

        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _offsetX = centerX - (float)(Bounds.Width / 2 / ZoomLevel);
            _offsetY = centerY - (float)(Bounds.Height / 2 / ZoomLevel);
            _pictureIsDirty = true;
            InvalidateVisual();
        }
    }

    private void CenterOnStartNode()
    {
        const float treeCenterX = 14000f;
        const float treeCenterY = 11000f;

        ZoomLevel = 0.15f;

        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _offsetX = treeCenterX - (float)(Bounds.Width / 2 / ZoomLevel);
            _offsetY = treeCenterY - (float)(Bounds.Height / 2 / ZoomLevel);
            _pictureIsDirty = true;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Custom draw operation using SKPicture caching.
    /// Records all tree draw commands to an SKPicture once, then replays it each frame.
    /// During pan, only the transform changes — the picture is replayed instantly.
    /// All SKPicture lifecycle management happens on the render thread to avoid race conditions.
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
        private readonly float _spriteScale;
        private readonly SkillTreeCanvas _owner;

        // Orbit constants (match SkillTreePositionHelper)
        private static readonly float[] OrbitRadii = { 0, 82, 162, 335, 493, 662, 846 };
        private static readonly int[] NodesPerOrbit = { 1, 6, 16, 16, 40, 72, 72 };

        // Below this zoom, use colored circles instead of sprites
        private const float SpriteZoomThreshold = 0.10f;

        public SkillTreeDrawOperation(Rect bounds, SkillTreeData treeData, HashSet<int> allocatedNodeIds,
            float zoomLevel, float offsetX, float offsetY,
            Dictionary<string, SKBitmap> spriteBitmaps, Dictionary<string, SpriteSheetData> spriteCoords,
            float spriteScale, SkillTreeCanvas owner)
        {
            _bounds = bounds;
            _treeData = treeData;
            _allocatedNodeIds = allocatedNodeIds;
            _zoomLevel = zoomLevel;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _spriteBitmaps = spriteBitmaps;
            _spriteCoords = spriteCoords;
            _spriteScale = spriteScale;
            _owner = owner;
        }

        public Rect Bounds => _bounds;
        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            // Check if we need to rebuild the cached picture
            // All checks happen on render thread — no race conditions
            bool needsRebuild = _owner._pictureIsDirty
                || _owner._cachedPicture == null
                || _owner._pictureZoom != _zoomLevel
                || _owner._pictureAllocCount != _allocatedNodeIds.Count
                || _owner._pictureSpriteCount != _spriteBitmaps.Count
                || _owner._pictureZoomKey != _owner._currentSpriteZoomKey;

            if (needsRebuild)
            {
                // Dispose old picture on render thread (safe — we're the only consumer)
                _owner._cachedPicture?.Dispose();
                _owner._cachedPicture = null;

                // Record all draw commands into an SKPicture in world space
                var recordBounds = SKRect.Create(-2000, -2000, 32000, 28000);
                using var recorder = new SKPictureRecorder();
                var recordCanvas = recorder.BeginRecording(recordBounds);

                bool useSprites = _zoomLevel >= SpriteZoomThreshold && _spriteBitmaps.Count > 0;

                // Shared paint for filtered bitmap scaling (avoids pixelation)
                using var bitmapPaint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.Medium,
                    IsAntialias = true
                };

                if (useSprites)
                    DrawGroupBackgrounds(recordCanvas, bitmapPaint);
                DrawConnections(recordCanvas);
                DrawNodes(recordCanvas, useSprites, bitmapPaint);

                _owner._cachedPicture = recorder.EndRecording();
                _owner._pictureZoom = _zoomLevel;
                _owner._pictureAllocCount = _allocatedNodeIds.Count;
                _owner._pictureSpriteCount = _spriteBitmaps.Count;
                _owner._pictureZoomKey = _owner._currentSpriteZoomKey;
                _owner._pictureIsDirty = false;
            }

            // Replay the cached picture with current pan offset + zoom
            if (_owner._cachedPicture != null)
            {
                canvas.Save();
                canvas.Translate(-_offsetX * _zoomLevel, -_offsetY * _zoomLevel);
                canvas.Scale(_zoomLevel, _zoomLevel);
                canvas.DrawPicture(_owner._cachedPicture);
                canvas.Restore();
            }
        }

        private void DrawGroupBackgrounds(SKCanvas canvas, SKPaint bitmapPaint)
        {
            if (!_spriteCoords.TryGetValue("groupBackground", out var bgCoords) ||
                !_spriteBitmaps.TryGetValue("groupBackground", out var bgBitmap))
                return;

            foreach (var group in _treeData.Groups.Values)
            {
                if (group.Background == null || string.IsNullOrEmpty(group.Background.Image))
                    continue;

                if (!bgCoords.Coords.TryGetValue(group.Background.Image, out var coord))
                    continue;

                var halfW = coord.W / 2f * _spriteScale;
                var halfH = coord.H / 2f * _spriteScale;
                var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);
                var destRect = new SKRect(group.X - halfW, group.Y - halfH, group.X + halfW, group.Y + halfH);

                canvas.DrawBitmap(bgBitmap, srcRect, destRect, bitmapPaint);

                if (group.Background.IsHalfImage)
                {
                    canvas.Save();
                    canvas.Translate(group.X, group.Y);
                    canvas.Scale(-1, 1);
                    canvas.Translate(-group.X, -group.Y);
                    canvas.DrawBitmap(bgBitmap, srcRect, destRect, bitmapPaint);
                    canvas.Restore();
                }
            }
        }

        private void DrawConnections(SKCanvas canvas)
        {
            using var unallocatedPaint = new SKPaint
            {
                Color = new SKColor(80, 80, 80),
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            using var allocatedPaint = new SKPaint
            {
                Color = new SKColor(200, 150, 50),
                StrokeWidth = 3,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

            using var unallocatedPath = new SKPath();
            using var allocatedPath = new SKPath();

            var drawnConnections = new HashSet<long>();

            foreach (var node in _treeData.Nodes.Values)
            {
                if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
                    continue;
                if (node.IsAscendancy)
                    continue;

                var startX = node.CalculatedX.Value;
                var startY = node.CalculatedY.Value;
                var nodeIsAllocated = _allocatedNodeIds.Contains(node.Id);

                foreach (var connectedId in node.ConnectedNodes)
                {
                    var a = node.Id < connectedId ? node.Id : connectedId;
                    var b = node.Id < connectedId ? connectedId : node.Id;
                    var key = ((long)a << 32) | (uint)b;

                    if (!drawnConnections.Add(key))
                        continue;

                    if (!_treeData.Nodes.TryGetValue(connectedId, out var connectedNode))
                        continue;
                    if (!connectedNode.CalculatedX.HasValue || !connectedNode.CalculatedY.HasValue)
                        continue;
                    if (connectedNode.IsAscendancy)
                        continue;

                    var endX = connectedNode.CalculatedX.Value;
                    var endY = connectedNode.CalculatedY.Value;

                    var path = (nodeIsAllocated && _allocatedNodeIds.Contains(connectedId))
                        ? allocatedPath
                        : unallocatedPath;

                    // Arc connections for nodes on same group + orbit
                    if (node.Group.HasValue && connectedNode.Group.HasValue &&
                        node.Group.Value == connectedNode.Group.Value &&
                        node.Orbit.HasValue && connectedNode.Orbit.HasValue &&
                        node.Orbit.Value == connectedNode.Orbit.Value &&
                        node.Orbit.Value > 0 && node.Orbit.Value < OrbitRadii.Length)
                    {
                        AddArcConnection(path, node, connectedNode);
                    }
                    else
                    {
                        path.MoveTo(startX, startY);
                        path.LineTo(endX, endY);
                    }
                }
            }

            canvas.DrawPath(unallocatedPath, unallocatedPaint);
            canvas.DrawPath(allocatedPath, allocatedPaint);
        }

        private void AddArcConnection(SKPath path, PassiveNode nodeA, PassiveNode nodeB)
        {
            var orbit = nodeA.Orbit!.Value;
            var radius = OrbitRadii[orbit];
            var nodesInOrbit = NodesPerOrbit[orbit];
            var groupId = nodeA.Group!.Value;

            if (!_treeData.Groups.TryGetValue(groupId, out var group))
            {
                path.MoveTo(nodeA.CalculatedX!.Value, nodeA.CalculatedY!.Value);
                path.LineTo(nodeB.CalculatedX!.Value, nodeB.CalculatedY!.Value);
                return;
            }

            var angleA = (360.0f * nodeA.OrbitIndex!.Value / nodesInOrbit) - 90f;
            var angleB = (360.0f * nodeB.OrbitIndex!.Value / nodesInOrbit) - 90f;

            var sweep = angleB - angleA;
            while (sweep > 180f) sweep -= 360f;
            while (sweep < -180f) sweep += 360f;

            var oval = new SKRect(
                group.X - radius, group.Y - radius,
                group.X + radius, group.Y + radius);

            path.ArcTo(oval, angleA, sweep, true);
        }

        private void DrawNodes(SKCanvas canvas, bool useSprites, SKPaint? bitmapPaint = null)
        {
            using var allocatedPaint = new SKPaint
            {
                Color = new SKColor(200, 150, 50),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var unallocatedPaint = new SKPaint
            {
                Color = new SKColor(60, 60, 60),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var masteryPaint = new SKPaint
            {
                Color = new SKColor(100, 80, 120),
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

                if (useSprites)
                {
                    if (node.IsMastery)
                    {
                        if (TryDrawMasterySprite(canvas, node, x, y, isAllocated, bitmapPaint))
                            continue;
                    }
                    else if (TryDrawNodeSprite(canvas, node, x, y, isAllocated, bitmapPaint))
                    {
                        TryDrawNodeFrame(canvas, node, x, y, isAllocated, bitmapPaint);
                        continue;
                    }
                }

                // Fallback: colored circles (sizes match halved sprite scale)
                float radius;
                if (node.IsKeystone) radius = 25f;
                else if (node.IsNotable) radius = 18f;
                else if (node.IsJewelSocket) radius = 15f;
                else if (node.IsMastery) radius = 25f;
                else radius = 9f;

                var paint = node.IsMastery ? masteryPaint : (isAllocated ? allocatedPaint : unallocatedPaint);
                canvas.DrawCircle(x, y, radius, paint);
            }
        }

        /// <summary>
        /// Draws mastery nodes: colored circle background for state + icon from "mastery" sprite type.
        /// </summary>
        private bool TryDrawMasterySprite(SKCanvas canvas, PassiveNode node, float x, float y, bool isAllocated, SKPaint? bitmapPaint)
        {
            if (string.IsNullOrEmpty(node.Icon))
                return false;

            if (!_spriteCoords.TryGetValue("mastery", out var iconSheet) ||
                !_spriteBitmaps.TryGetValue("mastery", out var iconBitmap) ||
                !iconSheet.Coords.TryGetValue(node.Icon, out var iconCoord))
                return false;

            var halfW = iconCoord.W / 2f * _spriteScale;
            var halfH = iconCoord.H / 2f * _spriteScale;
            float bgRadius = Math.Max(halfW, halfH) + 8f;

            SKColor bgColor;
            if (isAllocated)
            {
                bgColor = new SKColor(180, 140, 50, 180);
            }
            else
            {
                bool hasAllocatedNeighbor = false;
                foreach (var connId in node.ConnectedNodes)
                {
                    if (_allocatedNodeIds.Contains(connId))
                    {
                        hasAllocatedNeighbor = true;
                        break;
                    }
                }
                bgColor = hasAllocatedNeighbor
                    ? new SKColor(120, 100, 60, 140)
                    : new SKColor(50, 50, 60, 120);
            }

            using var bgPaintLocal = new SKPaint
            {
                Color = bgColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(x, y, bgRadius, bgPaintLocal);

            var srcRect = new SKRect(iconCoord.X, iconCoord.Y, iconCoord.X + iconCoord.W, iconCoord.Y + iconCoord.H);
            var destRect = new SKRect(x - halfW, y - halfH, x + halfW, y + halfH);
            canvas.DrawBitmap(iconBitmap, srcRect, destRect, bitmapPaint);

            return true;
        }

        private bool TryDrawNodeSprite(SKCanvas canvas, PassiveNode node, float x, float y, bool isAllocated, SKPaint? bitmapPaint)
        {
            string spriteType;
            if (node.IsKeystone)
                spriteType = isAllocated ? "keystoneActive" : "keystoneInactive";
            else if (node.IsNotable)
                spriteType = isAllocated ? "notableActive" : "notableInactive";
            else if (node.IsJewelSocket)
                spriteType = "jewel";
            else
                spriteType = isAllocated ? "normalActive" : "normalInactive";

            if (!_spriteCoords.TryGetValue(spriteType, out var sheetData) ||
                !_spriteBitmaps.TryGetValue(spriteType, out var bitmap))
                return false;

            string iconKey;
            if (node.IsJewelSocket)
                iconKey = isAllocated ? "JewelSocketActiveBlue" : "JewelSocketNormal";
            else if (string.IsNullOrEmpty(node.Icon))
                return false;
            else
                iconKey = node.Icon;

            if (!sheetData.Coords.TryGetValue(iconKey, out var coord))
                return false;

            var halfW = coord.W / 2f * _spriteScale;
            var halfH = coord.H / 2f * _spriteScale;
            var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);
            var destRect = new SKRect(x - halfW, y - halfH, x + halfW, y + halfH);

            canvas.DrawBitmap(bitmap, srcRect, destRect, bitmapPaint);
            return true;
        }

        private void TryDrawNodeFrame(SKCanvas canvas, PassiveNode node, float x, float y, bool isAllocated, SKPaint? bitmapPaint)
        {
            if (!_spriteCoords.TryGetValue("frame", out var frameData) ||
                !_spriteBitmaps.TryGetValue("frame", out var frameBitmap))
                return;

            string frameKey;
            if (node.IsKeystone)
                frameKey = isAllocated ? "KeystoneFrameAllocated" : "KeystoneFrameUnallocated";
            else if (node.IsNotable)
                frameKey = isAllocated ? "NotableFrameAllocated" : "NotableFrameUnallocated";
            else if (node.IsJewelSocket)
                return;
            else
                frameKey = isAllocated ? "PSSkillFrameActive" : "PSSkillFrame";

            if (!frameData.Coords.TryGetValue(frameKey, out var coord))
                return;

            var halfW = coord.W / 2f * _spriteScale;
            var halfH = coord.H / 2f * _spriteScale;
            var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);
            var destRect = new SKRect(x - halfW, y - halfH, x + halfW, y + halfH);

            canvas.DrawBitmap(frameBitmap, srcRect, destRect, bitmapPaint);
        }
    }
}
