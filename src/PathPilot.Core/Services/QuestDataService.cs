using PathPilot.Core.Models;
using System.Collections.Generic;

namespace PathPilot.Core.Services;

public class QuestDataService
{
    public List<Quest> GetAllQuests()
    {
        return new List<Quest>
        {
            // Act 1
            new() { Name = "The Dweller of the Deep", Act = 1, Reward = QuestReward.SkillPoint, Location = "The Flooded Depths", RecommendedLevel = 8 },
            new() { Name = "The Marooned Mariner", Act = 1, Reward = QuestReward.SkillPoint, Location = "The Ship Graveyard Cave", RecommendedLevel = 10 },
            new() { Name = "The Way Forward", Act = 1, Reward = QuestReward.SkillPoint, Location = "The Climb", RecommendedLevel = 12 },

            // Act 2
            new() { Name = "The Great White Beast", Act = 2, Reward = QuestReward.SkillPoint, Location = "The Den", RecommendedLevel = 15 },
            new() { Name = "Deal with the Bandits", Act = 2, Reward = QuestReward.SkillPoint, RewardDescription = "Kill all 3 or help one", Location = "Various Camps", RecommendedLevel = 18 },
            new() { Name = "Trial of Ascendancy", Act = 2, Reward = QuestReward.AscendancyTrial, Location = "The Crypt Level 1", RecommendedLevel = 15, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 2, Reward = QuestReward.AscendancyTrial, Location = "The Chamber of Sins Level 2", RecommendedLevel = 18, IsOptional = true },

            // Act 3
            new() { Name = "Victario's Secrets", Act = 3, Reward = QuestReward.SkillPoint, Location = "The Sewers", RecommendedLevel = 24 },
            new() { Name = "Piety's Pets", Act = 3, Reward = QuestReward.SkillPoint, Location = "The Lunaris Temple Level 2", RecommendedLevel = 26 },
            new() { Name = "Trial of Ascendancy", Act = 3, Reward = QuestReward.AscendancyTrial, Location = "The Crematorium", RecommendedLevel = 25, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 3, Reward = QuestReward.AscendancyTrial, Location = "The Catacombs", RecommendedLevel = 27, IsOptional = true },

            // Act 4
            new() { Name = "An Indomitable Spirit", Act = 4, Reward = QuestReward.SkillPoint, Location = "The Mines Level 2", RecommendedLevel = 32 },
            new() { Name = "Trial of Ascendancy", Act = 4, Reward = QuestReward.AscendancyTrial, Location = "The Imperial Gardens", RecommendedLevel = 33, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 4, Reward = QuestReward.AscendancyTrial, Location = "The Hedge Maze", RecommendedLevel = 33, IsOptional = true },
            new() { Name = "The Labyrinth", Act = 4, Reward = QuestReward.Labyrinth, RewardDescription = "Normal Lab - 2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 33, IsOptional = true },

            // Act 5
            new() { Name = "In Service to Science", Act = 5, Reward = QuestReward.SkillPoint, Location = "The Control Blocks", RecommendedLevel = 38 },
            new() { Name = "Kitava's Torments", Act = 5, Reward = QuestReward.SkillPoint, Location = "The Reliquary", RecommendedLevel = 40 },

            // Act 6
            new() { Name = "The Father of War", Act = 6, Reward = QuestReward.SkillPoint, Location = "The Karui Fortress", RecommendedLevel = 45 },
            new() { Name = "The Puppet Mistress", Act = 6, Reward = QuestReward.SkillPoint, Location = "The Mud Flats", RecommendedLevel = 42 },
            new() { Name = "The Cloven One", Act = 6, Reward = QuestReward.SkillPoint, Location = "The Prison", RecommendedLevel = 44 },
            new() { Name = "Trial of Ascendancy", Act = 6, Reward = QuestReward.AscendancyTrial, Location = "The Prison", RecommendedLevel = 44, IsOptional = true },

            // Act 7
            new() { Name = "The Master of a Million Faces", Act = 7, Reward = QuestReward.SkillPoint, Location = "The Ashen Fields", RecommendedLevel = 48 },
            new() { Name = "Queen of Despair", Act = 7, Reward = QuestReward.SkillPoint, Location = "The Causeway", RecommendedLevel = 50 },
            new() { Name = "Kishara's Star", Act = 7, Reward = QuestReward.SkillPoint, Location = "The Causeway", RecommendedLevel = 50 },
            new() { Name = "Trial of Ascendancy", Act = 7, Reward = QuestReward.AscendancyTrial, Location = "The Crypt", RecommendedLevel = 48, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 7, Reward = QuestReward.AscendancyTrial, Location = "The Chamber of Sins Level 2", RecommendedLevel = 52, IsOptional = true },
            new() { Name = "The Labyrinth", Act = 7, Reward = QuestReward.Labyrinth, RewardDescription = "Cruel Lab - 2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 55, IsOptional = true },

            // Act 8
            new() { Name = "Love is Dead", Act = 8, Reward = QuestReward.SkillPoint, Location = "The Quay", RecommendedLevel = 54 },
            new() { Name = "Reflection of Terror", Act = 8, Reward = QuestReward.SkillPoint, Location = "The Grain Gate", RecommendedLevel = 55 },
            new() { Name = "The Gemling Legion", Act = 8, Reward = QuestReward.SkillPoint, Location = "The Grain Gate", RecommendedLevel = 55 },
            new() { Name = "Trial of Ascendancy", Act = 8, Reward = QuestReward.AscendancyTrial, Location = "The Bath House", RecommendedLevel = 58, IsOptional = true },

            // Act 9
            new() { Name = "Queen of the Sands", Act = 9, Reward = QuestReward.SkillPoint, Location = "The Oasis", RecommendedLevel = 60 },
            new() { Name = "The Ruler of Highgate", Act = 9, Reward = QuestReward.SkillPoint, Location = "The Quarry", RecommendedLevel = 62 },
            new() { Name = "Trial of Ascendancy", Act = 9, Reward = QuestReward.AscendancyTrial, Location = "The Tunnel", RecommendedLevel = 61, IsOptional = true },

            // Act 10
            new() { Name = "Vilenta's Vengeance", Act = 10, Reward = QuestReward.SkillPoint, Location = "The Control Blocks", RecommendedLevel = 64 },
            new() { Name = "An End to Hunger", Act = 10, Reward = QuestReward.SkillPoint, Location = "The Feeding Trough", RecommendedLevel = 66 },
            new() { Name = "Trial of Ascendancy", Act = 10, Reward = QuestReward.AscendancyTrial, Location = "The Ossuary", RecommendedLevel = 65, IsOptional = true },
            new() { Name = "The Labyrinth", Act = 10, Reward = QuestReward.Labyrinth, RewardDescription = "Merciless Lab - 2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 68, IsOptional = true },
        };
    }

    public List<Quest> GetSkillPointQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.SkillPoint);

    public List<Quest> GetTrialQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.AscendancyTrial);

    public List<Quest> GetLabQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.Labyrinth);
}
