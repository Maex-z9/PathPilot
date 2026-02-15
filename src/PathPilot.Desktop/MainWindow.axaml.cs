using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PathPilot.Core.Models;
using PathPilot.Core.Parsers;
using PathPilot.Core.Services;
using PathPilot.Desktop.Services;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PathPilot.Desktop;

public partial class MainWindow : Window
{
    private Build? _currentBuild;
    private readonly GemDataService _gemDataService;
    private readonly GemIconService _gemIconService;
    private readonly OverlaySettings _overlaySettings;
    private readonly HotkeyService _hotkeyService;
    private readonly OverlayService _overlayService;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize gem data service
        _gemDataService = new GemDataService();
        _gemDataService.LoadDatabase();

        // Initialize gem icon service
        _gemIconService = new GemIconService();

        // Initialize overlay services
        _overlaySettings = OverlaySettings.Load();
        _hotkeyService = new HotkeyService(_overlaySettings);
        _overlayService = new OverlayService(_overlaySettings, _hotkeyService);

        // Register hotkeys when window opens
        Opened += OnMainWindowOpened;
        Closed += OnMainWindowClosed;

        // Add keyboard handler for local hotkeys (fallback for non-Windows)
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Toggle overlay visibility (uses configured hotkey)
        if (e.Key == _overlaySettings.ToggleKey && e.KeyModifiers == _overlaySettings.ToggleModifiers)
        {
            _overlayService.ToggleVisibility();
            e.Handled = true;
        }
        // Toggle interactive mode (uses configured hotkey)
        else if (e.Key == _overlaySettings.InteractiveKey && e.KeyModifiers == _overlaySettings.InteractiveModifiers)
        {
            _overlayService.ToggleInteractive();
            e.Handled = true;
        }
    }

    private async void OnMainWindowOpened(object? sender, EventArgs e)
    {
        // Start global hotkey listener
        _hotkeyService.Start();

        // Update overlay button tooltip with configured hotkey
        OverlayButton.SetValue(ToolTip.TipProperty,
            $"Show overlay ({FormatHotkey(_overlaySettings.ToggleModifiers, _overlaySettings.ToggleKey)} to toggle)");

        // Auto-load last saved build, or show import dialog if none exist
        var savedBuilds = BuildStorage.GetSavedBuilds();
        if (savedBuilds.Count > 0)
        {
            var lastBuild = BuildStorage.LoadBuild(savedBuilds[0].FilePath);
            if (lastBuild != null)
            {
                _currentBuild = lastBuild;
                PopulateLoadoutSelectors();
                UpdateDisplayedLoadout();
                BuildTitleText.Text = _currentBuild.CharacterDescription;
                SaveButton.IsEnabled = true;
                Console.WriteLine($"Auto-loaded last build: {_currentBuild.Name}");

                _overlayService.UpdateBuild(_currentBuild);
                _ = PreloadGemIconsAsync();
            }
        }
        else
        {
            ImportButton_Click(null, new RoutedEventArgs());
        }
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        _hotkeyService.Dispose();
        _overlayService.Dispose();
        _gemIconService.Dispose();
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

                        // Populate loadout selectors and display immediately (with color dots)
                        PopulateLoadoutSelectors();
                        UpdateDisplayedLoadout();
                        BuildTitleText.Text = _currentBuild.CharacterDescription;
                        SaveButton.IsEnabled = true;

                        // Update overlay with new build
                        _overlayService.UpdateBuild(_currentBuild);

                        // Async load gem icons, then refresh UI
                        _ = PreloadGemIconsAsync();
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

        // Populate unified loadout selector with all unique loadout names
        var loadoutNames = _currentBuild.GetLoadoutNames();
        LoadoutSelector.ItemsSource = loadoutNames;

        if (loadoutNames.Count > 0)
            LoadoutSelector.SelectedIndex = 0;
    }

    private void LoadoutSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentBuild == null || LoadoutSelector.SelectedItem == null) return;

        var selectedLoadout = LoadoutSelector.SelectedItem.ToString();
        if (!string.IsNullOrEmpty(selectedLoadout))
        {
            _currentBuild.SetActiveLoadout(selectedLoadout);
            UpdateDisplayedLoadout();

            // Update overlay when loadout changes
            _overlayService.UpdateBuild(_currentBuild);
        }
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

        // Update tree info
        var activeTreeSet = _currentBuild.ActiveTreeSet;
        if (activeTreeSet != null)
        {
            TreePointsText.Text = $"Points: {activeTreeSet.PointsUsed}";
            OpenTreeButton.IsEnabled = !string.IsNullOrEmpty(activeTreeSet.TreeUrl);
        }
        else
        {
            TreePointsText.Text = "Points: --";
            OpenTreeButton.IsEnabled = false;
        }
    }

    private async void OpenTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentBuild == null)
            return;

        var activeTreeSet = _currentBuild.ActiveTreeSet;
        if (activeTreeSet == null || string.IsNullOrEmpty(activeTreeSet.TreeUrl))
            return;

        try
        {
            var treeWindow = new TreeViewerWindow(_currentBuild);
            await treeWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening tree viewer: {ex.Message}");
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentBuild == null) return;

        // Create save dialog with name input
        var dialog = new Window
        {
            Title = "Save Build",
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };

        panel.Children.Add(new TextBlock { Text = "Enter a name for this build:" });

        var nameTextBox = new TextBox
        {
            Text = _currentBuild.Name,
            Watermark = "Build name..."
        };
        panel.Children.Add(nameTextBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var saveBtn = new Button { Content = "Save", Background = Avalonia.Media.Brushes.Green };
        var cancelBtn = new Button { Content = "Cancel" };

        saveBtn.Click += (s, ev) =>
        {
            try
            {
                var buildName = string.IsNullOrWhiteSpace(nameTextBox.Text)
                    ? _currentBuild.Name
                    : nameTextBox.Text.Trim();

                _currentBuild.Name = buildName;
                var filePath = BuildStorage.SaveBuild(_currentBuild);
                Console.WriteLine($"Build saved to: {filePath}");
                BuildTitleText.Text = _currentBuild.CharacterDescription;
                dialog.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving build: {ex.Message}");
            }
        };

        cancelBtn.Click += (s, ev) => dialog.Close();

        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(saveBtn);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        await dialog.ShowDialog(this);
    }

    private async void LoadButton_Click(object? sender, RoutedEventArgs e)
    {
        var savedBuilds = BuildStorage.GetSavedBuilds();

        if (savedBuilds.Count == 0)
        {
            var emptyDialog = new Window
            {
                Title = "No Saved Builds",
                Width = 300,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = "No saved builds found." },
                        new Button { Content = "OK", Margin = new Avalonia.Thickness(0, 15, 0, 0) }
                    }
                }
            };
            var okBtn = (Button)((StackPanel)emptyDialog.Content).Children[1];
            okBtn.Click += (s, ev) => emptyDialog.Close();
            await emptyDialog.ShowDialog(this);
            return;
        }

        // Create load dialog
        var dialog = new Window
        {
            Title = "Load Build",
            Width = 500,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };
        panel.Children.Add(new TextBlock { Text = "Select a build to load:", FontWeight = Avalonia.Media.FontWeight.Bold });

        var listBox = new ListBox
        {
            Height = 250,
            ItemsSource = savedBuilds,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<SavedBuildInfo>((info, _) =>
            {
                var sp = new StackPanel { Margin = new Avalonia.Thickness(5) };
                sp.Children.Add(new TextBlock { Text = info?.Name ?? "Unknown", FontWeight = Avalonia.Media.FontWeight.SemiBold });
                sp.Children.Add(new TextBlock { Text = info?.Description ?? "", Foreground = Avalonia.Media.Brushes.Gray, FontSize = 12 });
                sp.Children.Add(new TextBlock { Text = info?.LastModified.ToString("g") ?? "", Foreground = Avalonia.Media.Brushes.DarkGray, FontSize = 11 });
                return sp;
            }, supportsRecycling: true)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 10
        };

        var loadBtn = new Button { Content = "Load" };
        var deleteBtn = new Button { Content = "Delete", Background = Avalonia.Media.Brushes.DarkRed };
        var cancelBtn = new Button { Content = "Cancel" };

        loadBtn.Click += (s, ev) =>
        {
            if (listBox.SelectedItem is SavedBuildInfo selectedBuild)
            {
                var build = BuildStorage.LoadBuild(selectedBuild.FilePath);
                if (build != null)
                {
                    _currentBuild = build;
                    PopulateLoadoutSelectors();
                    UpdateDisplayedLoadout();
                    BuildTitleText.Text = _currentBuild.CharacterDescription;
                    SaveButton.IsEnabled = true;
                    Console.WriteLine($"Build loaded: {_currentBuild.Name}");

                    // Update overlay with loaded build
                    _overlayService.UpdateBuild(_currentBuild);

                    // Async load gem icons, then refresh UI
                    _ = PreloadGemIconsAsync();
                }
                dialog.Close();
            }
        };

        deleteBtn.Click += (s, ev) =>
        {
            if (listBox.SelectedItem is SavedBuildInfo selectedBuild)
            {
                BuildStorage.DeleteBuild(selectedBuild.FilePath);
                var updatedList = BuildStorage.GetSavedBuilds();
                listBox.ItemsSource = updatedList;
                if (updatedList.Count == 0)
                    dialog.Close();
            }
        };

        cancelBtn.Click += (s, ev) => dialog.Close();

        buttonPanel.Children.Add(deleteBtn);
        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(loadBtn);

        panel.Children.Add(listBox);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        await dialog.ShowDialog(this);
    }

    private void OverlayButton_Click(object? sender, RoutedEventArgs e)
    {
        _overlayService.ShowOverlay(_currentBuild);
    }

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        // Temporarily stop hotkey listener while settings dialog is open
        _hotkeyService.Stop();

        var settingsWindow = new SettingsWindow(_overlaySettings);
        await settingsWindow.ShowDialog(this);

        // Restart hotkey listener with potentially new settings
        _hotkeyService.Restart();

        // Update overlay button tooltip with new hotkey
        OverlayButton.SetValue(ToolTip.TipProperty,
            $"Show overlay ({FormatHotkey(_overlaySettings.ToggleModifiers, _overlaySettings.ToggleKey)} to toggle)");
    }

    private async Task PreloadGemIconsAsync()
    {
        if (_currentBuild == null) return;

        // Enrich gems with IconUrl from database if missing (e.g. loaded from old saves)
        var allGems = _currentBuild.SkillSets
            .SelectMany(ss => ss.LinkGroups)
            .SelectMany(lg => lg.Gems)
            .GroupBy(g => g.Name)
            .Select(g => g.First())
            .ToList();

        foreach (var gem in allGems)
        {
            if (string.IsNullOrEmpty(gem.IconUrl))
            {
                var gemInfo = _gemDataService.GetGemInfo(gem.Name);
                if (gemInfo?.IconUrl != null)
                    gem.IconUrl = gemInfo.IconUrl;
            }
        }

        allGems = allGems.Where(g => !string.IsNullOrEmpty(g.IconUrl)).ToList();

        Console.WriteLine($"Loading icons for {allGems.Count} gems...");

        var tasks = allGems.Select(async gem =>
        {
            var iconPath = await _gemIconService.GetIconPathAsync(gem.Name, gem.IconUrl);
            return (gem.Name, iconPath);
        });

        var results = await Task.WhenAll(tasks);

        // Build a lookup of name -> path
        var iconPaths = new Dictionary<string, string>();
        foreach (var (name, path) in results)
        {
            if (path != null)
                iconPaths[name] = path;
        }

        Console.WriteLine($"Loaded {iconPaths.Count} gem icons");

        // Update all gems across all skill sets with their icon paths
        foreach (var skillSet in _currentBuild.SkillSets)
        {
            foreach (var linkGroup in skillSet.LinkGroups)
            {
                foreach (var gem in linkGroup.Gems)
                {
                    if (iconPaths.TryGetValue(gem.Name, out var path))
                        gem.IconPath = path;
                }
            }
        }

        // Refresh the UI to show icons
        Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateDisplayedLoadout());
    }

    private static string FormatHotkey(Avalonia.Input.KeyModifiers modifiers, Avalonia.Input.Key key)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
