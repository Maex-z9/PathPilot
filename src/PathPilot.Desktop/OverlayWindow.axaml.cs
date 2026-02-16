using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PathPilot.Core.Models;
using PathPilot.Core.Services;
using PathPilot.Desktop.Platform;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace PathPilot.Desktop;

public partial class OverlayWindow : Window
{
    private readonly IOverlayPlatform _platform;
    private readonly OverlaySettings _settings;
    private readonly QuestDataService _questDataService;
    private readonly QuestProgressService _questProgressService;
    private Build? _currentBuild;
    private bool _isDragging;
    private Point _dragStartPoint;
    private List<Quest> _allQuests = new();
    private HashSet<string> _completedQuestIds = new();
    private QuestCategory _currentQuestCategory = QuestCategory.SkillPoints;
    private bool _hideCompleted;

    private enum QuestCategory { SkillPoints, Trials }

    private static readonly SolidColorBrush ActiveTabBrush = new(Color.FromRgb(61, 53, 32));   // #3d3520
    private static readonly SolidColorBrush InactiveTabBrush = new(Color.FromRgb(42, 37, 32)); // #2a2520
    private static readonly SolidColorBrush ActiveTabForeground = new(Color.FromRgb(200, 170, 110));  // #c8aa6e
    private static readonly SolidColorBrush InactiveTabForeground = new(Color.FromRgb(168, 155, 140)); // #a89b8c
    private static readonly SolidColorBrush ActiveTabBorder = new(Color.FromRgb(200, 170, 110));  // #c8aa6e
    private static readonly SolidColorBrush InactiveTabBorder = new(Color.FromRgb(90, 79, 61));   // #5a4f3d

    public OverlayWindow(OverlaySettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _platform = new WindowsOverlayPlatform();
        _questDataService = new QuestDataService();
        _questProgressService = new QuestProgressService();

        // Set initial position from settings
        Position = new PixelPoint((int)_settings.OverlayX, (int)_settings.OverlayY);

        // Make window click-through on load
        Opened += OnWindowOpened;

        // Setup drag handling
        DragHandle.PointerPressed += OnDragHandlePointerPressed;
        DragHandle.PointerMoved += OnDragHandlePointerMoved;
        DragHandle.PointerReleased += OnDragHandlePointerReleased;

        // Update hotkey info text
        UpdateHotkeyInfoText();

        // Load quests with saved progress
        LoadQuests();
    }

    private void LoadQuests()
    {
        _allQuests = _questDataService.GetAllQuests();
        _completedQuestIds = _questProgressService.LoadCompletedQuestIds();

        // Restore completed state
        foreach (var quest in _allQuests)
        {
            if (_completedQuestIds.Contains(quest.Id))
                quest.IsCompleted = true;

            quest.PropertyChanged += OnQuestCompletedChanged;
        }

        UpdateQuestDisplay();
    }

