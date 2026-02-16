using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using PathPilot.Core.Models;
using System;
using System.Collections.Generic;

namespace PathPilot.Desktop;

public partial class QuestNotificationWindow : Window
{
    private readonly DispatcherTimer _autoCloseTimer;

    public QuestNotificationWindow()
    {
        InitializeComponent();

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _autoCloseTimer.Tick += (_, _) => Close();

        PointerPressed += OnWindowClicked;
    }

    public QuestNotificationWindow(string zoneName, List<Quest> quests) : this()
    {
        ZoneNameText.Text = zoneName;
        QuestItemsControl.ItemsSource = quests;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Position: centered horizontally, near top of screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var scaling = screen.Scaling;
            var screenWidth = screen.WorkingArea.Width / scaling;
            var desiredX = (screenWidth - Width) / 2;

            // SizeToContent means Width may not be set yet — use a reasonable estimate
            if (double.IsNaN(Width) || Width <= 0)
                desiredX = (screenWidth - 320) / 2;

            Position = new PixelPoint((int)(desiredX * scaling), 50);
        }

        _autoCloseTimer.Start();
    }

    private void OnWindowClicked(object? sender, PointerPressedEventArgs e)
    {
        _autoCloseTimer.Stop();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoCloseTimer.Stop();
        base.OnClosed(e);
    }
}
