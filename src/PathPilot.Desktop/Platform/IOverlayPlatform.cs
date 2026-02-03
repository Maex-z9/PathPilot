using System;

namespace PathPilot.Desktop.Platform;

public interface IOverlayPlatform
{
    void MakeClickThrough(IntPtr hwnd);
    void MakeInteractive(IntPtr hwnd);
    bool IsClickThrough { get; }
}
