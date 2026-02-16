using Avalonia.Threading;
using PathPilot.Core.Models;
using PathPilot.Core.Services;
using PathPilot.Desktop.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PathPilot.Desktop.Services;

public class QuestNotificationService : IDisposable
{
    private readonly OverlaySettings _settings;
    private readonly QuestDataService _questDataService;
    private readonly QuestProgressService _questProgressService;
    private readonly ClientLogWatcher _logWatcher = new();

    private List<Quest>? _allQuests;
    private HashSet<string>? _completedQuestIds;
    private QuestNotificationWindow? _currentNotification;

    // /passives detection: collect quest names seen in a short window
    private readonly List<string> _passivesBuffer = new();
    private Timer? _passivesFlushTimer;
    private readonly object _passivesLock = new();

    public event Action? QuestsAutoCompleted;

    public QuestNotificationService(
        OverlaySettings settings,
        QuestDataService questDataService,
        QuestProgressService questProgressService)
    {
        _settings = settings;
        _questDataService = questDataService;
        _questProgressService = questProgressService;

        _logWatcher.AreaChanged += OnAreaChanged;
        _logWatcher.LogLineReceived += OnLogLine;
    }

    public void Start()
    {
        var logPath = _settings.PoeLogFilePath;

        // Auto-detect if not configured
        if (string.IsNullOrEmpty(logPath))
        {
            logPath = OverlaySettings.AutoDetectLogPath();
            if (logPath != null)
            {
                _settings.PoeLogFilePath = logPath;
                _settings.Save();
                Console.WriteLine($"Auto-detected PoE log: {logPath}");
            }
        }

        if (string.IsNullOrEmpty(logPath))
        {
            Console.WriteLine("No PoE log file configured — quest notifications disabled");
            return;
        }

        // Load quest data
        _allQuests = _questDataService.GetAllQuests();
        _completedQuestIds = _questProgressService.LoadCompletedQuestIds();

        _logWatcher.Start(logPath);
        Console.WriteLine($"Quest notification service started, watching: {logPath}");
    }

    public void Stop()
    {
        _logWatcher.Stop();
    }

    public void ReloadProgress()
    {
        _completedQuestIds = _questProgressService.LoadCompletedQuestIds();
    }

    private void OnAreaChanged(string areaName)
    {
        if (_allQuests == null || _completedQuestIds == null)
            return;

        var matchingQuests = _allQuests
            .Where(q => !_completedQuestIds.Contains(q.Id))
            .Where(q => string.Equals(NormalizeLocation(q.Location), areaName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingQuests.Count == 0)
            return;

        Console.WriteLine($"Zone '{areaName}' has {matchingQuests.Count} pending quest(s)");

        Dispatcher.UIThread.Post(() => ShowNotification(areaName, matchingQuests));
    }

    private void OnLogLine(string line)
    {
        if (_allQuests == null || _completedQuestIds == null)
            return;

        // Only look at system messages (contain "] : " but not chat channels like #, @, $)
        var msgIndex = line.IndexOf("] : ", StringComparison.Ordinal);
        if (msgIndex < 0)
            return;

        var message = line[(msgIndex + 4)..];

        // Skip chat messages (start with #, @, $, <)
        if (message.Length > 0 && (message[0] == '#' || message[0] == '@' || message[0] == '$' || message[0] == '<'))
            return;

        // Check if line contains a known skill point quest name
        var matchedQuest = _allQuests
            .Where(q => q.Reward == QuestReward.SkillPoint)
            .FirstOrDefault(q => message.Contains(q.Name, StringComparison.OrdinalIgnoreCase));

        if (matchedQuest == null)
            return;

        lock (_passivesLock)
        {
            _passivesBuffer.Add(matchedQuest.Id);

            // Reset the flush timer — wait for more lines (batch /passives output)
            _passivesFlushTimer?.Dispose();
            _passivesFlushTimer = new Timer(_ => FlushPassivesBuffer(), null, 2000, Timeout.Infinite);
        }
    }

    private void FlushPassivesBuffer()
    {
        List<string> questIds;
        lock (_passivesLock)
        {
            if (_passivesBuffer.Count == 0)
                return;

            questIds = new List<string>(_passivesBuffer);
            _passivesBuffer.Clear();
        }

        // Only treat as /passives output if we see at least 2 quest names in quick succession
        // (single quest name in a line could be random chat)
        if (questIds.Count < 2)
        {
            Console.WriteLine($"Ignoring single quest mention (might be chat): {questIds[0]}");
            return;
        }

        if (_completedQuestIds == null)
            return;

        var newlyCompleted = new List<string>();
        foreach (var id in questIds)
        {
            if (_completedQuestIds.Add(id))
                newlyCompleted.Add(id);
        }

        if (newlyCompleted.Count == 0)
        {
            Console.WriteLine($"/passives detected {questIds.Count} quests — all already marked complete");
            return;
        }

        // Save progress
        _questProgressService.SaveCompletedQuestIds(_completedQuestIds);

        var questNames = newlyCompleted
            .Select(id => _allQuests?.FirstOrDefault(q => q.Id == id)?.Name ?? id)
            .ToList();

        Console.WriteLine($"/passives: auto-completed {newlyCompleted.Count} quest(s): {string.Join(", ", questNames)}");

        Dispatcher.UIThread.Post(() => QuestsAutoCompleted?.Invoke());
    }

    private void ShowNotification(string zoneName, List<Quest> quests)
    {
        // Close any existing notification
        _currentNotification?.Close();

        _currentNotification = new QuestNotificationWindow(zoneName, quests);
        _currentNotification.Closed += (_, _) => _currentNotification = null;
        _currentNotification.Show();
    }

    private static string NormalizeLocation(string location)
    {
        var parenIndex = location.IndexOf('(');
        return (parenIndex > 0 ? location[..parenIndex] : location).Trim();
    }

    public void Dispose()
    {
        _passivesFlushTimer?.Dispose();
        _logWatcher.AreaChanged -= OnAreaChanged;
        _logWatcher.LogLineReceived -= OnLogLine;
        _logWatcher.Dispose();
    }
}
