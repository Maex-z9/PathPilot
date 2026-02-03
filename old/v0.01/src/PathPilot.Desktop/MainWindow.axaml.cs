using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PathPilot.Core.Models;
using PathPilot.Core.Parsers;
using PathPilot.Core.Services;

namespace PathPilot.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly GemDataService _gemDataService;
        private readonly PobUrlImporter _pobImporter;
        private readonly PobXmlParser _pobParser;
        
        private Build _currentBuild;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize services
            _gemDataService = new GemDataService();
            _pobParser = new PobXmlParser(_gemDataService);
            _pobImporter = new PobUrlImporter(_pobParser);
        }

        /// <summary>
        /// Handle PoB import button click
        /// </summary>
        private async void OnImportClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var pobInput = PobInputTextBox?.Text?.Trim();
                
                if (string.IsNullOrWhiteSpace(pobInput))
                {
                    await ShowError("Please enter a PoB pastebin URL or paste code");
                    return;
                }

                // Show loading indicator
                ShowLoading(true);

                // Import the build
                _currentBuild = await _pobImporter.ImportAsync(pobInput);

                // Update UI
                UpdateBuildDisplay();
                
                ShowLoading(false);
                
                // Show success message
                StatusTextBlock.Text = $"✓ Loaded: {_currentBuild.Name}";
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                await ShowError($"Import failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the entire build display
        /// </summary>
        private void UpdateBuildDisplay()
        {
            if (_currentBuild == null)
                return;

            // Update build header info
            BuildNameTextBlock.Text = _currentBuild.Name;
            CharacterInfoTextBlock.Text = _currentBuild.CharacterDescription;

            // Populate loadout selector
            PopulateLoadoutSelector();

            // Display the active loadout
            UpdateDisplayedLoadout();
        }

        /// <summary>
        /// Populate the skill set (loadout) dropdown
        /// </summary>
        private void PopulateLoadoutSelector()
        {
            if (LoadoutComboBox == null || _currentBuild == null)
                return;

            LoadoutComboBox.Items.Clear();

            foreach (var skillSet in _currentBuild.SkillSets)
            {
                LoadoutComboBox.Items.Add(skillSet.Title);
            }

            // Select the active loadout
            if (_currentBuild.SkillSets.Any())
            {
                LoadoutComboBox.SelectedIndex = _currentBuild.ActiveSkillSetIndex;
            }
        }

        /// <summary>
        /// Handle loadout selection change
        /// </summary>
        private void OnLoadoutChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_currentBuild == null || LoadoutComboBox.SelectedIndex < 0)
                return;

            _currentBuild.ActiveSkillSetIndex = LoadoutComboBox.SelectedIndex;
            UpdateDisplayedLoadout();
        }

        /// <summary>
        /// Update the displayed gems and items for the current loadout
        /// THIS IS THE KEY METHOD - Uses real link groups from PoB
        /// </summary>
        private void UpdateDisplayedLoadout()
        {
            if (_currentBuild?.ActiveSkillSet == null)
                return;

            var activeSkillSet = _currentBuild.ActiveSkillSet;
            
            // Clear existing display
            LinkGroupsPanel?.Children.Clear();

            // Display each link group
            foreach (var linkGroup in activeSkillSet.LinkGroups)
            {
                DisplayLinkGroup(linkGroup);
            }

            // Update stats
            UpdateGemStats(activeSkillSet);

            // Update items display
            UpdateItemsDisplay();
        }

        /// <summary>
        /// Display a single link group with proper visual grouping
        /// </summary>
        private void DisplayLinkGroup(GemLinkGroup linkGroup)
        {
            if (LinkGroupsPanel == null || linkGroup.Gems == null || !linkGroup.Gems.Any())
                return;

            // Create container for this link group
            var groupPanel = new StackPanel
            {
                Classes = { "link-group-panel" },
                Margin = new Thickness(0, 0, 0, 16)
            };

            // Header for link group
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Classes = { "link-group-header" },
                Margin = new Thickness(0, 0, 0, 8)
            };

            var slotText = new TextBlock
            {
                Text = linkGroup.Key,
                Classes = { "link-group-slot" },
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            var linkCountText = new TextBlock
            {
                Text = $"({linkGroup.LinkCount}L)",
                Classes = { "link-count" },
                Margin = new Thickness(8, 0, 0, 0),
                Foreground = new SolidColorBrush(Colors.Gray)
            };

            headerPanel.Children.Add(slotText);
            headerPanel.Children.Add(linkCountText);
            
            if (!linkGroup.IsEnabled)
            {
                var disabledText = new TextBlock
                {
                    Text = "DISABLED",
                    Margin = new Thickness(8, 0, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Red),
                    FontSize = 12
                };
                headerPanel.Children.Add(disabledText);
            }

            groupPanel.Children.Add(headerPanel);

            // Display each gem in the link group
            var gemsContainer = new StackPanel { Classes = { "gems-container" } };
            
            foreach (var gem in linkGroup.Gems)
            {
                var gemPanel = CreateGemPanel(gem);
                gemsContainer.Children.Add(gemPanel);
            }

            groupPanel.Children.Add(gemsContainer);
            
            // Add visual separator
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Margin = new Thickness(0, 8, 0, 0)
            };
            groupPanel.Children.Add(separator);

            LinkGroupsPanel.Children.Add(groupPanel);
        }

        /// <summary>
        /// Create UI panel for a single gem
        /// </summary>
        private Panel CreateGemPanel(Gem gem)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 4),
                Classes = { "gem-panel" }
            };

            // Gem color indicator
            var colorIndicator = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(GetGemColor(gem.Color)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            panel.Children.Add(colorIndicator);

            // Gem name
            var nameText = new TextBlock
            {
                Text = gem.DisplayName,
                FontWeight = gem.IsMainActiveSkill ? FontWeight.Bold : 
                            gem.IsSupport ? FontWeight.Normal : FontWeight.SemiBold,
                FontSize = gem.IsMainActiveSkill ? 14 : 13,
                Foreground = gem.IsEnabled ? 
                    new SolidColorBrush(Colors.White) : 
                    new SolidColorBrush(Colors.Gray)
            };
            panel.Children.Add(nameText);

            // Support gem indicator
            if (gem.IsSupport)
            {
                var supportBadge = new TextBlock
                {
                    Text = "SUPPORT",
                    FontSize = 10,
                    Margin = new Thickness(8, 0, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237))
                };
                panel.Children.Add(supportBadge);
            }

            // Main active skill indicator
            if (gem.IsMainActiveSkill)
            {
                var mainSkillBadge = new TextBlock
                {
                    Text = "★",
                    FontSize = 14,
                    Margin = new Thickness(8, 0, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Gold)
                };
                panel.Children.Add(mainSkillBadge);
            }

            // Level/quality info
            var levelQualityText = new TextBlock
            {
                Text = $"Lvl {gem.Level}",
                FontSize = 11,
                Margin = new Thickness(8, 0, 0, 0),
                Foreground = new SolidColorBrush(Colors.LightGray)
            };
            panel.Children.Add(levelQualityText);

            // Acquisition info (on hover or separate panel)
            if (!string.IsNullOrWhiteSpace(gem.AcquisitionInfo))
            {
                panel.ToolTip.Content = gem.AcquisitionInfo;
            }

            return panel;
        }

        /// <summary>
        /// Update gem statistics display
        /// </summary>
        private void UpdateGemStats(SkillSet skillSet)
        {
            if (GemStatsPanel == null)
                return;

            var statsText = $"Link Groups: {skillSet.LinkGroups.Count} | " +
                          $"Total Gems: {skillSet.TotalGems} | " +
                          $"Active: {skillSet.ActiveGemCount} | " +
                          $"Support: {skillSet.SupportGemCount}";

            GemStatsTextBlock.Text = statsText;
        }

        /// <summary>
        /// Update items display
        /// </summary>
        private void UpdateItemsDisplay()
        {
            if (_currentBuild?.ActiveItemSet == null || ItemsPanel == null)
                return;

            ItemsPanel.Children.Clear();

            foreach (var item in _currentBuild.ActiveItemSet.Items)
            {
                var itemPanel = CreateItemPanel(item);
                ItemsPanel.Children.Add(itemPanel);
            }
        }

        /// <summary>
        /// Create UI panel for a single item
        /// </summary>
        private Panel CreateItemPanel(Item item)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 4, 0, 4),
                Classes = { "item-panel" }
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var slotText = new TextBlock
            {
                Text = $"{item.Slot}:",
                Width = 100,
                FontWeight = FontWeight.Bold
            };
            headerPanel.Children.Add(slotText);

            var nameText = new TextBlock
            {
                Text = item.DisplayName,
                Foreground = new SolidColorBrush(GetRarityColor(item.Rarity))
            };
            headerPanel.Children.Add(nameText);

            panel.Children.Add(headerPanel);

            // Add tooltip with full item details
            if (!string.IsNullOrWhiteSpace(item.RawText))
            {
                panel.ToolTip.Content = item.RawText;
            }

            return panel;
        }

        /// <summary>
        /// Get Avalonia color for gem socket color
        /// </summary>
        private Color GetGemColor(SocketColor socketColor)
        {
            return socketColor switch
            {
                SocketColor.Red => Color.FromRgb(255, 77, 77),
                SocketColor.Green => Color.FromRgb(77, 255, 77),
                SocketColor.Blue => Color.FromRgb(77, 136, 255),
                SocketColor.White => Color.FromRgb(200, 200, 200),
                _ => Colors.Gray
            };
        }

        /// <summary>
        /// Get color for item rarity
        /// </summary>
        private Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "unique" => Color.FromRgb(175, 96, 37),
                "rare" => Color.FromRgb(255, 255, 119),
                "magic" => Color.FromRgb(136, 136, 255),
                _ => Colors.White
            };
        }

        private void ShowLoading(bool show)
        {
            // Implement loading indicator
            LoadingPanel.IsVisible = show;
        }

        private async Task ShowError(string message)
        {
            // Implement error dialog
            var dialog = new Window
            {
                Title = "Error",
                Width = 400,
                Height = 150,
                Content = new TextBlock { Text = message, Margin = new Thickness(20) }
            };
            await dialog.ShowDialog(this);
        }
    }
}
