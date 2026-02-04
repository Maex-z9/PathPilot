using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PathPilot.Core.Models;
using PathPilot.Core.Parsers;
using PathPilot.Core.Services;
using PathPilot.Desktop.Controls;

namespace PathPilot.Desktop;

public partial class TreeViewerWindow : Window
{
    private readonly SkillTreeDataService _treeDataService;
    private HashSet<int> _allocatedNodeIds = new();
    private string _treeUrl = string.Empty;
    private double _zoomLevel = 0.4; // Reasonable starting zoom
    private const double ZoomStep = 0.05;
    private const double MinZoom = 0.02;
    private const double MaxZoom = 2.0;

    public TreeViewerWindow()
    {
        InitializeComponent();
        _treeDataService = new SkillTreeDataService();
    }

    public TreeViewerWindow(string treeUrl, string title, List<int>? allocatedNodes = null) : this()
    {
        _treeUrl = treeUrl;
        TreeTitleText.Text = title;

        // Always decode tree URL directly to ensure correct node IDs
        // (Stored allocatedNodes may have been decoded with buggy older code)
        if (!string.IsNullOrEmpty(treeUrl))
        {
            var decodedNodes = TreeUrlDecoder.DecodeAllocatedNodes(treeUrl);
            _allocatedNodeIds = new HashSet<int>(decodedNodes);
            Console.WriteLine($"TreeViewer decoded {decodedNodes.Count} nodes from URL");
        }
        else
        {
            _allocatedNodeIds = allocatedNodes != null ? new HashSet<int>(allocatedNodes) : new();
        }

        // Load tree when window opens
        Opened += OnWindowOpened;
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        await LoadTreeAsync();
    }

    private async Task LoadTreeAsync()
    {
        try
        {
            var treeData = await _treeDataService.GetTreeDataAsync();
            if (treeData == null)
            {
                Console.WriteLine("Failed to load tree data");
                return;
            }

            // Calculate positions for all nodes
            SkillTreePositionHelper.CalculateAllPositions(treeData);

            // Apply offset to shift tree into positive coordinate space
            // GGG tree bounds: X from -13902 to +12430, Y from -10689 to +10023
            // Add offset to shift minimum to ~100 (padding)
            const float offsetX = 14000f;  // Shifts -13902 to ~100
            const float offsetY = 11000f;  // Shifts -10689 to ~311

            foreach (var node in treeData.Nodes.Values)
            {
                if (node.CalculatedX.HasValue)
                    node.CalculatedX += offsetX;
                if (node.CalculatedY.HasValue)
                    node.CalculatedY += offsetY;
            }

            // Debug: Count nodes with calculated positions
            var nodesWithPos = treeData.Nodes.Values.Count(n => n.CalculatedX.HasValue && n.CalculatedY.HasValue);
            Console.WriteLine($"Nodes with calculated positions: {nodesWithPos}/{treeData.TotalNodes}");

            // Debug: Sample position range
            var posNodes = treeData.Nodes.Values.Where(n => n.CalculatedX.HasValue).ToList();
            if (posNodes.Any())
            {
                var minX = posNodes.Min(n => n.CalculatedX!.Value);
                var maxX = posNodes.Max(n => n.CalculatedX!.Value);
                var minY = posNodes.Min(n => n.CalculatedY!.Value);
                var maxY = posNodes.Max(n => n.CalculatedY!.Value);
                Console.WriteLine($"Position range: X={minX:F0} to {maxX:F0}, Y={minY:F0} to {maxY:F0}");
            }

            // Set data on canvas
            TreeCanvas.TreeData = treeData;
            TreeCanvas.AllocatedNodeIds = _allocatedNodeIds;

            Console.WriteLine($"Tree loaded: {treeData.TotalNodes} nodes, {_allocatedNodeIds.Count} allocated");
            Console.WriteLine($"Allocated node IDs (first 10): {string.Join(", ", _allocatedNodeIds.Take(10))}");

            // Debug: Check how many allocated nodes exist in tree data
            var foundInTree = _allocatedNodeIds.Count(id => treeData.Nodes.ContainsKey(id));
            var notFoundIds = _allocatedNodeIds.Where(id => !treeData.Nodes.ContainsKey(id)).Take(5);
            Console.WriteLine($"Allocated nodes found in tree: {foundInTree}/{_allocatedNodeIds.Count}");
            if (notFoundIds.Any())
                Console.WriteLine($"Not found IDs (first 5): {string.Join(", ", notFoundIds)}");

            // Apply initial zoom
            ApplyZoom();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tree: {ex.Message}");
        }
    }

    private async void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        await LoadTreeAsync();
    }

    private void ZoomInButton_Click(object? sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Min(MaxZoom, _zoomLevel + ZoomStep);
        ApplyZoom();
    }

    private void ZoomOutButton_Click(object? sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Max(MinZoom, _zoomLevel - ZoomStep);
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        // Scale by adjusting canvas size and passing zoom level to the canvas for SkiaSharp rendering
        const double baseWidth = 28000;
        const double baseHeight = 22000;

        TreeCanvas.Width = baseWidth * _zoomLevel;
        TreeCanvas.Height = baseHeight * _zoomLevel;
        TreeCanvas.ZoomLevel = _zoomLevel;

        ZoomLevelText.Text = $"{_zoomLevel * 100:F0}%";
    }
}
