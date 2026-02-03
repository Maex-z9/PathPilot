using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PathPilot.Desktop;

public partial class ImportDialog : Window
{
    private TextBox? _inputTextBox;
    private string? _result;

    public ImportDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Width = 600;
        Height = 400;
        Title = "Import Build";
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;

        var mainPanel = new StackPanel
        {
            Spacing = 20,
            Margin = new Avalonia.Thickness(30)
        };

        // Title
        mainPanel.Children.Add(new TextBlock
        {
            Text = "Import Path of Building Build",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold
        });

        // Instructions
        mainPanel.Children.Add(new TextBlock
        {
            Text = "Paste your Path of Building code or pobb.in URL below:",
            FontSize = 14,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888888"))
        });

        // Input TextBox
        _inputTextBox = new TextBox
        {
            Height = 150,
            AcceptsReturn = true,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Watermark = "Paste PoB code or URL here...",
            Padding = new Avalonia.Thickness(10)
        };
        mainPanel.Children.Add(_inputTextBox);

        // Example text
        mainPanel.Children.Add(new TextBlock
        {
            Text = "Examples:\n• https://pobb.in/abc123\n• eNqdWQtv2zgS/...",
            FontSize = 12,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666"))
        });

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Avalonia.Thickness(20, 10),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#404040"))
        };
        cancelButton.Click += CancelButton_Click;

        var importButton = new Button
        {
            Content = "Import",
            Padding = new Avalonia.Thickness(20, 10),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0066cc")),
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White),
            FontWeight = Avalonia.Media.FontWeight.Bold
        };
        importButton.Click += ImportButton_Click;

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(importButton);

        mainPanel.Children.Add(buttonPanel);

        Content = mainPanel;
    }

    private void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        _result = _inputTextBox?.Text?.Trim();
        Close(_result);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}