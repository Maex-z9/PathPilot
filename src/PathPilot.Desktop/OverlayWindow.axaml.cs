using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PathPilot.Core.Models;
using PathPilot.Desktop.Platform;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PathPilot.Desktop;

public partial class OverlayWindow : Window
{
    private readonly IOverlayPlatform _platform;
    private readonly OverlaySettings _settings;
    private Build? _currentBuild;
    private bool _isDragging;
    private Point _dragStartPoint;

    public OverlayWindow(OverlaySettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _platform = new WindowsOverlayPlatform();

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
            ? Avalonia.Media.Brushes.LightGreen
            : Avalonia.Media.Brushes.Orange;
    }

    public bool IsClickThrough => _platform.IsClickThrough;
}