    private void OnQuestCompletedChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Quest.IsCompleted) || sender is not Quest quest)
            return;

        if (quest.IsCompleted)
            _completedQuestIds.Add(quest.Id);
        else
            _completedQuestIds.Remove(quest.Id);

        _questProgressService.SaveCompletedQuestIds(_completedQuestIds);
        UpdateQuestProgressText();

        if (_hideCompleted)
            UpdateQuestDisplay();
    }

    private void UpdateQuestDisplay()
    {
        var filtered = _currentQuestCategory switch
        {
            QuestCategory.SkillPoints => _allQuests.Where(q => q.Reward == QuestReward.SkillPoint),
            QuestCategory.Trials => _allQuests.Where(q => q.Reward == QuestReward.AscendancyTrial),
            _ => _allQuests.AsEnumerable()
        };

        if (_hideCompleted)
            filtered = filtered.Where(q => !q.IsCompleted);

        QuestsListBox.ItemsSource = filtered.ToList();
        UpdateQuestProgressText();
    }

    private void UpdateQuestProgressText()
    {
        var categoryQuests = _currentQuestCategory switch
        {
            QuestCategory.SkillPoints => _allQuests.Where(q => q.Reward == QuestReward.SkillPoint),
            QuestCategory.Trials => _allQuests.Where(q => q.Reward == QuestReward.AscendancyTrial),
            _ => _allQuests.AsEnumerable()
        };

        var total = categoryQuests.Count();
        var completed = categoryQuests.Count(q => q.IsCompleted);
        QuestProgressText.Text = $"{completed}/{total}";
    }

    private void SetQuestCategoryTab(QuestCategory category)
    {
        _currentQuestCategory = category;

        SkillPointsTabButton.Background = category == QuestCategory.SkillPoints ? ActiveTabBrush : InactiveTabBrush;
        SkillPointsTabButton.Foreground = category == QuestCategory.SkillPoints ? ActiveTabForeground : InactiveTabForeground;
        SkillPointsTabButton.BorderBrush = category == QuestCategory.SkillPoints ? ActiveTabBorder : InactiveTabBorder;
        TrialsTabButton.Background = category == QuestCategory.Trials ? ActiveTabBrush : InactiveTabBrush;
        TrialsTabButton.Foreground = category == QuestCategory.Trials ? ActiveTabForeground : InactiveTabForeground;
        TrialsTabButton.BorderBrush = category == QuestCategory.Trials ? ActiveTabBorder : InactiveTabBorder;

        UpdateQuestDisplay();
    }

    private void SkillPointsTabButton_Click(object? sender, RoutedEventArgs e) =>
        SetQuestCategoryTab(QuestCategory.SkillPoints);

    private void TrialsTabButton_Click(object? sender, RoutedEventArgs e) =>
        SetQuestCategoryTab(QuestCategory.Trials);

    private void HideCompletedCheckBox_Click(object? sender, RoutedEventArgs e)
    {
        _hideCompleted = HideCompletedCheckBox.IsChecked == true;
        UpdateQuestDisplay();
    }

    private void GemsTabButton_Click(object? sender, RoutedEventArgs e)
    {
        GemsPanel.IsVisible = true;
        QuestsPanel.IsVisible = false;
        GemsTabButton.Background = ActiveTabBrush;
        GemsTabButton.Foreground = ActiveTabForeground;
        GemsTabButton.BorderBrush = ActiveTabBorder;
        QuestsTabButton.Background = InactiveTabBrush;
        QuestsTabButton.Foreground = InactiveTabForeground;
        QuestsTabButton.BorderBrush = InactiveTabBorder;
    }

    private void QuestsTabButton_Click(object? sender, RoutedEventArgs e)
    {
        GemsPanel.IsVisible = false;
        QuestsPanel.IsVisible = true;
        GemsTabButton.Background = InactiveTabBrush;
        GemsTabButton.Foreground = InactiveTabForeground;
        GemsTabButton.BorderBrush = InactiveTabBorder;
        QuestsTabButton.Background = ActiveTabBrush;
        QuestsTabButton.Foreground = ActiveTabForeground;
        QuestsTabButton.BorderBrush = ActiveTabBorder;
    }

    private void UpdateHotkeyInfoText()
    {
        var toggleHotkey = FormatHotkey(_settings.ToggleModifiers, _settings.ToggleKey);
        var interactiveHotkey = FormatHotkey(_settings.InteractiveModifiers, _settings.InteractiveKey);
        HotkeyInfoText.Text = $"{toggleHotkey}: Toggle | {interactiveHotkey}: Interact";
    }

    private static string FormatHotkey(KeyModifiers modifiers, Key key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle != IntPtr.Zero)
            {
                _platform.MakeClickThrough(handle);
                UpdateModeText();
            }
        }
    }

    private void OnDragHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(DragHandle);
            e.Handled = true;
        }
    }

    private void OnDragHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;

            Position = new PixelPoint(
                Position.X + (int)offset.X,
                Position.Y + (int)offset.Y
            );
        }
    }

    private void OnDragHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);

            // Save position to settings
            _settings.OverlayX = Position.X;
            _settings.OverlayY = Position.Y;
            _settings.Save();
        }
    }

    public void SetBuild(Build? build)
    {
        _currentBuild = build;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_currentBuild == null)
        {
            BuildNameText.Text = "No Build Loaded";
            CharacterInfoText.Text = "";
            GemsListBox.ItemsSource = null;
            return;
        }

        BuildNameText.Text = _currentBuild.Name;
        CharacterInfoText.Text = _currentBuild.CharacterDescription;

        // Get link groups from the active skill set
        var activeSkillSet = _currentBuild.ActiveSkillSet;
        if (activeSkillSet != null)
        {
            var linkGroups = activeSkillSet.LinkGroups
                .Where(lg => lg.IsEnabled && lg.Gems.Any())
                .ToList();
            GemsListBox.ItemsSource = linkGroups;
        }
        else
        {
            GemsListBox.ItemsSource = null;
        }
    }

    public void ToggleInteractive()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero)
            return;

        if (_platform.IsClickThrough)
        {
            _platform.MakeInteractive(handle);
        }
        else
        {
            _platform.MakeClickThrough(handle);
        }

        UpdateModeText();
    }

    private void UpdateModeText()
    {
        ModeText.Text = _platform.IsClickThrough ? "Click-Through" : "Interactive";
        ModeText.Foreground = _platform.IsClickThrough
            ? new SolidColorBrush(Color.FromRgb(200, 170, 110))   // Gold - click-through
            : new SolidColorBrush(Color.FromRgb(232, 213, 163));  // Bright gold - interactive
    }

    public bool IsClickThrough => _platform.IsClickThrough;
}
