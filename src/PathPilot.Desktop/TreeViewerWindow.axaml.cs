using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PathPilot.Core.Models;
using PathPilot.Core.Services;
using PathPilot.Desktop.Controls;

namespace PathPilot.Desktop;

public partial class TreeViewerWindow : Window
{
    private readonly SkillTreeDataService _treeDataService;
    private HashSet<int> _allocatedNodeIds = new();
    private string _treeUrl = string.Empty;

    public TreeViewerWindow()
    {
        InitializeComponent();
        _treeDataService = new SkillTreeDataService();
    }

    public TreeViewerWindow(string treeUrl, string title, List<int>? allocatedNodes = null) : this()
    {
        _treeUrl = treeUrl;
        TreeTitleText.Text = title;
        _allocatedNodeIds = allocatedNodes != null ? new HashSet<int>(allocatedNodes) : new();

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

            // Apply offset to center tree (GGG coords can be negative)
            // Offset by 5000 to center in 10000x10000 canvas
            foreach (var node in treeData.Nodes.Values)
            {
                if (node.CalculatedX.HasValue)
                    node.CalculatedX += 5000;
                if (node.CalculatedY.HasValue)
                    node.CalculatedY += 5000;
            }

            // Set data on canvas
            TreeCanvas.TreeData = treeData;
            TreeCanvas.AllocatedNodeIds = _allocatedNodeIds;

            Console.WriteLine($"Tree loaded: {treeData.TotalNodes} nodes, {_allocatedNodeIds.Count} allocated");
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
}
