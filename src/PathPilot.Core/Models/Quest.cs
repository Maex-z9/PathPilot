namespace PathPilot.Core.Models;

public class Quest
{
    public string Name { get; set; } = string.Empty;
    public int Act { get; set; }
    public QuestReward Reward { get; set; } = QuestReward.None;
    public string RewardDescription { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int RecommendedLevel { get; set; }
    public bool IsOptional { get; set; }
    public bool IsCompleted { get; set; }
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
