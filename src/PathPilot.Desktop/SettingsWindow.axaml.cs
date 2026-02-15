using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PathPilot.Core.Services;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace PathPilot.Desktop;

public partial class SettingsWindow : Window
{
    private readonly OverlaySettings _settings;
    private readonly UpdateCheckService _updateCheckService = new();
    private string? _releaseUrl;
    private Button? _recordingButton;

    // Temporary values while editing
    private Key _toggleKey;
    private KeyModifiers _toggleModifiers;
    private Key _interactiveKey;
    private KeyModifiers _interactiveModifiers;

    public bool SettingsChanged { get; private set; }

    public SettingsWindow(OverlaySettings settings)
    {
        InitializeComponent();

        _settings = settings;

        // Load current values
        _toggleKey = settings.ToggleKey;
        _toggleModifiers = settings.ToggleModifiers;
        _interactiveKey = settings.InteractiveKey;
        _interactiveModifiers = settings.InteractiveModifiers;

        UpdateHotkeyButtonTexts();

        // Show current version
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
            VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";

        // Handle key recording
        KeyDown += OnKeyDown;
    }

    private void UpdateHotkeyButtonTexts()
    {
        ToggleHotkeyButton.Content = FormatHotkey(_toggleModifiers, _toggleKey);
        InteractiveHotkeyButton.Content = FormatHotkey(_interactiveModifiers, _interactiveKey);
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

    private void ToggleHotkeyButton_Click(object? sender, RoutedEventArgs e)
    {
        StartRecording(ToggleHotkeyButton);
    }

    private void InteractiveHotkeyButton_Click(object? sender, RoutedEventArgs e)
    {
        StartRecording(InteractiveHotkeyButton);
    }

    private void StartRecording(Button button)
    {
        // Reset previous recording button if any
        if (_recordingButton != null)
        {
            StopRecording(false);
        }

        _recordingButton = button;
        button.Content = "Press key...";
        button.Background = new SolidColorBrush(Color.FromRgb(200, 170, 110)); // Gold recording
        button.Foreground = new SolidColorBrush(Color.FromRgb(12, 11, 10));   // Dark text
    }

    private void StopRecording(bool success)
    {
        if (_recordingButton != null)
        {
            _recordingButton.Background = new SolidColorBrush(Color.FromRgb(42, 37, 32));    // #2a2520
            _recordingButton.Foreground = new SolidColorBrush(Color.FromRgb(224, 214, 194)); // #e0d6c2
            _recordingButton = null;
        }

        if (success)
        {
            UpdateHotkeyButtonTexts();
        }
        else
        {
            UpdateHotkeyButtonTexts();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_recordingButton == null)
            return;

        // Ignore modifier-only presses
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            return;
        }

        // Check if it's a valid hotkey key
        if (!IsValidHotkeyKey(e.Key))
        {
            return;
        }

        var modifiers = e.KeyModifiers;

        // Assign to the appropriate hotkey
        if (_recordingButton == ToggleHotkeyButton)
        {
            _toggleKey = e.Key;
            _toggleModifiers = modifiers;
        }
        else if (_recordingButton == InteractiveHotkeyButton)
        {
            _interactiveKey = e.Key;
            _interactiveModifiers = modifiers;
        }

        StopRecording(true);
        e.Handled = true;
    }

    private static bool IsValidHotkeyKey(Key key)
    {
        // Allow F1-F12
        if (key >= Key.F1 && key <= Key.F12)
            return true;

        // Allow A-Z
        if (key >= Key.A && key <= Key.Z)
            return true;

        // Allow 0-9
        if (key >= Key.D0 && key <= Key.D9)
            return true;

        // Allow some special keys
        return key switch
        {
            Key.Escape or Key.Tab or Key.Space or
            Key.Insert or Key.Delete or Key.Home or Key.End or
            Key.PageUp or Key.PageDown or
            Key.Pause or Key.Scroll => true,
            _ => false
        };
    }

    private void ResetPositionButton_Click(object? sender, RoutedEventArgs e)
    {
        _settings.OverlayX = 10;
        _settings.OverlayY = 10;
        SettingsChanged = true;
    }

    private async void CheckUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "Checking...";
        UpdateStatusText.Text = "Checking for updates...";
        UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 97, 86)); // #6b6156
        UpdateStatusText.Cursor = new Cursor(StandardCursorType.Arrow);
        _releaseUrl = null;

        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var currentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

        var (hasUpdate, newVersion, releaseUrl, error) = await _updateCheckService.CheckForUpdateAsync(currentVersion);

        if (error)
        {
            UpdateStatusText.Text = "Check failed — try again later";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(168, 107, 107)); // muted red
        }
        else if (hasUpdate && newVersion != null)
        {
            _releaseUrl = releaseUrl;
            UpdateStatusText.Text = $"v{newVersion} available! Click here to download.";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 110)); // #c8aa6e gold
            UpdateStatusText.Cursor = new Cursor(StandardCursorType.Hand);
            UpdateStatusText.PointerPressed -= UpdateStatusText_PointerPressed;
            UpdateStatusText.PointerPressed += UpdateStatusText_PointerPressed;
        }
        else
        {
            UpdateStatusText.Text = "Up to date";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 97, 86)); // #6b6156
        }

        CheckUpdateButton.Content = "Check for Updates";
        CheckUpdateButton.IsEnabled = true;
    }

    private void UpdateStatusText_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_releaseUrl != null)
        {
            Process.Start(new ProcessStartInfo(_releaseUrl) { UseShellExecute = true });
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Check if anything changed
        if (_toggleKey != _settings.ToggleKey ||
            _toggleModifiers != _settings.ToggleModifiers ||
            _interactiveKey != _settings.InteractiveKey ||
            _interactiveModifiers != _settings.InteractiveModifiers)
        {
            SettingsChanged = true;
        }

        // Apply changes
        _settings.ToggleKey = _toggleKey;
        _settings.ToggleModifiers = _toggleModifiers;
        _settings.InteractiveKey = _interactiveKey;
        _settings.InteractiveModifiers = _interactiveModifiers;

        // Save to disk
        _settings.Save();

        Close();
    }
}
