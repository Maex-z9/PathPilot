using PathPilot.Core.Models;
using PathPilot.Desktop.Settings;
using System;

namespace PathPilot.Desktop.Services;

public class OverlayService : IDisposable
{
    private OverlayWindow? _overlayWindow;
    private Build? _currentBuild;
    private readonly OverlaySettings _settings;
    private readonly HotkeyService _hotkeyService;

    public bool IsVisible => _overlayWindow?.IsVisible ?? false;
    public bool IsInteractive => !(_overlayWindow?.IsClickThrough ?? true);

    public event Action<bool>? VisibilityChanged;

    public OverlayService(OverlaySettings settings, HotkeyService hotkeyService)
    {
        _settings = settings;
        _hotkeyService = hotkeyService;

        _hotkeyService.ToggleOverlayRequested += ToggleVisibility;
        _hotkeyService.ToggleInteractiveRequested += ToggleInteractive;
    }

    public void ShowOverlay(Build? build = null)
    {
        if (build != null)
            _currentBuild = build;

        if (_overlayWindow == null)
        {
            _overlayWindow = new OverlayWindow(_settings);
            _overlayWindow.Closed += (_, _) =>
            {
                _overlayWindow = null;
                VisibilityChanged?.Invoke(false);
            };
        }

        _overlayWindow.SetBuild(_currentBuild);
        _overlayWindow.Show();
        VisibilityChanged?.Invoke(true);
    }

    public void HideOverlay()
    {
        _overlayWindow?.Hide();
        VisibilityChanged?.Invoke(false);
    }

    public void ToggleVisibility()
    {
        if (_overlayWindow == null || !_overlayWindow.IsVisible)
        {
            ShowOverlay();
        }
        else
        {
            HideOverlay();
        }
    }

    public void ToggleInteractive()
    {
        _overlayWindow?.ToggleInteractive();
    }

    public void UpdateBuild(Build? build)
    {
        _currentBuild = build;
        _overlayWindow?.SetBuild(build);
    }

    public void Dispose()
    {
        _hotkeyService.ToggleOverlayRequested -= ToggleVisibility;
        _hotkeyService.ToggleInteractiveRequested -= ToggleInteractive;
        _overlayWindow?.Close();
        _overlayWindow = null;
    }
}
