using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;

namespace PathPilot.Desktop;

public partial class SettingsWindow : Window
{
    private readonly OverlaySettings _settings;
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
        button.Background = new SolidColorBrush(Color.FromRgb(255, 107, 53)); // Orange
    }

    private void StopRecording(bool success)
    {
        if (_recordingButton != null)
        {
            _recordingButton.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
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
