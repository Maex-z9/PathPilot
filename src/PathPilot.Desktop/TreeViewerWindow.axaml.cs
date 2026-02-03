using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PathPilot.Desktop;

public partial class TreeViewerWindow : Window
{
    private string _treeUrl = string.Empty;
    private string _fullUrl = string.Empty;

    public TreeViewerWindow()
    {
        InitializeComponent();
    }

    public TreeViewerWindow(string treeUrl, string title) : this()
    {
        _treeUrl = treeUrl;
        TreeTitleText.Text = title;

        // Build the full URL
        _fullUrl = treeUrl;
        if (!_fullUrl.StartsWith("http"))
        {
            _fullUrl = "https://www.pathofexile.com/passive-skill-tree/" + _fullUrl;
        }

        // Navigate to the tree URL when window loads
        Opened += OnWindowOpened;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        NavigateToTree();
    }

    private void NavigateToTree()
    {
        if (string.IsNullOrEmpty(_fullUrl))
            return;

        try
        {
            TreeWebView.Address = _fullUrl;
            Console.WriteLine($"WebView navigating to: {_fullUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating WebView: {ex.Message}");
        }
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            TreeWebView.Reload();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reloading: {ex.Message}");
            NavigateToTree();
        }
    }

    private void OpenExternalButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_fullUrl))
            return;

        try
        {
            OpenUrl(_fullUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL: {ex.Message}");
        }
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
}
