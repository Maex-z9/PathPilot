using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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
    private readonly SkillTreeSpriteService _spriteService;
    private HashSet<int> _allocatedNodeIds = new();
    private Dictionary<int, int> _masterySelections = new();
    private string _treeUrl = string.Empty;
    private double _zoomLevel = 0.15; // Match CenterOnAllocatedNodes default
    private const double ZoomStep = 0.05;
    private const double MinZoom = 0.02;
    private const double MaxZoom = 2.0;
    private Build? _build;
    private bool _suppressSelectionChanged;

    // Search state
    private DispatcherTimer? _searchDebounceTimer;
    private List<(int NodeId, string Display, string Name)> _searchResults = new();
    private SkillTreeData? _loadedTreeData;

    public TreeViewerWindow()
    {
        InitializeComponent();
        _treeDataService = new SkillTreeDataService();
        _spriteService = new SkillTreeSpriteService();

        // Dispose sprite service when window closes
        Closed += (s, e) => _spriteService.Dispose();
    }

    public TreeViewerWindow(string treeUrl, string title, List<int>? allocatedNodes = null) : this()
    {
        _treeUrl = treeUrl;
        TreeTitleText.Text = title;

        // Always decode tree URL directly to ensure correct node IDs
        // (Stored allocatedNodes may have been decoded with buggy older code)
        if (!string.IsNullOrEmpty(treeUrl))
        {
            var decoded = TreeUrlDecoder.DecodeTreeUrl(treeUrl);
            _allocatedNodeIds = new HashSet<int>(decoded.AllocatedNodes);
            _masterySelections = decoded.MasterySelections;
            Console.WriteLine($"TreeViewer decoded {decoded.AllocatedNodes.Count} nodes, {decoded.MasterySelections.Count} mastery selections from URL");
        }
        else
        {
            _allocatedNodeIds = allocatedNodes != null ? new HashSet<int>(allocatedNodes) : new();
        }

        // Load tree when window opens
        Opened += OnWindowOpened;
    }

    public TreeViewerWindow(Build build) : this()
    {
        _build = build;

        var activeTreeSet = build.ActiveTreeSet;
        if (activeTreeSet != null && !string.IsNullOrEmpty(activeTreeSet.TreeUrl))
        {
            _treeUrl = activeTreeSet.TreeUrl;
            TreeTitleText.Text = $"Skill Tree - {activeTreeSet.Title}";

            var decoded = TreeUrlDecoder.DecodeTreeUrl(activeTreeSet.TreeUrl);
            _allocatedNodeIds = new HashSet<int>(decoded.AllocatedNodes);
            _masterySelections = decoded.MasterySelections;
            Console.WriteLine($"TreeViewer decoded {decoded.AllocatedNodes.Count} nodes, {decoded.MasterySelections.Count} mastery selections from URL");
        }

        // Populate ComboBox with tree set names
        if (build.TreeSets.Count > 1)
        {
            _suppressSelectionChanged = true;
            TreeSetSelector.ItemsSource = build.TreeSets.Select(t => t.Title).ToList();
            TreeSetSelector.SelectedIndex = build.ActiveTreeSetIndex;
            TreeSetSelector.IsVisible = true;
            _suppressSelectionChanged = false;
        }

        // Load tree when window opens
        Opened += OnWindowOpened;
    }

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        await LoadTreeAsync();
    }

    private void TreeSetSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged || _build == null || TreeSetSelector.SelectedIndex < 0)
            return;

        var selectedIndex = TreeSetSelector.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _build.TreeSets.Count)
        {
            SwitchToTreeSet(_build.TreeSets[selectedIndex]);
        }
    }

    private void SwitchToTreeSet(SkillTreeSet treeSet)
    {
        if (string.IsNullOrEmpty(treeSet.TreeUrl))
            return;

        ClearSearch();

        _treeUrl = treeSet.TreeUrl;
        TreeTitleText.Text = $"Skill Tree - {treeSet.Title}";

        var decoded = TreeUrlDecoder.DecodeTreeUrl(treeSet.TreeUrl);
        _allocatedNodeIds = new HashSet<int>(decoded.AllocatedNodes);
        _masterySelections = decoded.MasterySelections;

        Console.WriteLine($"Switched to tree set '{treeSet.Title}': {decoded.AllocatedNodes.Count} nodes, {decoded.MasterySelections.Count} mastery selections");

        // Update canvas with new data (tree data is already loaded, just update allocated nodes)
        TreeCanvas.AllocatedNodeIds = _allocatedNodeIds;
        TreeCanvas.MasterySelections = _masterySelections;

        // Re-center on the new allocated nodes
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TreeCanvas.CenterOnAllocatedNodes();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
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

            // Apply same offset to group positions for background rendering
            foreach (var group in treeData.Groups.Values)
            {
                group.X += offsetX;
                group.Y += offsetY;
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

            // Determine initial zoom key for sprite LOD
            var initialZoomKey = GetSpriteZoomKey((float)_zoomLevel);
            Console.WriteLine($"Initial sprite zoom key: {initialZoomKey}");

            // Preload sprite sheets for initial zoom level BEFORE setting tree data on canvas.
            // This ensures sprites are in memory before the first render frame, eliminating the
            // black flash that occurred when sprites loaded asynchronously after canvas display.
            await _spriteService.PreloadSpriteSheetsAsync(treeData, initialZoomKey);

            // Verify sprite preload succeeded -- check that key sprite sheets are actually loaded
            var preloadedCount = 0;
            var expectedCount = 0;
            foreach (var (spriteType, zoomDict) in treeData.SpriteSheets)
            {
                if (zoomDict.TryGetValue(initialZoomKey, out var sd) && !string.IsNullOrEmpty(sd.Filename))
                {
                    expectedCount++;
                    if (_spriteService.TryGetLoadedBitmap(sd.Filename) != null)
                        preloadedCount++;
                }
            }
            Console.WriteLine($"Sprite preload verification: {preloadedCount}/{expectedCount} sheets loaded for zoom {initialZoomKey}");

            // Debug: Check mastery sprite availability
            var masteryTypes = new[] { "mastery", "masteryInactive", "masteryConnected", "masteryActiveSelected" };
            foreach (var mt in masteryTypes)
            {
                if (treeData.SpriteSheets.TryGetValue(mt, out var zd) && zd.TryGetValue(initialZoomKey, out var sd))
                {
                    var bitmap = _spriteService.TryGetLoadedBitmap(sd.Filename);
                    Console.WriteLine($"Mastery sprite '{mt}': {sd.Coords.Count} coords, filename={sd.Filename}, loaded={bitmap != null}");
                }
                else
                {
                    Console.WriteLine($"Mastery sprite '{mt}': NOT FOUND in SpriteSheets for zoom {initialZoomKey}");
                }
            }

            // Store reference for search
            _loadedTreeData = treeData;

            // Set data on canvas -- sprites are guaranteed loaded at this point
            TreeCanvas.TreeData = treeData;
            TreeCanvas.AllocatedNodeIds = _allocatedNodeIds;
            TreeCanvas.MasterySelections = _masterySelections;
            TreeCanvas.SetSpriteService(_spriteService);

            Console.WriteLine($"Tree loaded: {treeData.TotalNodes} nodes, {_allocatedNodeIds.Count} allocated");
            Console.WriteLine($"Allocated node IDs (first 10): {string.Join(", ", _allocatedNodeIds.Take(10))}");

            // Debug: Check how many allocated nodes exist in tree data
            var foundInTree = _allocatedNodeIds.Count(id => treeData.Nodes.ContainsKey(id));
            var notFoundIds = _allocatedNodeIds.Where(id => !treeData.Nodes.ContainsKey(id)).Take(5);
            Console.WriteLine($"Allocated nodes found in tree: {foundInTree}/{_allocatedNodeIds.Count}");
            if (notFoundIds.Any())
                Console.WriteLine($"Not found IDs (first 5): {string.Join(", ", notFoundIds)}");

            // Set initial zoom level
            TreeCanvas.ZoomLevel = _zoomLevel;
            UpdateZoomDisplay();

            // Center on allocated nodes after layout completes
            // Use Dispatcher to ensure bounds are calculated
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                TreeCanvas.CenterOnAllocatedNodes();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
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
        TreeCanvas.ZoomIn();
        UpdateZoomDisplay();
    }

    private void ZoomOutButton_Click(object? sender, RoutedEventArgs e)
    {
        TreeCanvas.ZoomOut();
        UpdateZoomDisplay();
    }

    private void Zoom26Button_Click(object? sender, RoutedEventArgs e)
    {
        TreeCanvas.SetZoom(0.26);
        UpdateZoomDisplay();
    }

    private void UpdateZoomDisplay()
    {
        ZoomLevelText.Text = $"{TreeCanvas.ZoomLevel * 100:F0}%";
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

    // --- Node Search ---

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer?.Stop();
        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            PerformSearch();
        };
        _searchDebounceTimer.Start();
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ClearSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (_searchResults.Count > 0)
            {
                NavigateToSearchResult(0);
                SearchPopup.IsOpen = false;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down && SearchPopup.IsOpen)
        {
            SearchResultsList.Focus();
            if (SearchResultsList.ItemCount > 0)
                SearchResultsList.SelectedIndex = 0;
            e.Handled = true;
        }
    }

    private void PerformSearch()
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query) || _loadedTreeData == null)
        {
            SearchPopup.IsOpen = false;
            TreeCanvas.HighlightedNodeIds = null;
            TreeCanvas.ClearFocusedNode();
            _searchResults.Clear();
            return;
        }

        var matches = _loadedTreeData.Nodes.Values
            .Where(n => !n.IsClassStart
                && !string.IsNullOrEmpty(n.Name)
                && n.CalculatedX.HasValue
                && n.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(n => n.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(n => n.Name.Length)
            .Take(20)
            .Select(n =>
            {
                string type = n.IsKeystone ? "Keystone"
                    : n.IsNotable ? "Notable"
                    : n.IsJewelSocket ? "Jewel Socket"
                    : n.IsMastery ? "Mastery"
                    : "Passive";
                return (n.Id, Display: $"{n.Name}  [{type}]", n.Name);
            })
            .ToList();

        _searchResults = matches;
        SearchResultsList.ItemsSource = matches.Select(m => m.Display).ToList();

        if (matches.Count > 0)
        {
            SearchPopup.IsOpen = true;
            TreeCanvas.HighlightedNodeIds = new HashSet<int>(matches.Select(m => m.Id));
        }
        else
        {
            SearchPopup.IsOpen = false;
            TreeCanvas.HighlightedNodeIds = null;
        }

        TreeCanvas.ClearFocusedNode();
    }

    private void SearchResultsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var index = SearchResultsList.SelectedIndex;
        if (index >= 0 && index < _searchResults.Count)
        {
            NavigateToSearchResult(index);
            SearchPopup.IsOpen = false;
        }
    }

    private void NavigateToSearchResult(int index)
    {
        if (index < 0 || index >= _searchResults.Count)
            return;

        var nodeId = _searchResults[index].NodeId;
        TreeCanvas.NavigateToNode(nodeId);
        UpdateZoomDisplay();
    }

    private void ClearSearch()
    {
        _searchDebounceTimer?.Stop();
        SearchBox.Text = string.Empty;
        SearchPopup.IsOpen = false;
        _searchResults.Clear();
        TreeCanvas.HighlightedNodeIds = null;
        TreeCanvas.ClearFocusedNode();
    }
}
