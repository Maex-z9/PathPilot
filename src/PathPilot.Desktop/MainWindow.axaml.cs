using Avalonia.Controls;
using Avalonia.Interactivity;
using PathPilot.Core.Models;
using PathPilot.Core.Parsers;
using System;
using System.Linq;
using PathPilot.Core.Services;

namespace PathPilot.Desktop;

public partial class MainWindow : Window
{
    private Build? _currentBuild;
    private readonly GemDataService _gemDataService = new();

    public MainWindow()
    {
        InitializeComponent();
        _gemDataService = new GemDataService();
        
        // Set default text
        if (BuildNameText != null)
        {
            BuildNameText.Text = "No build loaded";
        }
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        // Create import dialog
        var dialog = new ImportDialog();
        var result = await dialog.ShowDialog<string?>(this);

        if (string.IsNullOrWhiteSpace(result))
        {
            return; // User cancelled
        }

        try
        {
            // Show loading indicator
            BuildNameText.Text = "Loading...";

            Build build;

            // Check if it's a URL or paste code
            if (result.StartsWith("http://") || result.StartsWith("https://"))
            {
                // Import from URL
                var importer = new PobUrlImporter();
                var xml = await importer.ImportFromUrlAsync(result);
                
                var parser = new PobXmlParser();
                build = parser.Parse(xml);
            }
            else
            {
                // Parse from paste code
                var parser = new PobXmlParser();
                build = parser.ParseFromPasteCode(result);
            }

            // Store and display the build
            _currentBuild = build;
            DisplayBuild(build);

            // Hide welcome screen, show build display
            WelcomeScreen.IsVisible = false;
            BuildDisplay.IsVisible = true;
        }
        catch (Exception ex)
        {
            // Show error dialog
            await ShowErrorDialog("Import Failed", $"Failed to import build:\n\n{ex.Message}");
            BuildNameText.Text = "No build loaded";
        }
    }

    private void DisplayBuild(Build build)
    {
        // Update header
        BuildNameText.Text = build.Name;
        BuildTitleText.Text = build.Name;
        BuildSubtitleText.Text = $"{build.ClassName} - {build.Ascendancy}";

        // Update stats
        LevelText.Text = build.Level.ToString();
        PassivePointsText.Text = build.SkillTree?.PointsUsed.ToString() ?? "0";
        AscendancyPointsText.Text = build.SkillTree?.AscendancyPointsUsed.ToString() ?? "0";

        // Populate Skill Set ComboBox
        SkillSetComboBox.ItemsSource = build.SkillSets.Select((s, i) => $"{i + 1}. {s.Name}").ToList();
        SkillSetComboBox.SelectedIndex = build.ActiveSkillSetIndex;

        // Populate Item Set ComboBox
        ItemSetComboBox.ItemsSource = build.ItemSets.Select((s, i) => $"{i + 1}. {s.Name}").ToList();
        ItemSetComboBox.SelectedIndex = build.ActiveItemSetIndex;

        // Show loadout selector if there are multiple sets
        LoadoutSelector.IsVisible = build.SkillSets.Count > 1 || build.ItemSets.Count > 1;

        // Display current loadout
        UpdateDisplayedLoadout();

        // Display skill tree info
        if (build.SkillTree != null)
        {
            var tree = build.SkillTree;
            SkillTreeInfoText.Text = $"Allocated {tree.AllocatedNodes.Count} passive nodes\n" +
                                     $"Class: {tree.ClassName}\n" +
                                     $"Ascendancy: {tree.Ascendancy}\n" +
                                     $"Ascendancy Points: {tree.AscendancyPointsUsed}/8\n" +
                                     $"Masteries: {tree.MasterySelections.Count}";
        }
    }

private void UpdateDisplayedLoadout()
{
    if (_currentBuild == null) return;

    // Enrich gems with acquisition info AND color
    var enrichedGems = _currentBuild.Gems
        .Where(g => g.IsEnabled)
        .Select(g =>
        {
            var gemInfo = _gemDataService.GetGemInfo(g.Name);
            if (gemInfo != null)
            {
                var source = gemInfo.Sources.OrderBy(s => s.Act).FirstOrDefault();
                if (source != null)
                {
                    g.AcquisitionInfo = $"Act {source.Act}: {source.VendorName ?? source.QuestName ?? "Available"}";
                }
                
                // Set color from database
                g.Color = gemInfo.Color switch
                {
                    "Red" => SocketColor.Red,
                    "Green" => SocketColor.Green,
                    "Blue" => SocketColor.Blue,
                    _ => SocketColor.White
                };
            }
            else
            {
                g.AcquisitionInfo = "Unknown source";
                g.Color = SocketColor.White;
            }
            return g;
        })
        .OrderBy(g => g.LinkGroup)
        .ThenBy(g => g.Type == GemType.Support ? 1 : 0)
        .ToList();

    GemsListBox.ItemsSource = enrichedGems;

    // Display items from active item set
    ItemsListBox.ItemsSource = _currentBuild.Items
        .OrderBy(i => i.Slot)
        .ToList();
}

    private void SkillSetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentBuild != null && SkillSetComboBox.SelectedIndex >= 0)
        {
            _currentBuild.ActiveSkillSetIndex = SkillSetComboBox.SelectedIndex;
            UpdateDisplayedLoadout();
        }
    }

    private void ItemSetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentBuild != null && ItemSetComboBox.SelectedIndex >= 0)
        {
            _currentBuild.ActiveItemSetIndex = ItemSetComboBox.SelectedIndex;
            UpdateDisplayedLoadout();
        }
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
    {
        var errorDialog = new Window
        {
            Title = title,
            Width = 450,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Spacing = 15,
            Margin = new Avalonia.Thickness(20)
        };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Avalonia.Media.FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Padding = new Avalonia.Thickness(20, 10),
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        okButton.Click += (s, e) => errorDialog.Close();
        panel.Children.Add(okButton);

        errorDialog.Content = panel;

        await errorDialog.ShowDialog(this);
    }
}
