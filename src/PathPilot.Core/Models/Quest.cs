using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PathPilot.Core.Models;

public class Quest : INotifyPropertyChanged
{
    private bool _isCompleted;

    public string Id => $"{Act}_{Name}_{Location}";
    public string Name { get; set; } = string.Empty;
    public int Act { get; set; }
    public QuestReward Reward { get; set; } = QuestReward.None;
    public string RewardDescription { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TrapType { get; set; } = string.Empty;
    public int RecommendedLevel { get; set; }
    public bool IsOptional { get; set; }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted != value)
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum QuestReward
{
    None,
    SkillPoint,
    PassiveRespec,
    AscendancyTrial,
    Labyrinth,
    PantheonSoul,
    SkillGem
}
