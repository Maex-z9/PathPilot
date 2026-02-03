using System;
using System.Runtime.InteropServices;

namespace PathPilot.Desktop.Platform;

public class WindowsOverlayPlatform : IOverlayPlatform
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private IntPtr _hwnd;
    private int _originalExStyle;

    public bool IsClickThrough { get; private set; } = true;

    public void MakeClickThrough(IntPtr hwnd)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        _hwnd = hwnd;
        _originalExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        int newStyle = _originalExStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        IsClickThrough = true;
    }

    public void MakeInteractive(IntPtr hwnd)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        _hwnd = hwnd;
        int currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        // Remove WS_EX_TRANSPARENT but keep LAYERED and TOOLWINDOW
        int newStyle = (currentStyle & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED | WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        IsClickThrough = false;
    }
}
