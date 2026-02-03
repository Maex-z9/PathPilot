using Avalonia.Controls;
using Avalonia.Interactivity;
using PathPilot.Core.Models;
using PathPilot.Core.Parsers;
using PathPilot.Core.Services;
using System;
using System.Linq;

namespace PathPilot.Desktop;

public partial class MainWindow : Window
{
    private Build? _currentBuild;
    private readonly GemDataService _gemDataService;

    public MainWindow()
    {
        InitializeComponent();
        
        // Initialize gem data service
        _gemDataService = new GemDataService();
        _gemDataService.LoadDatabase();
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        // Create input dialog
        var dialog = new Window
        {
            Title = "Import Path of Building",
            Width = 600,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        
        var textBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Height = 250,
            Watermark = "Paste your Path of Building code here..."
        };

        var buttonPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 10, 0, 0),
            Spacing = 10
        };

        var importBtn = new Button { Content = "Import" };
        var cancelBtn = new Button { Content = "Cancel" };

        importBtn.Click += async (s, ev) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                try
                {
                    string input = textBox.Text.Trim();
                    
                    Console.WriteLine($"Input length: {input.Length}");
                    
                    // Check if it's a URL or direct paste code
                    if (input.StartsWith("http://") || input.StartsWith("https://"))
                    {
                        Console.WriteLine("Detected as URL");
                        var importer = new PobUrlImporter();
                        var xmlContent = await importer.ImportFromUrlAsync(input);
                        var parser = new PobXmlParser(_gemDataService);
                        _currentBuild = parser.Parse(xmlContent);
                    }
                    else
                    {
                        Console.WriteLine("Detected as paste code");
                        var xmlContent = PobDecoder.DecodeToXml(input);
                        var parser = new PobXmlParser(_gemDataService);
                        _currentBuild = parser.Parse(xmlContent);
                    }

                    if (_currentBuild != null)
                    {
                        Console.WriteLine($"Build parsed: {_currentBuild.Name}");
                        Console.WriteLine($"SkillSets: {_currentBuild.SkillSets.Count}, ItemSets: {_currentBuild.ItemSets.Count}");

                        // Populate loadout selectors
                        PopulateLoadoutSelectors();
                        UpdateDisplayedLoadout();
                        BuildTitleText.Text = _currentBuild.CharacterDescription;
                    }

                    dialog.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    var errorDialog = new Window
                    {
                        Title = "Error",
                        Width = 500,
                        Height = 300,
                        Content = new ScrollViewer
                        {
                            Content = new TextBlock 
                            { 
                                Text = $"Failed to import:\n\n{ex.Message}",
                                Margin = new Avalonia.Thickness(20),
                                TextWrapping = Avalonia.Media.TextWrapping.Wrap
                            }
                        }
                    };
                    await errorDialog.ShowDialog(this);
                }
            }
        };

        cancelBtn.Click += (s, ev) => dialog.Close();

        buttonPanel.Children.Add(importBtn);
        buttonPanel.Children.Add(cancelBtn);
        
        panel.Children.Add(new TextBlock { Text = "Paste PoB Code:", Margin = new Avalonia.Thickness(0, 0, 0, 10) });
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        await dialog.ShowDialog(this);
    }

    private void PopulateLoadoutSelectors()
    {
        if (_currentBuild == null) return;

        // Populate skill set selector
        SkillSetSelector.ItemsSource = _currentBuild.SkillSets
            .Select((ss, index) => new { Index = index, Title = ss.Title, Display = $"{ss.Title} ({ss.TotalGems} gems)" })
            .ToList();
        SkillSetSelector.DisplayMemberBinding = new Avalonia.Data.Binding("Display");
        if (_currentBuild.SkillSets.Count > 0)
            SkillSetSelector.SelectedIndex = 0;

        // Populate item set selector
        ItemSetSelector.ItemsSource = _currentBuild.ItemSets
            .Select((its, index) => new { Index = index, Title = its.Title, Display = $"{its.Title} ({its.Items.Count} items)" })
            .ToList();
        ItemSetSelector.DisplayMemberBinding = new Avalonia.Data.Binding("Display");
        if (_currentBuild.ItemSets.Count > 0)
            ItemSetSelector.SelectedIndex = 0;
    }

    private void SkillSetSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentBuild == null || SkillSetSelector.SelectedIndex < 0) return;
        _currentBuild.ActiveSkillSetIndex = SkillSetSelector.SelectedIndex;
        UpdateDisplayedLoadout();
    }

    private void ItemSetSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentBuild == null || ItemSetSelector.SelectedIndex < 0) return;
        _currentBuild.ActiveItemSetIndex = ItemSetSelector.SelectedIndex;
        UpdateDisplayedLoadout();
    }

    private void UpdateDisplayedLoadout()
    {
        if (_currentBuild == null) return;

        Console.WriteLine($"=== UpdateDisplayedLoadout ===");

        // Get link groups from the active skill set
        var activeSkillSet = _currentBuild.ActiveSkillSet;
        if (activeSkillSet != null)
        {
            Console.WriteLine($"Active skill set: {activeSkillSet.Title}, LinkGroups: {activeSkillSet.LinkGroups.Count}");

            // Use the pre-parsed link groups from the build
            var linkGroups = activeSkillSet.LinkGroups
                .Where(lg => lg.IsEnabled && lg.Gems.Any())
                .ToList();

            LinkGroupsListBox.ItemsSource = linkGroups;
        }
        else
        {
            Console.WriteLine("No active skill set found");
            LinkGroupsListBox.ItemsSource = null;
        }

        // Get items from the active item set
        var activeItemSet = _currentBuild.ActiveItemSet;
        if (activeItemSet != null)
        {
            ItemsListBox.ItemsSource = activeItemSet.Items.OrderBy(i => i.Slot).ToList();
        }
        else
        {
            ItemsListBox.ItemsSource = null;
        }
    }
}
