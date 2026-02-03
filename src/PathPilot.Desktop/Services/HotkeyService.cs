using Avalonia.Input;
using Avalonia.Threading;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PathPilot.Desktop.Services;

public class HotkeyService : IDisposable
{
    // Windows API for low-level keyboard hook
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private readonly OverlaySettings _settings;
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _isDisposed;

    public event Action? ToggleOverlayRequested;
    public event Action? ToggleInteractiveRequested;

    public HotkeyService(OverlaySettings settings)
    {
        _settings = settings;
    }

    public void Start()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("HotkeyService: Global hotkeys only supported on Windows");
            return;
        }

        if (_hookId != IntPtr.Zero)
            return;

        _proc = HookCallback;
        _hookId = SetHook(_proc);

        if (_hookId == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to set keyboard hook: {Marshal.GetLastWin32Error()}");
        }
        else
        {
            Console.WriteLine("Global keyboard hook installed");
            Console.WriteLine($"  Toggle Overlay: {FormatHotkey(_settings.ToggleModifiers, _settings.ToggleKey)}");
            Console.WriteLine($"  Toggle Interactive: {FormatHotkey(_settings.InteractiveModifiers, _settings.InteractiveKey)}");
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Console.WriteLine("Global keyboard hook stopped");
        }
    }

    public void Restart()
    {
        Stop();
        Start();
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

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var moduleName = curModule?.ModuleName;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(moduleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (int)hookStruct.vkCode;
            var pressedKey = VirtualKeyToAvaloniaKey(vkCode);

            if (pressedKey != Key.None)
            {
                var modifiers = GetCurrentModifiers();

                // Check for Toggle Overlay hotkey
                if (pressedKey == _settings.ToggleKey && modifiers == _settings.ToggleModifiers)
                {
                    Dispatcher.UIThread.Post(() => ToggleOverlayRequested?.Invoke());
                    return (IntPtr)1; // Suppress the key
                }

                // Check for Toggle Interactive hotkey
                if (pressedKey == _settings.InteractiveKey && modifiers == _settings.InteractiveModifiers)
                {
                    Dispatcher.UIThread.Post(() => ToggleInteractiveRequested?.Invoke());
                    return (IntPtr)1; // Suppress the key
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static KeyModifiers GetCurrentModifiers()
    {
        var modifiers = KeyModifiers.None;

        if ((GetAsyncKeyState(0x11) & 0x8000) != 0) // VK_CONTROL
            modifiers |= KeyModifiers.Control;
        if ((GetAsyncKeyState(0x10) & 0x8000) != 0) // VK_SHIFT
            modifiers |= KeyModifiers.Shift;
        if ((GetAsyncKeyState(0x12) & 0x8000) != 0) // VK_MENU (Alt)
            modifiers |= KeyModifiers.Alt;

        return modifiers;
    }

    private static Key VirtualKeyToAvaloniaKey(int vkCode)
    {
        return vkCode switch
        {
            0x70 => Key.F1,
            0x71 => Key.F2,
            0x72 => Key.F3,
            0x73 => Key.F4,
            0x74 => Key.F5,
            0x75 => Key.F6,
            0x76 => Key.F7,
            0x77 => Key.F8,
            0x78 => Key.F9,
            0x79 => Key.F10,
            0x7A => Key.F11,
            0x7B => Key.F12,
            0x41 => Key.A,
            0x42 => Key.B,
            0x43 => Key.C,
            0x44 => Key.D,
            0x45 => Key.E,
            0x46 => Key.F,
            0x47 => Key.G,
            0x48 => Key.H,
            0x49 => Key.I,
            0x4A => Key.J,
            0x4B => Key.K,
            0x4C => Key.L,
            0x4D => Key.M,
            0x4E => Key.N,
            0x4F => Key.O,
            0x50 => Key.P,
            0x51 => Key.Q,
            0x52 => Key.R,
            0x53 => Key.S,
            0x54 => Key.T,
            0x55 => Key.U,
            0x56 => Key.V,
            0x57 => Key.W,
            0x58 => Key.X,
            0x59 => Key.Y,
            0x5A => Key.Z,
            _ => Key.None
        };
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_hookId != IntPtr.Zero && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Console.WriteLine("Global keyboard hook removed");
        }
    }
}
