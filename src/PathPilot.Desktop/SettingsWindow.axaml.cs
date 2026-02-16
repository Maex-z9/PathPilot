using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PathPilot.Core.Services;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PathPilot.Desktop;

public partial class SettingsWindow : Window
{
    private readonly OverlaySettings _settings;
    private readonly UpdateCheckService _updateCheckService = new();
    private string? _installerUrl;
    private string? _releaseUrl;
    private Button? _recordingButton;

    // Temporary values while editing
    private Key _toggleKey;
    private KeyModifiers _toggleModifiers;
    private Key _interactiveKey;
    private KeyModifiers _interactiveModifiers;
    private string? _logFilePath;

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
        _logFilePath = settings.PoeLogFilePath;

        UpdateHotkeyButtonTexts();
        LogPathTextBox.Text = _logFilePath ?? "";

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

    private async void BrowseLogButton_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select PoE Client.txt",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Log files") { Patterns = new[] { "Client.txt" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            _logFilePath = path;
            LogPathTextBox.Text = path;
        }
    }

    private void ResetPositionButton_Click(object? sender, RoutedEventArgs e)
    {
        _settings.OverlayX = 10;
        _settings.OverlayY = 10;
        SettingsChanged = true;
    }

    private async void CheckUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        // If we already found an update, start the install
        if (_installerUrl != null)
        {
            await DownloadAndInstallAsync();
            return;
        }

        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "Checking...";
        UpdateStatusText.Text = "Checking for updates...";
        UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 97, 86)); // #6b6156
        UpdateStatusText.Cursor = new Cursor(StandardCursorType.Arrow);
        _installerUrl = null;
        _releaseUrl = null;

        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var currentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";

        var (hasUpdate, newVersion, installerUrl, releaseUrl, error) = await _updateCheckService.CheckForUpdateAsync(currentVersion);

        if (error)
        {
            UpdateStatusText.Text = "Check failed — try again later";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(168, 107, 107)); // muted red
        }
        else if (hasUpdate && newVersion != null)
        {
            _installerUrl = installerUrl;
            _releaseUrl = releaseUrl;

            if (installerUrl != null)
            {
                UpdateStatusText.Text = $"v{newVersion} available!";
                UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 110)); // gold
                CheckUpdateButton.Content = "Install Update";
            }
            else
            {
                // No installer asset found — fallback to opening release page
                UpdateStatusText.Text = $"v{newVersion} available! Click here to download.";
                UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 110));
                UpdateStatusText.Cursor = new Cursor(StandardCursorType.Hand);
                UpdateStatusText.PointerPressed -= UpdateStatusText_PointerPressed;
                UpdateStatusText.PointerPressed += UpdateStatusText_PointerPressed;
            }
        }
        else
        {
            UpdateStatusText.Text = "Up to date";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(107, 97, 86)); // #6b6156
        }

        if (_installerUrl == null)
            CheckUpdateButton.Content = "Check for Updates";
        CheckUpdateButton.IsEnabled = true;
    }

    private async Task DownloadAndInstallAsync()
    {
        if (_installerUrl == null) return;

        CheckUpdateButton.IsEnabled = false;
        CheckUpdateButton.Content = "Downloading...";
        UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(200, 170, 110));

        var progress = new Progress<int>(percent =>
        {
            UpdateStatusText.Text = $"Downloading... {percent}%";
        });

        var installerPath = await _updateCheckService.DownloadInstallerAsync(_installerUrl, progress);

        if (installerPath != null)
        {
            UpdateStatusText.Text = "Starting installer...";
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            // Close the app so the installer can replace files
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
        }
        else
        {
            UpdateStatusText.Text = "Download failed — try again";
            UpdateStatusText.Foreground = new SolidColorBrush(Color.FromRgb(168, 107, 107));
            CheckUpdateButton.Content = "Install Update";
            CheckUpdateButton.IsEnabled = true;
        }
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
        // Read log path from textbox (user may have typed directly)
        var logPath = string.IsNullOrWhiteSpace(LogPathTextBox.Text) ? null : LogPathTextBox.Text.Trim();

        // Check if anything changed
        if (_toggleKey != _settings.ToggleKey ||
            _toggleModifiers != _settings.ToggleModifiers ||
            _interactiveKey != _settings.InteractiveKey ||
            _interactiveModifiers != _settings.InteractiveModifiers ||
            logPath != _settings.PoeLogFilePath)
        {
            SettingsChanged = true;
        }

        // Apply changes
        _settings.ToggleKey = _toggleKey;
        _settings.ToggleModifiers = _toggleModifiers;
        _settings.InteractiveKey = _interactiveKey;
        _settings.InteractiveModifiers = _interactiveModifiers;
        _settings.PoeLogFilePath = logPath;

        // Save to disk
        _settings.Save();

        Close();
    }
}
