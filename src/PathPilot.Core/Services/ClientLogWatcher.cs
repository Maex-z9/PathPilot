using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace PathPilot.Core.Services;

public class ClientLogWatcher : IDisposable
{
    private static readonly Regex AreaRegex = new(@"You have entered (.+)\.$", RegexOptions.Compiled);

    private FileSystemWatcher? _watcher;
    private long _lastPosition;
    private string? _logFilePath;
    private Timer? _debounceTimer;

    public event Action<string>? AreaChanged;
    public event Action<string>? LogLineReceived;

    public void Start(string logFilePath)
    {
        Stop();

        if (!File.Exists(logFilePath))
            return;

        _logFilePath = logFilePath;

        // Start reading from end of file (only new entries)
        _lastPosition = new FileInfo(logFilePath).Length;

        var directory = Path.GetDirectoryName(logFilePath);
        var fileName = Path.GetFileName(logFilePath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
            return;

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
    }

    public void Stop()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;

        if (_watcher != null)
        {
            _watcher.Changed -= OnFileChanged;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        _logFilePath = null;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce — FileSystemWatcher fires multiple times per write
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => ReadNewLines(), null, 100, Timeout.Infinite);
    }

    private void ReadNewLines()
    {
        if (_logFilePath == null || !File.Exists(_logFilePath))
            return;

        try
        {
            using var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (stream.Length < _lastPosition)
            {
                // File was truncated (e.g. new session), reset
                _lastPosition = 0;
            }

            if (stream.Length == _lastPosition)
                return;

            stream.Seek(_lastPosition, SeekOrigin.Begin);

            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                LogLineReceived?.Invoke(line);

                var match = AreaRegex.Match(line);
                if (match.Success)
                {
                    var areaName = match.Groups[1].Value;
                    AreaChanged?.Invoke(areaName);
                }
            }

            _lastPosition = stream.Position;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClientLogWatcher error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
